using System;
using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE BLADE, BAKED PIXEL BY PIXEL
//
// No sword in the weapons atlas, so it is baked at the size it is drawn at -
// the world uses PointClamp and scaling would come out stair stepped.
//
// Baked LYING ALONG +X: pommel at the left edge, tip at the right, spine on the
// middle row. Whatever draws it only has to turn it about (0, height / 2), the
// same origin the gun uses.
//
// UNLIKE the other baked textures this one carries real COLOUR, not an alpha
// mask - a sword is three materials in a row and one tint cannot say that. The
// tint is still used for the fade and the trail ghosts.
//
// HARD EDGES, NO SMOOTHING - everything else on screen is pixel art.
//

public static class SwordTexture
{
    // WHERE ONE PART ENDS AND THE NEXT BEGINS, as a share of the whole length.
    // The hand is at the very back, so the blade is most of it - a sword that
    // reaches 120 pixels has to be a blade for something like a hundred of them.
    private const float POMMEL_END = 0.035f;
    private const float GRIP_END = 0.14f;
    private const float GUARD_END = 0.19f;

    // How far along the BLADE the taper to a point starts, 0 at the guard and
    // 1 at the tip. Straight sided for most of its length and then a point,
    // which is what a sword looks like - a triangle from guard to tip reads as
    // a spike.
    private const float TIP_START = 0.72f;

    // HALF WIDTHS, also as a share of the length, so the whole thing scales
    // together if the reach is ever changed
    private const float POMMEL_HALF = 0.050f;
    private const float GRIP_HALF = 0.028f;
    private const float GUARD_HALF = 0.105f;
    private const float BLADE_HALF = 0.055f;

    // How wide the bright edge down each side of the blade is, and the darker
    // groove down its middle. Both in pixels - they are highlights, not parts
    // of the shape, and they have to stay a couple of pixels wide at any size.
    private const float EDGE_PIXELS = 1.6f;
    private const float FULLER_PIXELS = 1.4f;

    // THE MATERIALS. Cold, so the sword never reads as one of the two warm guns
    // (the rifle's cream, the shell's orange) out of the corner of the eye.
    private static readonly Color POMMEL = new(150, 132, 96);
    private static readonly Color GRIP = new(64, 44, 38);
    private static readonly Color GUARD = new(150, 132, 96);
    private static readonly Color BLADE = new(176, 194, 210);
    private static readonly Color EDGE = new(238, 246, 252);
    private static readonly Color FULLER = new(120, 140, 158);

    // One texture per length. There is only ever one sword, so this fills up
    // on the first swing of the first run and never grows again.
    //
    // Only the CACHE is shared with the round shapes - this one is neither
    // square nor an alpha mask, so it walks its own pixels below.
    private static readonly TextureCache cache = new(Bake);

    // length is how far the sword reaches from the hand, in pixels
    public static Texture2D Get(int length) => cache.Get(length);

    private static Texture2D Bake(int length)
    {
        // Wide enough for the guard, which is the widest part, plus a row of
        // daylight either side so nothing is clipped by the texture edge
        int height = (int)MathF.Ceiling(length * MathF.Max(GUARD_HALF, POMMEL_HALF) * 2f) + 2;

        // Odd heights put the spine on a whole row rather than between two,
        // which keeps a blade aimed straight along an axis from looking bent
        if (height % 2 == 0)
            height++;

        Texture2D texture = new(Core.GraphicsDevice, length, height);
        Color[] pixels = new Color[length * height];

        float middle = height / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < length; x++)
            {
                // 0 at the pommel, 1 at the tip; and how far this pixel is off
                // the spine, in pixels, whichever side of it it lies
                float along = (x + 0.5f) / length;
                float across = MathF.Abs(y + 0.5f - middle);

                Color? paint = Paint(along, across, length);

                if (paint.HasValue)
                    pixels[y * length + x] = paint.Value;
            }
        }

        texture.SetData(pixels);
        return texture;
    }

    // What this pixel is made of, or null for the air around the sword
    private static Color? Paint(float along, float across, int length)
    {
        if (along < POMMEL_END)
            return across <= POMMEL_HALF * length ? POMMEL : null;

        if (along < GRIP_END)
            return across <= GRIP_HALF * length ? GRIP : null;

        if (along < GUARD_END)
            return across <= GUARD_HALF * length ? GUARD : null;

        return Steel(along, across, length);
    }

    // THE BLADE: a straight sided bar that comes to a point, with a bright edge
    // riding each side and a groove down the middle. The two highlights are
    // what stop it reading as a flat grey stick against a dark background.
    private static Color? Steel(float along, float across, int length)
    {
        float half = BladeHalf(along) * length;

        if (across > half)
            return null;

        // The last pixel and a half of steel on each side, which is the part
        // that catches the light on a real blade
        if (across > half - EDGE_PIXELS)
            return EDGE;

        if (across < FULLER_PIXELS)
            return FULLER;

        return BLADE;
    }

    // Half the width of the blade at this point along it, as a share of the
    // length. Flat to TIP_START and then closing to nothing.
    private static float BladeHalf(float along)
    {
        float t = (along - GUARD_END) / (1f - GUARD_END);

        if (t < TIP_START)
            return BLADE_HALF;

        return MathHelper.Lerp(BLADE_HALF, 0f, (t - TIP_START) / (1f - TIP_START));
    }
}
