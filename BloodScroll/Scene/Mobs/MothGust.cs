using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE MOTH'S WING BLAST
//
// Two curved white lines of shoved air, as wide as the moth itself. There is
// no art for this anywhere in the atlases, so it is baked here the same way
// the shield bubble and the web overlay are: once, at the size it is drawn at,
// because the world is drawn with PointClamp and scaling would chew up lines
// this thin.
//
// The texture is drawn travelling to the RIGHT, and then simply turned to
// point wherever the moth blew it, so one picture covers every direction.
//
// Owned and driven by the moth rather than being a mob or a bullet of its own.
// The moth keeps the damage numbers; this keeps the shape and the flight.
//

public class MothGust
{
    // Both lines are arcs of circles centred this far BEHIND the gust, which is
    // what gives them their shallow, wind swept bow. 1 is half the texture.
    private const float CURVE = 1.1f;

    // How far in front of the middle each line sits
    private static readonly float[] Lines = [0.35f, 0.85f];

    private const float SPREAD = 0.5f;      // half the wedge the lines cover, radians
    private const float TAPER = 0.18f;      // how much of that wedge fades out at the tips
    private const float STRAND = 3f;        // half thickness of a line, in pixels

    private const float SPEED = 700f;
    private const float LIFE_SECONDS = 0.7f;
    private const float GROWTH = 0.6f;      // how much wider the air spreads as it goes

    // Where the lines themselves are, measured from the middle of the texture,
    // and how much of that counts as being hit
    private const float FRONT = 0.30f;
    private const float REACH = 0.40f;

    // One moth at a time in a fight, but a new one every boss layer. Baking the
    // same picture again for each of them would leak a texture per fight.
    private static Texture2D _shared;
    private static int _sharedSize;

    private float size;

    private Vector2 position;   // the middle of the puff, in world space
    private Vector2 direction = Vector2.UnitX;
    private float life = 0f;
    private bool spent = true;  // already caught the player, or never fired

    public bool InFlight => life > 0f;
    public Vector2 Direction => direction;

    // Sized off the moth, because the whole point is that the blast is as big
    // as the thing that made it
    public void LoadContent(float mothWidth)
    {
        size = mothWidth;

        int pixels = (int)MathF.Ceiling(size);

        if (_shared == null || _sharedSize != pixels)
        {
            _shared = CreateGust(pixels);
            _sharedSize = pixels;
        }
    }

    public void Release(Vector2 from, Vector2 towards)
    {
        direction = towards - from;

        if (direction.LengthSquared() < 0.0001f)
            direction = Vector2.UnitX;

        direction.Normalize();

        // Starts just clear of the wings, so it reads as leaving the moth
        // instead of appearing on top of it
        position = from + direction * (size * 0.35f);

        life = LIFE_SECONDS;
        spent = false;
    }

    public void Update()
    {
        if (!InFlight)
            return;

        life -= Globals.DT;
        position += direction * SPEED * Globals.DT;
    }

    // True on the single frame it catches him. The lines are the FRONT of the
    // gust, so what does the hitting sits ahead of the middle of the texture.
    public bool CaughtPlayer(IPlayer player)
    {
        if (!InFlight || spent)
            return false;

        Vector2 middle = position + direction * (size * FRONT);

        Circle air = new((int)middle.X, (int)middle.Y, (int)(size * REACH));

        if (!player.HurtBox.Intersects(air))
            return false;

        // One hit per blast - it blows through him, it does not grind him down
        spent = true;
        return true;
    }

    public void Draw()
    {
        if (!InFlight || _shared == null)
            return;

        // 0 the moment it leaves the wings, 1 as it dies
        float age = 1f - life / LIFE_SECONDS;

        Globals.SpriteBatch.Draw(
            _shared,
            position,
            null,
            // Holds its brightness for most of the flight, then goes quickly
            Color.White * (1f - age * age),
            MathF.Atan2(direction.Y, direction.X),
            new Vector2(_shared.Width / 2f, _shared.Height / 2f),
            1f + GROWTH * age,
            SpriteEffects.None,
            0f
        );
    }

    private static Texture2D CreateGust(int size)
    {
        Texture2D texture = new(Core.GraphicsDevice, size, size);
        Color[] pixels = new Color[size * size];

        float half = size / 2f;
        float strand = STRAND / half;   // the line thickness in the same units as the rest

        for (int y = 0; y < size; y ++)
        {
            for (int x = 0; x < size; x ++)
            {
                // -1 to 1 across the texture, with +u pointing the way it travels
                float u = (x + 0.5f) / half - 1f;
                float v = (y + 0.5f) / half - 1f;

                // Measured from the centre of curvature sitting behind the gust
                float behind = u + CURVE;
                float distance = MathF.Sqrt(behind * behind + v * v);
                float angle = MathF.Atan2(v, behind);

                // Outside the wedge there is no line at all, and the last
                // stretch of it fades so each line ends in a point rather than
                // being cut off square
                float taper = MathHelper.Clamp((SPREAD - MathF.Abs(angle)) / TAPER, 0f, 1f);

                if (taper <= 0f)
                    continue;

                float alpha = 0f;
                foreach (float line in Lines)
                    alpha = MathF.Max(alpha, Falloff(MathF.Abs(distance - (CURVE + line)), strand));

                // Premultiplied, the same way the rest of the game shades with Color * alpha
                pixels[y * size + x] = Color.White * (alpha * taper);
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    // A pixel of softness either side of a line, so it is not jagged
    private static float Falloff(float distance, float strand)
    {
        return MathHelper.Clamp(1f - distance / strand, 0f, 1f);
    }
}
