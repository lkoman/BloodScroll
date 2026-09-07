using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

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
// The cache and the pixel walk are BakedTexture's - all that is left here is
// what a bullet actually looks like.
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

    private static readonly TextureCache cache = new(Bake);

    // size is how wide the disc comes out, in pixels
    public static Texture2D Get(int size) => cache.Get(Math.Max(MIN_SIZE, size));

    private static Texture2D Bake(int size) =>
        BakedTexture.Mask(size, (dx, dy, radius) =>
        {
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            // Solid all the way out and then one pixel of softness at the rim.
            // Without it a disc this small is a visibly jagged blob.
            return MathHelper.Clamp(radius - dist + 0.5f, 0f, 1f);
        });
}
