using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// THE FRIENDLY ONE - the jellyfish turned inside out
//
// Drifts around harmlessly. WALK INTO IT and it starts a short fuse, then
// bursts into a cloud that HEALS whoever is standing in it.
//
// Touching is what sets it off, exactly like the jellyfish it is built from -
// and that is the point of the pair. Both are set off by the same act, one
// heals you and one kills you, and the green is all you get to tell them
// apart. Shooting it used to work, which let the player pop it from across the
// room for free; now getting to it is the whole cost, and the fuse means he has
// to still be there a second and a half later.
//
// Its drift leans towards the player, same as the jellyfish it is built from,
// so a butterfly he climbed past follows him up instead of being stranded a
// layer below. This one is in the player's favour: the only healing outside a
// boss kill stays reachable.
//
// IT BURSTS INTO DUST, NOT INTO A FIREBALL
//
// The burst used to be a hand drawn cloud - which was a drawing of an
// explosion, and so the one thing in the game that HELPS you looked exactly
// like the two things that blow you up. Now it throws gold dust (see GoldDust):
// warm, rising, and the only gold on the screen. A player who has learnt what
// pink means does not have to learn what gold means twice.
//
// AND IT ONLY EVER HEALS THE PLAYER. The jellyfish next door asks the world for
// a blast that catches everything standing in it, mobs included. This one does
// not go anywhere near that path - it hands the player his health directly and
// the dust is only a picture. A healing field that touched mobs would quietly
// heal the swarm chasing him, which is the exact opposite of a reward.
//

public class Butterfly : MobBase
{
    private AnimatedSprite _idle;

    private const int SPEED_MIN = 40, SPEED_MAX = 90;
    private Vector2 target = Vector2.Zero;
    private readonly int targetOffset = 250;

    // How far each new drift target is pulled towards the player. Kept under
    // targetOffset above so the wobble still dominates any one step.
    private const float DRIFT_TOWARDS = 200f;

    private const int HEAL_AMOUNT = 60;
    private const float FUSE_SECONDS = 1.5f;

    //
    // HOW CLOSE HE HAS TO BE WHEN IT GOES OFF
    //
    // He lit the fuse by walking into it, so this is really asking whether he
    // STAYED - which is the whole cost of the heal, and why it is not simply
    // handed to whoever brushed past.
    //
    // The old burst was a small cloud the player could touch on any frame of a
    // one second animation: a tight area, a long window. This is one instant
    // check instead, so the area has to be wider to be the same bargain - but
    // only about twice the old cloud, not the whole puff of dust. The dust
    // scatters further than this on purpose; it is the THICK of it that heals,
    // and standing in the thin edge of it was never going to be enough.
    private const float HEAL_RADIUS = 160f;

    private bool fuseLit = false;
    private float fuseTimer = 0f;

    // ITS OWN ARTWORK, IN ITS OWN COLOURS. It used to be the jellyfish drawing
    // washed green, because the shape was shared and the hue was the only thing
    // separating the mob that heals from the mob that explodes in your face.
    // With a butterfly that actually looks like a butterfly there is nothing
    // left to disambiguate, so it is drawn exactly as it was painted.
    private static readonly Color BODY = Color.White;

    private Color bodyColor = BODY;

    // Nothing ever hits it
    protected override AudioId? HitSound => null;

    // A pickup, not an enemy - never blocks the layer from being cleared
    public override bool CountsAsEnemy => false;

    // Shots pass straight through, same as the jellyfish. It is not shot at
    // all now, so it must not eat the bullets meant for what is behind it -
    // and with nothing ever hitting it, it never flashes red either.
    public override bool StopsBullets => false;

    // It burns itself out handing the player a heal. That is not a kill.
    public override bool GivesLifeSteal => false;

    protected override Vector2 HitboxScale => new(0.65f, 0.65f);

    public Butterfly()
    {
        SetHP(100);

        // Nothing reads this - it never touches the player for damage OR for
        // healing. The heal is handed over in Burst, where it can be given to
        // the player and to nobody else.
        DAMAGE = 0;

        PointsOnKill = 0;       // the heal IS the reward
        ON_TOUCH = OnTouch.Explode; // touching it lights the fuse - see Explode
        Invulnerable = true;
    }

