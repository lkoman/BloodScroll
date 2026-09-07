using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// A patch of web on the ground. NO DAMAGE - standing in one just slows the
// player.
//
// The same baked orb web the spiders spit (see WebTexture), only bigger.
//
// Owned and drawn by the Spider Queen rather than being a mob of its own, so it
// never has to be shot, killed or counted.
//

public class Web
{
    // How wide a patch comes out, in pixels
    private const int SIZE = 140;

    // The web is round but the hitbox is a box, so the box is pulled in off the
    // corners - otherwise the patch would catch the player on empty pixels.
    // What is left is about as wide as the old placeholder patch, so the fight
    // is no easier for the new art.
    private const float GRAB = 0.8f;

    private readonly Texture2D _web;
    private readonly Vector2 _center;

    public Rectangle Bounds { get; private set; }

    private float life;
    public bool Expired => life <= 0f;

    // position is the MIDDLE of the patch
    public Web(Vector2 position, float lifeSeconds)
    {
        _web = WebTexture.Get(SIZE);
        _center = position;

        int grab = (int)(SIZE * GRAB);
        Bounds = new Rectangle(
            (int)(position.X - grab / 2f),
            (int)(position.Y - grab / 2f),
            grab,
            grab
        );

        life = lifeSeconds;
    }

    public void Update()
    {
        life -= Globals.DT;
    }

    public void Draw()
    {
        // Fades out over its last second so it does not just blink away
        float alpha = MathHelper.Clamp(life, 0f, 1f) * 0.75f;

        Globals.SpriteBatch.Draw(
            _web,
            _center,
            null,
            Color.White * alpha,
            0f,
            new Vector2(_web.Width / 2f, _web.Height / 2f),
            1f,
            SpriteEffects.None,
            0f
        );
    }
}
