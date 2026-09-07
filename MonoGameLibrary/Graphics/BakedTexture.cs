using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

//
// SHAPES DRAWN IN CODE INSTEAD OF IN AN ATLAS
//
// Several things in this game have no artwork at all - the bullets, the webs,
// the fireball, the shield bubble - and are worked out pixel by pixel instead.
// Every one of them used to carry its own copy of the same two things: a
// dictionary of textures it had already baked, and a pair of nested loops
// walking the pixels and measuring each one from the middle.
//
// Both live here now, so a shape is just its own formula and nothing else.
//
// WHY THEY ARE BAKED PER SIZE rather than scaled from one master: the world is
// drawn with PointClamp, so a small circle blown up comes out with a stair
// stepped rim. Each size gets its own bake.
//

//
// The alpha of one pixel, given where it sits relative to the middle of the
// texture. dx and dy are in pixels and radius is half the width, so a shape can
// work in pixels or in a 0..1 share of the radius, whichever suits it.
//
public delegate float AlphaAt(float dx, float dy, float radius);

public static class BakedTexture
{
    //
    // A SQUARE ALPHA MASK, `size` pixels across
    //
    // White everywhere, and the shape is entirely in the alpha - so whatever
    // draws it picks the colour, and one bake serves every tint. That is what
    // lets the same web be a spider's grey shot and the green spider's
    // fluorescent one.
    //
    public static Texture2D Mask(int size, AlphaAt alphaAt)
    {
        Texture2D texture = new(Core.GraphicsDevice, size, size);
        Color[] pixels = new Color[size * size];
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Measured from the MIDDLE of the pixel, so the shape comes out
                // centred rather than half a pixel off up and to the left
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;

                float alpha = MathHelper.Clamp(alphaAt(dx, dy, radius), 0f, 1f);

                // Premultiplied, the same way the UI shades itself with Color * alpha
                pixels[y * size + x] = Color.White * alpha;
            }
        }

        texture.SetData(pixels);
        return texture;
    }
}

//
// ONE TEXTURE PER SIZE, BAKED ON FIRST USE
//
// Every shape in the game is asked for at a handful of sizes at most - four
// guns and five shooting mobs share about six bullet sizes between them - so
// these fill up in the first fight and never grow again.
//
public class TextureCache(Func<int, Texture2D> bake)
{
    private readonly Dictionary<int, Texture2D> baked = [];

    public Texture2D Get(int size)
    {
        if (baked.TryGetValue(size, out Texture2D texture))
            return texture;

        texture = bake(size);
        baked[size] = texture;

        return texture;
    }
}