    public override void LoadContent(Vector2 playerPos, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        // The burst animation is not loaded at all any more - the dust is
        // thrown by the world, not drawn by the mob that left it
        _idle = MobArt.Butterflies(MobArt.Butterfly);

        Sprite = _idle;

        SetSpawn();
        SetTarget(playerPos);

        SetDirectionTimer(0f, 3f);

        RebuildBounds();
    }

    protected override void SetSpawn()
    {
        Sprite.Position = new Vector2(
            Globals.R.Next(0, Globals.VIRTUAL_WIDTH - (int)Sprite.Width),
            LayerTopY + Globals.R.Next(0, Globals.VIRTUAL_HEIGHT - (int)Sprite.Height)
        );
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld gameWorld)
    {
        if (DirectionTimerElapsed())
        {
            SetTarget(player.Position);
            ResetDirectionTimer(0.5f, 3f);
        }

        (Sprite.Position, velocity) = MovementUtils.MoveTowardsTarget(Sprite.Position, target, velocity, speed, max_speed);
        velocity = MovementUtils.BounceFromEdge(velocity, Sprite.Position, Sprite.Width);
        SyncBounds();

        if (fuseLit)
            FuseTimer(player, gameWorld);

        Sprite.Update();
    }

    private void FuseTimer(IPlayer player, GameWorld gameWorld)
    {
        fuseTimer += Globals.DT;

        if (fuseTimer < FUSE_SECONDS)
        {
            // Flashes towards the green everything helpful in this game is
            // drawn in, faster the closer the burst is. The art is warm yellow,
            // so the green reads clearly against it without hiding it - and it
            // says "this is about to heal you" in the game's own colour.
            float t = fuseTimer / FUSE_SECONDS;
            float pulse = 0.5f + 0.5f * MathF.Sin(fuseTimer * MathHelper.TwoPi * (2f + 6f * t));

            bodyColor = Color.Lerp(BODY, Globals.HealGreen, pulse);
            return;
        }

        Burst(player, gameWorld);
    }

    //
    // IT BURSTS, AND IT IS GONE
    //
    // The heal is given here and nowhere else, which is what keeps it the
    // player's alone: there is no healing hitbox left lying around for a mob to
    // wander into. The dust is handed to the world afterwards because the
    // butterfly is dead on this frame and cannot draw anything itself.
    //
    private void Burst(IPlayer player, GameWorld gameWorld)
    {
        Vector2 centre = Sprite.Position + new Vector2(Sprite.Width, Sprite.Height) * 0.5f;

        // Only if he stayed for it. Measured the same way a blast is, so
        // "inside the cloud" means the same thing here as it does over there.
        Circle reach = new((int)centre.X, (int)centre.Y, (int)HEAL_RADIUS);

        if (player.HurtBox.Intersects(reach))
            player.Heal(HEAL_AMOUNT);

        gameWorld.SpawnGoldDust(centre);

        HP = 0;
    }

    // Walking into it lights the fuse. Called from the collision response, the
    // same OnTouch.Explode path the jellyfish uses - the two mobs are set off
    // by the same act on purpose.
    public override void Explode()
    {
        if (fuseLit)
            return;

        fuseLit = true; // the flashing in FuseTimer is the telegraph from here
    }

    // Bullets fly through it and nothing else can touch it
    public override void TakeDamage(int damage, IAudioService audio) {}

    public override void Draw()
    {
        Sprite.Draw(Tinted(bodyColor));
    }

    private void SetTarget(Vector2 playerPos)
    {
        // A step in his direction, then the old random wobble on top of it
        Vector2 towards = playerPos - Sprite.Position;

        if (towards != Vector2.Zero)
            towards.Normalize();

        target = Sprite.Position
               + towards * DRIFT_TOWARDS
               + new Vector2(
                     Globals.R.Next(-targetOffset, targetOffset),
                     Globals.R.Next(-targetOffset, targetOffset)
                 );

        speed = Globals.R.Next(SPEED_MIN, SPEED_MAX);
        max_speed = speed * 1.2f;
    }
}
