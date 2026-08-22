using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// BOSS - THE HOLE
//
// A black hole hanging in the middle of the arena. It never moves and never
// touches the player: everything that hurts climbs out of the middle of it, so
// the pressure comes from the room filling up rather than from the boss.
//
// SHOOTING IT IS WHAT SPEEDS IT UP. At full HP it lets something out every few
// seconds and the layer is quiet; the more of it you have shot off, the harder
// it works, until it is spitting one out a second. So there is no safe pace -
// killing it fast means eating the flood, and taking it slow means the room
// fills anyway.
//
// The spin and the colour of the disc say the same thing the spawn rate does,
// so the player can read how bad it is about to get without counting mobs.
//
// There is no art for it anywhere in the atlases. The hole is baked pixel by
// pixel at the bottom of this file, the same way the web and the moth's gust
// are, so nothing has to be drawn for it.
//

public class Hive : MobBase
{
    // How wide the hole comes out, in pixels
    private const int SIZE = 320;

    // Nothing gets out inside this, as a share of the radius. The glowing disc
    // lives between it and the rim of the frame.
    private const float HORIZON = 0.40f;

    private const int ARMS = 5;             // streaks of matter falling in
    private const float TWIST = 3.6f;       // how tightly they wind
    private const float SHARP = 3f;         // higher = thinner streaks with darker gaps
    private const float ARM_FLOOR = 0.05f;  // what is left burning between them
    private const float PHOTON = 0.03f;     // thickness of the bright ring hugging the hole
    private const float SOFT = 1.5f;        // pixels of softening on the black edge
    private const float RIM = 0.08f;        // how much of the outside fades out

    // SPAWNING. The gap between adds is walked between these two by how much
    // HP is left - a trickle while it is untouched, a stream once it is nearly
    // dead. Seconds.
    private const float SLOW_SECONDS = 6f;
    private const float FAST_SECONDS = 0.7f;
    private float spawnTimer = 0f;

    // Radians per second, walked the same way
    private const float SLOW_SPIN = 0.35f;
    private const float FAST_SPIN = 3.2f;

    // What comes out, picked at random each time
    private static readonly MobType[] Adds = [MobType.Bat, MobType.PurpleBat, MobType.Fireball];

    // One hole per fight, but a new one every boss layer that rolls it, so the
    // picture is baked once and shared
    private static Texture2D _shared;

    protected override bool ShowBossHP => true;
    protected override AudioId? HitSound => AudioId.BatSqueak;

    // A ring of points instead of a box, because the thing is a circle and a
    // box would eat shots that clearly went past it
    protected override Vector2[] HitboxShape => Ring(12, HORIZON / 2f + 0.02f);

    public Hive()
    {
        SetHP(2500);
        DAMAGE = 0;
        PointsOnKill = 1500;
        ON_TOUCH = OnTouch.Nothing; // the adds do the hurting
    }

    public override void LoadContent(Vector2 _, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        _shared ??= Bake(SIZE);

        // One frame that never advances - the hole turns, it does not animate
        Sprite = new AnimatedSprite(
            new Animation([new TextureRegion(_shared, 0, 0, SIZE, SIZE)], TimeSpan.FromSeconds(1))
        );

        SetSpawn();
        RebuildBounds();
    }

    // Hangs high in the middle of the arena
    protected override void SetSpawn()
    {
        Sprite.Position = new Vector2(
            (Globals.VIRTUAL_WIDTH - Sprite.Width) / 2f,
            LayerTopY + 80
        );
    }

    protected override void UpdateBehaviour(IPlayer _, GameWorld gameWorld)
    {
        float rage = Rage;

        SpriteRotation = MathHelper.WrapAngle(
            SpriteRotation + MathHelper.Lerp(SLOW_SPIN, FAST_SPIN, rage) * Globals.DT
        );

        SpawnTimer(gameWorld, rage);
    }

    // The core is baked black with no colour in it, so a tint only ever lands
    // on the disc: it burns from pink to red as the hole is shot down, and
    // flashes near white when it is hit - a red flash on a red disc at the end
    // of the fight would say nothing.
    protected override Color DrawColour =>
        isHit ? Globals.AlmostWhite : Color.Lerp(Globals.HotPink, Globals.Red, Rage);

