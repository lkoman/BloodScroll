using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE FIREBALL, BAKED PIXEL BY PIXEL
//
// There is no explosion art in any of the atlas, and the same trick the shield
// bubble and the webs use works here: bake the shape once, at the size it is
// drawn at, because the world is drawn with PointClamp and blowing a small
// circle up gives a stair stepped edge.
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

    // One texture per size. There are only ever two - the shell gun's blast
    // and the flower bomb's - so this fills up once and never grows again.
    private static readonly Dictionary<int, Texture2D> baked = [];

    // size is how wide the blast comes out at full stretch, in pixels
    public static Texture2D Get(int size)
    {
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
                // 0 in the middle of the texture, 1 on the rim
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float d = MathF.Sqrt(dx * dx + dy * dy) / radius;

                if (d > 1f)
                    continue;

                // Solid to the edge of the core, then falling away to nothing
                float body = d <= CORE ? 1f : 1f - (d - CORE) / (1f - CORE);

                float rim = MathHelper.Clamp(1f - MathF.Abs(d - RIM_CENTER) / RIM_WIDTH, 0f, 1f);

                float alpha = MathHelper.Clamp(MathF.Max(body * body, rim), 0f, 1f);

                pixels[y * size + x] = Color.White * alpha;
            }
        }

        texture.SetData(pixels);
        return texture;
    }
}
