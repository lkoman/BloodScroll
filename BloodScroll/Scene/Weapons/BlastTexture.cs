using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// THE FIREBALL, BAKED PIXEL BY PIXEL
//
// There is no explosion art in any of the atlas, so the shape is worked out
// here instead - see BakedTexture, which owns the cache and the pixel walk.
//
// What is baked is a disc, not a ring: bright and solid in the middle, thinning
// out towards the rim. The blast grows and fades as it is drawn (see Explosion),
// so the texture only has to describe its SHAPE, never its size or its life.
//

public static class BlastTexture
{
    // How far out the solid core reaches, as a share of the radius. Inside
    // this the disc is at full strength - that is the part that reads as fire
    // rather than as smoke.
    private const float CORE = 0.45f;

    // A bright edge riding the rim, so the blast has an outline to read
    // against a busy background instead of a soft blur
    private const float RIM_CENTER = 0.93f;
    private const float RIM_WIDTH = 0.11f;

    private static readonly TextureCache cache = new(Bake);

    // size is how wide the blast comes out at full stretch, in pixels
    public static Texture2D Get(int size) => cache.Get(size);

    private static Texture2D Bake(int size) =>
        BakedTexture.Mask(size, (dx, dy, radius) =>
        {
            // 0 in the middle of the texture, 1 on the rim
            float d = MathF.Sqrt(dx * dx + dy * dy) / radius;

            // Nothing outside the circle. The rim band alone would otherwise
            // reach a little past it and leave a stray ring in the corners.
            if (d > 1f)
                return 0f;

            // Solid to the edge of the core, then falling away to nothing
            float body = d <= CORE ? 1f : 1f - (d - CORE) / (1f - CORE);

            float rim = MathHelper.Clamp(1f - MathF.Abs(d - RIM_CENTER) / RIM_WIDTH, 0f, 1f);

            return MathF.Max(body * body, rim);
        });
}
