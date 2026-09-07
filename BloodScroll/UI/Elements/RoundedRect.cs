using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// ROUNDED RECTANGLES, DRAWN OUT OF ONE BAKED CORNER
//
// Every panel, button, bar and chip in the UI is the same shape.
//
// NOT baked per rectangle - a bar changes width every frame and would fill
// memory with hundreds of near identical textures. What is baked is one DISC
// PER RADIUS: its four quadrants are the four corners, and the flats between
// them are the plain white pixel stretched. About six radii exist in the whole
// game, so this bakes six textures and never allocates again.
//
// The disc carries the antialiasing at its rim.
//

public static class RoundedRect
{
    // The stretched flat parts, and anything else that wants a plain rectangle
    private static Texture2D _pixel;

    // One filled disc per radius, and one ring per radius plus thickness
    private static readonly Dictionary<int, Texture2D> _discs = [];
    private static readonly Dictionary<int, Texture2D> _rings = [];

    // How many bands a gradient is painted in. Enough that the steps are below
    // the eye's threshold on a 96px button, few enough to stay cheap.
    private const int GRADIENT_BANDS = 22;

    public static Texture2D Pixel
    {
        get
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
                _pixel.SetData([Color.White]);
            }
            return _pixel;
        }
    }

    // A plain square rectangle. Here so nothing else has to keep its own pixel.
    public static void Rect(Rectangle r, Color colour)
        => Globals.SpriteBatch.Draw(Pixel, r, colour);

    //
    // A SOLID ROUNDED RECTANGLE
    //
    public static void Fill(Rectangle r, Color colour, int radius)
    {
        radius = ClampRadius(r, radius);

        if (radius <= 0)
        {
            Rect(r, colour);
            return;
        }

        Texture2D disc = Disc(radius);

        // The four corners, one quadrant of the disc each
        Globals.SpriteBatch.Draw(disc, new Rectangle(r.X, r.Y, radius, radius),
            new Rectangle(0, 0, radius, radius), colour);

        Globals.SpriteBatch.Draw(disc, new Rectangle(r.Right - radius, r.Y, radius, radius),
            new Rectangle(radius, 0, radius, radius), colour);

        Globals.SpriteBatch.Draw(disc, new Rectangle(r.X, r.Bottom - radius, radius, radius),
            new Rectangle(0, radius, radius, radius), colour);

        Globals.SpriteBatch.Draw(disc, new Rectangle(r.Right - radius, r.Bottom - radius, radius, radius),
            new Rectangle(radius, radius, radius, radius), colour);

        // The flat parts: a strip above and below between the corners, and the
        // whole width through the middle
        Rect(new Rectangle(r.X + radius, r.Y, r.Width - radius * 2, radius), colour);
        Rect(new Rectangle(r.X, r.Y + radius, r.Width, r.Height - radius * 2), colour);
        Rect(new Rectangle(r.X + radius, r.Bottom - radius, r.Width - radius * 2, radius), colour);
    }

    //
    // THE SAME SHAPE, SHADED TOP TO BOTTOM
    //
    // The bottom colour is laid down first so the rounded rim is antialiased
    // against the background exactly once, then the lighter top is painted over
    // it in horizontal bands.
    //
    // Each band is inset by however far the corner has eaten into that row, so
    // the gradient follows the corner instead of squaring it off.
    //
    public static void FillGradient(Rectangle r, Color top, Color bottom, int radius)
    {
        radius = ClampRadius(r, radius);

        Fill(r, bottom, radius);

        if (r.Height <= 0)
            return;

        int bands = Math.Min(GRADIENT_BANDS, r.Height);
        float bandHeight = r.Height / (float)bands;

        for (int i = 0; i < bands; i++)
        {
            int y = r.Y + (int)(i * bandHeight);
            int h = (int)Math.Ceiling(bandHeight);

            // Sampled in the middle of the band rather than at its top edge,
            // so the ramp is centred on the rectangle and not half a band high
            float t = (i + 0.5f) / bands;
            Color colour = Color.Lerp(top, bottom, t);

            int inset = HorizontalInset(y + h / 2f, r, radius);

            Rect(new Rectangle(r.X + inset, y, r.Width - inset * 2, h), colour);
        }
    }

    //
    // AN OUTLINE OF THE SAME SHAPE, DRAWN INSIDE THE RECTANGLE
    //
    public static void Border(Rectangle r, Color colour, int radius, int thickness = 2)
    {
        radius = ClampRadius(r, radius);
        thickness = Math.Max(1, Math.Min(thickness, radius <= 0 ? int.MaxValue : radius));

        if (radius <= 0)
        {
            Rect(new Rectangle(r.X, r.Y, r.Width, thickness), colour);
            Rect(new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), colour);
            Rect(new Rectangle(r.X, r.Y, thickness, r.Height), colour);
            Rect(new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), colour);
            return;
        }

        Texture2D ring = Ring(radius, thickness);

        Globals.SpriteBatch.Draw(ring, new Rectangle(r.X, r.Y, radius, radius),
            new Rectangle(0, 0, radius, radius), colour);

        Globals.SpriteBatch.Draw(ring, new Rectangle(r.Right - radius, r.Y, radius, radius),
            new Rectangle(radius, 0, radius, radius), colour);

        Globals.SpriteBatch.Draw(ring, new Rectangle(r.X, r.Bottom - radius, radius, radius),
            new Rectangle(0, radius, radius, radius), colour);

        Globals.SpriteBatch.Draw(ring, new Rectangle(r.Right - radius, r.Bottom - radius, radius, radius),
            new Rectangle(radius, radius, radius, radius), colour);

        // The straight runs between the corner arcs
        Rect(new Rectangle(r.X + radius, r.Y, r.Width - radius * 2, thickness), colour);
        Rect(new Rectangle(r.X + radius, r.Bottom - thickness, r.Width - radius * 2, thickness), colour);
        Rect(new Rectangle(r.X, r.Y + radius, thickness, r.Height - radius * 2), colour);
        Rect(new Rectangle(r.Right - thickness, r.Y + radius, thickness, r.Height - radius * 2), colour);
    }

    //
    // A SOFT EDGE AROUND THE RECTANGLE
    //
    // Both ways round: black under a panel is the drop shadow, the accent colour
    // behind a hovered button is the glow.
    //
    // The same fill drawn a handful of times, each a little larger and fainter.
    //
    public static void Glow(Rectangle r, Color colour, int radius, int spread, float strength = 0.16f)
    {
        for (int i = spread; i > 0; i--)
        {
            // Faintest at the outside, strongest hard against the rectangle
            float t = 1f - i / (float)spread;
            float alpha = strength * (0.35f + 0.65f * t);

            Rectangle grown = new(r.X - i, r.Y - i, r.Width + i * 2, r.Height + i * 2);

            Fill(grown, colour * alpha, radius + i);
        }
    }

    // How far the rounded corner has bitten into the row at height y. Zero
    // everywhere between the two corner bands, which is most of a panel.
    private static int HorizontalInset(float y, Rectangle r, int radius)
    {
        if (radius <= 0)
            return 0;

        // Distance from the row into the corner band, measured downwards from
        // the top edge and upwards from the bottom one
        float depth = -1f;

        if (y < r.Y + radius)
            depth = y - r.Y;
        else if (y > r.Bottom - radius)
            depth = r.Bottom - y;

        if (depth < 0f)
            return 0;

        // The corner is a quarter circle: at depth d below the top of the arc,
        // the shape starts radius - sqrt(r^2 - (r - d)^2) in from the edge
        float dy = radius - depth;
        float dx = MathF.Sqrt(MathF.Max(0f, radius * radius - dy * dy));

        return (int)MathF.Ceiling(radius - dx);
    }

    private static int ClampRadius(Rectangle r, int radius)
        => Math.Max(0, Math.Min(radius, Math.Min(r.Width, r.Height) / 2));

    // THE BAKES. A disc of diameter 2r, and a ring of the same size t pixels
    // thick. One pixel of softness at each rim, same as the bullets.
    private static Texture2D Disc(int radius)
    {
        if (_discs.TryGetValue(radius, out Texture2D cached))
            return cached;

        int size = radius * 2;
        Texture2D texture = new(Core.GraphicsDevice, size, size);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                pixels[y * size + x] = Color.White * MathHelper.Clamp(radius - dist + 0.5f, 0f, 1f);
            }
        }

        texture.SetData(pixels);
        _discs[radius] = texture;

        return texture;
    }

    private static Texture2D Ring(int radius, int thickness)
    {
        // Both fit comfortably inside a short: radii are tens of pixels and
        // borders are single digits
        int key = radius * 1000 + thickness;

        if (_rings.TryGetValue(key, out Texture2D cached))
            return cached;

        int size = radius * 2;
        Texture2D texture = new(Core.GraphicsDevice, size, size);
        Color[] pixels = new Color[size * size];
        float inner = radius - thickness;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                // Inside the outer rim AND outside the inner one
                float outside = MathHelper.Clamp(radius - dist + 0.5f, 0f, 1f);
                float inside = MathHelper.Clamp(dist - inner + 0.5f, 0f, 1f);

                pixels[y * size + x] = Color.White * MathF.Min(outside, inside);
            }
        }

        texture.SetData(pixels);
        _rings[key] = texture;

        return texture;
    }
}