    private void SpawnTimer(GameWorld gameWorld, float rage)
    {
        spawnTimer += Globals.DT;

        if (spawnTimer < MathHelper.Lerp(SLOW_SECONDS, FAST_SECONDS, rage))
            return;

        spawnTimer = 0f;

        MobType add = Adds[Globals.R.Next(Adds.Length)];

        // Queued rather than added straight away, because we are being
        // iterated by the very list this would grow
        gameWorld.RequestMobSpawn(SpawnLayer, add, Centre);
    }

    // The middle of the hole - where everything comes out
    private Vector2 Centre => Sprite.Position + new Vector2(Sprite.Width, Sprite.Height) / 2f;

    // 0 while it is untouched, 1 as it dies. Everything the hole does - how
    // often it spits, how fast it turns, what colour it is - hangs off this.
    private float Rage => 1f - MathHelper.Clamp((float)HP / MaxHP, 0f, 1f);

    // Fixed in place
    public override void BounceFromFloor() {}

    // Points evenly round a circle, as fractions of the sprite frame, which is
    // what HitboxShape is measured in
    private static Vector2[] Ring(int points, float radius)
    {
        Vector2[] ring = new Vector2[points];

        for (int i = 0; i < points; i ++)
        {
            float angle = MathF.Tau * i / points;
            ring[i] = new Vector2(0.5f + radius * MathF.Cos(angle), 0.5f + radius * MathF.Sin(angle));
        }

        return ring;
    }

    //
    // THE HOLE ITSELF, BAKED PIXEL BY PIXEL
    //
    // Baked once at the size it is drawn at, because the world is drawn with
    // PointClamp and scaling would chew up the thin ring.
    //
    // Black in the middle and OPAQUE, so it reads as a piece missing out of the
    // level rather than a dark sprite laid over it, with the disc of matter
    // spiralling into it around the outside.
    //
    private static Texture2D Bake(int size)
    {
        Texture2D texture = new(Core.GraphicsDevice, size, size);
        Color[] pixels = new Color[size * size];

        float radius = size / 2f;
        float soft = SOFT / radius;     // the softening in the same units as the rest

        for (int y = 0; y < size; y ++)
        {
            for (int x = 0; x < size; x ++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float r = MathF.Sqrt(dx * dx + dy * dy) / radius;

                if (r > 1f)
                    continue;

                // The hole. Runs a hair past the horizon so its soft edge is
                // hidden under the bright ring instead of showing as a gap.
                float core = MathHelper.Clamp((HORIZON - r) / soft + 1f, 0f, 1f);

                float glow = 0f;

                if (r > HORIZON)
                {
                    // Brightest against the hole, gone by the rim of the frame
                    float outward = (r - HORIZON) / (1f - HORIZON);
                    float fade = (1f - outward) * (1f - outward);

                    // Streaks of matter winding in. They spiral rather than run
                    // straight, and are pinched thin, which is what makes the
                    // turning read as falling instead of as a glow sitting still.
                    float angle = MathF.Atan2(dy, dx);
                    float arms = 0.5f + 0.5f * MathF.Sin(ARMS * angle + TWIST * MathF.Log(r / HORIZON));
                    arms = MathF.Pow(arms, SHARP);

                    glow = fade * (ARM_FLOOR + (1f - ARM_FLOOR) * arms);

                    // The ring of light bent right around the edge - what makes
                    // this a black hole and not just a dark circle
                    float photon = 1f - MathHelper.Clamp((r - HORIZON) / PHOTON, 0f, 1f);
                    glow = MathF.Max(glow, photon * photon);

                    // Softened towards the rim so the disc does not end on a cut circle
                    glow *= MathHelper.Clamp((1f - r) / RIM, 0f, 1f);
                }

                // Premultiplied, like every other baked texture in the game.
                // The core carries alpha with no colour in it, which is what
                // keeps the tint off it and on the disc.
                pixels[y * size + x] = new Color(glow, glow, glow, MathHelper.Clamp(core + glow, 0f, 1f));
            }
        }

        texture.SetData(pixels);
        return texture;
    }
}
