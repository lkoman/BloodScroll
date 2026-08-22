using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE SHOT ITSELF, BAKED PIXEL BY PIXEL
//
// Every bullet in the game used to be a little sprite out of the weapons atlas,
// and every one of them was the same handful of pixels in a different colour -
// too small to read as a shape, and hand drawn art nobody ever looked at. So
// they are drawn here instead: a plain filled disc, which is all a bullet has
// ever needed to be.
//
// Baked once per size and handed out, the same as the webs and the blast. The
// world is drawn with PointClamp, so a disc scaled up from one master texture
// comes out with a stair stepped rim - each size gets its own bake instead.
//
// A SHOT IS A DISC INSIDE A DISC. Bullet draws this twice: once at full width
// in a pale version of the shot's colour and once, smaller, in the colour
// itself. That is where the border comes from - there is no ring baked in here,
// because a ring baked in would have to wear the same tint as the middle.
//

public static class BulletTexture
{
    // Nothing sensible can be drawn smaller than this, and clamping here means
    // no caller has to think about it
    private const int MIN_SIZE = 2;

    // One texture per size. Four guns and five shooting mobs between them use
    // about half a dozen sizes, so this fills up in the first fight and never
    // grows again.
    private static readonly Dictionary<int, Texture2D> baked = [];

    // size is how wide the disc comes out, in pixels
    public static Texture2D Get(int size)
    {
        size = Math.Max(MIN_SIZE, size);

        if (baked.TryGetValue(size, out Texture2D texture))
            return texture;

        texture = Bake(size);
        baked[size] = texture;

        return texture;
    }

    private static Texture2D Bake(int size)
    {
        Texture2D texture = new(Core.GraphicsDevice, size, size);
        Color[] pixels = new Color[size * size];
        float radius = size / 2f;

        for (int y = 0; y < size; y ++)
        {
            for (int x = 0; x < size; x ++)
            {
                // Measured from the middle of the pixel, so the disc comes out
                // centred rather than half a pixel off up and to the left
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                // Solid all the way out and then one pixel of softness at the
                // rim. Without it a disc this small is a visibly jagged blob.
                float alpha = MathHelper.Clamp(radius - dist + 0.5f, 0f, 1f);

                pixels[y * size + x] = Color.White * alpha;
            }
        }

        texture.SetData(pixels);
        return texture;
    }
}
