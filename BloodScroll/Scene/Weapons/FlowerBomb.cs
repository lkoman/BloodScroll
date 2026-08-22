using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// THE FLOWER BOMB
//
// A flower head pulled off its stalk (see Flower.OfferPluck) and put back down
// somewhere else. Three seconds later it goes off.
//
// It STAYS EXACTLY WHERE IT WAS PUT. No gravity, no rolling: one placed in mid
// air hangs there. That is what makes it a tool rather than a grenade - the
// player chooses the spot, including a spot in the air a bat is about to fly
// through, and the whole skill of it is knowing where things will be in three
// seconds' time.
//
// IT HURTS THE PLAYER TOO. It has to: a bomb that only ever hurt what the
// player wanted it to would be a free screen clear on a one flower cooldown,
// and he already paid the real price for it by standing on the flower's ledge
// to pluck it. Three seconds is a long time to get out of the way - not being
// somewhere else by then is the mistake being punished.
//

public class FlowerBomb : IDrawableLayer
{
    // Under the player and the mobs - it is a thing lying on the floor
    public int DrawLayer { get; set; } = 25;

    public const float FUSE_SECONDS = 3f;

    // Hits a lot harder than a gun does. A whole flower's worth of HP went into
    // making it, and it only goes where the player physically walked.
    public const int DAMAGE = 300;
    public const float RADIUS = 240f;

    // What it costs him if he is still standing in it. Painful, survivable at
    // full health, and lethal if he was already hurt.
    public const int PLAYER_DAMAGE = 140;

    // Small enough to read as an object on the ground rather than as another mob
    private const float ART_SCALE = 0.8f;

    // How fast it blinks at the start and at the end. The flash speeding up is
    // the only warning the fuse gives, so the two are far apart on purpose.
    private const float SLOW_BLINK = 3f;   // flashes a second at three seconds left
    private const float FAST_BLINK = 20f;  // and at none left

    private readonly Sprite _bomb;
    private float fuse = FUSE_SECONDS;

    public bool FuseSpent => fuse <= 0f;

    public Vector2 Centre => _bomb.Position + new Vector2(_bomb.Width, _bomb.Height) * 0.5f;

    public FlowerBomb(Vector2 centre)
    {
        _bomb = Globals.Flower.CreateSprite("Flower_face");
        _bomb.Scale = new Vector2(ART_SCALE, ART_SCALE);

        _bomb.Position = centre - new Vector2(_bomb.Width, _bomb.Height) * 0.5f;
    }

    public void Update()
    {
        fuse -= Globals.DT;
    }

    public void Draw()
    {
        _bomb.Draw(FuseColour());
    }

    // Flashes between its own colour and red, faster the closer it gets. The
    // fuse doubles as the clock - it runs down at a fixed rate and the game has
    // no global one.
    private Color FuseColour()
    {
        float spent = 1f - MathHelper.Clamp(fuse / FUSE_SECONDS, 0f, 1f);
        float rate = MathHelper.Lerp(SLOW_BLINK, FAST_BLINK, spent);

        float pulse = 0.5f + 0.5f * MathF.Sin(fuse * rate);

        return Color.Lerp(Color.White, Globals.Red, pulse);
    }
}
