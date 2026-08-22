using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// GOLD DUST - what the butterfly leaves behind
//
// The butterfly used to burst into a hand drawn cloud, which was a picture of
// an explosion and read like one: the thing in the game that HEALS you looked
// exactly like the two things that blow you up. So it bursts into dust instead.
//
// WHY DUST AND WHY GOLD
//
// The blast (see Explosion) is one hard circle that arrives all at once, and
// that is right for something that hits you. This is the opposite shape on
// purpose - a hundred small motes thrown outwards, slowing, drifting UP as they
// fade. Nothing else in the game rises. Between the direction and the gold, the
// butterfly going off cannot be mistaken for a jellyfish going off even out of
// the corner of an eye, which is the entire point of the pair.
//
// IT IS ONLY A PICTURE. There is no hitbox here and nothing in this file deals
// damage or healing to anybody - the butterfly hands the player his health
// itself, on the frame the dust appears, and then this just plays out. See
// Butterfly.Burst.
//
// THE RANDOM STREAM
//
// This uses its OWN Random and must keep doing so. Globals.R is the seeded
// stream the whole world is generated from, and the order of draws from it is
// load bearing (see MobManager.SpawnOrder) - a puff of dust is spawned whenever
// the player happens to walk into a butterfly, so taking even one number from
// that stream would change every layer generated above it.
//

public class GoldDust : IDrawableLayer
{
    // In front of the mobs and the player, same as a blast
    public int DrawLayer { get; set; } = 45;

    private const int MOTES = 110;
    private const float SECONDS = 1.2f;

    // How hard it is thrown outwards. A wide band, so the cloud has a dense
    // heart and a thin scatter well beyond it rather than one clean ring.
    private const float SPEED_MIN = 120f;
    private const float SPEED_MAX = 720f;

    // What is left of a mote's speed after a second. Well under 1, so the throw
    // is over almost immediately and the rest of its life is the drift.
    private const float DRAG_PER_SECOND = 0.02f;

    // Upwards is negative. Gentle - the motes only start climbing once the drag
    // has taken the throw out of them, which is what makes the cloud hang.
    private const float RISE = -240f;

    private const int SIZE_MIN = 3;
    private const int SIZE_MAX = 9;

    // Two golds and a spark of near white. Warm all the way through: this is
    // the only thing in the game that is good news, and it is the only thing
    // drawn in gold.
    private static readonly Color DEEP = new(214, 158, 38);
    private static readonly Color BRIGHT = new(255, 216, 92);
    private static readonly Color SPARK = new(255, 248, 214);

    private struct Mote
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Colour;
        public int Size;

        // Its own share of the effect's life, so the cloud thins out unevenly
        // instead of every mote switching off on the same frame
        public float Life;
    }

    private readonly Mote[] motes = new Mote[MOTES];
    private float timer = 0f;

    public bool Finished => timer >= SECONDS;

    // NOT Globals.R - see the note at the top of the file
    private static readonly Random rng = new();

    public GoldDust(Vector2 centre)
    {
        for (int i = 0; i < MOTES; i++)
        {
            float angle = (float)rng.NextDouble() * MathF.Tau;
            float speed = MathHelper.Lerp(SPEED_MIN, SPEED_MAX, (float)rng.NextDouble());

            // Squared, so most motes are small and the few big ones stand out
            float roll = (float)rng.NextDouble();

            motes[i] = new Mote
            {
                Position = centre,
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Size = (int)MathHelper.Lerp(SIZE_MAX, SIZE_MIN, roll * roll),
                Colour = PickColour(),
                Life = MathHelper.Lerp(SECONDS * 0.45f, SECONDS, (float)rng.NextDouble())
            };
        }
    }

    // Mostly the two golds, with about one mote in eight catching the light
    private static Color PickColour()
    {
        if (rng.Next(8) == 0)
            return SPARK;

        return Color.Lerp(DEEP, BRIGHT, (float)rng.NextDouble());
    }

    public void Update()
    {
        timer += Globals.DT;

        float drag = MathF.Pow(DRAG_PER_SECOND, Globals.DT);

        for (int i = 0; i < motes.Length; i++)
        {
            motes[i].Velocity *= drag;
            motes[i].Velocity.Y += RISE * Globals.DT;
            motes[i].Position += motes[i].Velocity * Globals.DT;
        }
    }

    public void Draw()
    {
        for (int i = 0; i < motes.Length; i++)
        {
            Mote mote = motes[i];

            float t = timer / mote.Life;

            if (t >= 1f)
                continue;

            // Holds its brightness and then goes quickly, so the cloud reads as
            // gold for most of its life rather than as a grey smear
            float alpha = 1f - t * t * t;

            // Shrinking as well as fading. Rounded to whole pixels because the
            // world is drawn with PointClamp and a scaled up disc comes out
            // with a stair stepped rim - so each size is baked on its own.
            int size = (int)MathF.Round(mote.Size * (1f - t * 0.6f));

            if (size < 2)
                continue;

            Texture2D disc = BulletTexture.Get(size);

            Globals.SpriteBatch.Draw(
                disc,
                mote.Position,
                null,
                mote.Colour * alpha,
                0f,
                new Vector2(disc.Width / 2f, disc.Height / 2f),
                1f,
                SpriteEffects.None,
                0f
            );
        }
    }
}
