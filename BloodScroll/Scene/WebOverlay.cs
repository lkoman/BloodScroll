using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE WEB STUCK TO THE PLAYER WHILE HE IS SLOWED
//
// The strands themselves are baked in WebTexture - this only decides how big
// the player's one is and how hard it is holding on to him.
//

public class WebOverlay
{
    // How far the web reaches past the player, as a share of the player size
    private const float RADIUS_SCALE = 0.6f;

    private Texture2D _web;

    public void LoadContent(float playerWidth, float playerHeight)
    {
        float radius = MathF.Max(playerWidth, playerHeight) * RADIUS_SCALE;
        _web = WebTexture.Get((int)MathF.Ceiling(radius * 2f));
    }

    // strength: 0 gone, 1 freshly caught
    public void Draw(Vector2 center, float strength)
    {
        strength = MathHelper.Clamp(strength, 0f, 1f);

        if (_web == null || strength <= 0f)
            return;

        Globals.SpriteBatch.Draw(
            _web,
            center,
            null,
            Color.White * strength,
            0f,
            new Vector2(_web.Width / 2f, _web.Height / 2f),
            1f,
            SpriteEffects.None,
            0f
        );
    }
}
