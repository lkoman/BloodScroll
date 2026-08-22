using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE YELLOW BUBBLE AROUND THE PLAYER
//
// How much shield is left decides nothing here except how bright the ring is
// drawn - the player owns the numbers, this owns the look.
//
// The ring is baked into a texture once, at the exact size it gets drawn at.
// The world is drawn with PointClamp, so blowing a small circle up would give
// a stair stepped edge instead of a smooth one.
//

public class ShieldAura
{
    // How far outside the player the ring sits, as a share of the player size
    private const float RADIUS_SCALE = 0.5f;

    // Where the bright ring sits in the texture (1 = the very rim) and how wide it is
    private const float RING_CENTER = 0.86f;
    private const float RING_WIDTH = 0.13f;

    // The faint fill inside the ring, strongest just under it
    private const float FILL_ALPHA = 0.20f;

    // How opaque the bubble gets at a full shield. Everything below that is
    // scaled straight down with the shield, so half a shield is half of this.
    private const float MAX_OPACITY = 0.5f;

    private Texture2D _bubble;

    public void LoadContent(float playerWidth, float playerHeight)
    {
        float radius = MathF.Max(playerWidth, playerHeight) * RADIUS_SCALE;
        _bubble = CreateBubble((int)MathF.Ceiling(radius * 2f));
    }

    // strength: 0 nothing at all, 1 a full bright ring
    public void Draw(Vector2 center, float strength)
    {
        strength = MathHelper.Clamp(strength, 0f, 1f);

        if (_bubble == null || strength <= 0f)
            return;

        Globals.SpriteBatch.Draw(
            _bubble,
            center,
            null,
            Globals.Yellow * (strength * MAX_OPACITY),
            0f,
            new Vector2(_bubble.Width / 2f, _bubble.Height / 2f),
            1f,
            SpriteEffects.None,
            0f
        );
    }

    private static Texture2D CreateBubble(int size)
    {
        Texture2D texture = new(Core.GraphicsDevice, size, size);
        Color[] pixels = new Color[size * size];
        float radius = size / 2f;

        for (int y = 0; y < size; y ++)
        {
            for (int x = 0; x < size; x ++)
            {
                // Distance from the middle of the texture: 0 in the centre, 1 on the rim
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float d = MathF.Sqrt(dx * dx + dy * dy) / radius;

                // Fades out on both sides of RING_CENTER, so the ring has no hard edge
                float ring = MathHelper.Clamp(1f - MathF.Abs(d - RING_CENTER) / RING_WIDTH, 0f, 1f);

                // Barely there in the middle so the player stays readable through it
                float fill = d <= RING_CENTER ? FILL_ALPHA * d * d : 0f;

                float alpha = MathHelper.Clamp(ring + fill, 0f, 1f);

                // Premultiplied, the same way the UI shades itself with Color * alpha
                pixels[y * size + x] = Color.White * alpha;
            }
        }

        texture.SetData(pixels);
        return texture;
    }
}
