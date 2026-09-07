using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

public class Sprite 
{
    public TextureRegion Region { get; set; }
    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Bottom => Position.Y + Height;
    public float Top => Position.Y;
    public Color Color { get; set; } = Color.White;
    public float Rotation { get; set; } = 0.0f;
    public Vector2 Scale { get; set; } = Vector2.One;
    public Vector2 Origin { get; set; } = Vector2.Zero;
    public SpriteEffects Effects { get; set; } = SpriteEffects.None;
    public float LayerDepth { get; set; } = 0.0f;
    public float Width => Region.Width * Scale.X;
    public float Height => Region.Height * Scale.Y;

    public Sprite() { }

    public Sprite(TextureRegion region)
    {
        Region = region;
    }

    public void Draw()
    {
        Region.Draw(Position, Color, Rotation, Origin, Scale, Effects, LayerDepth);
    }

    public void Draw(Color c)
    {
        Region.Draw(Position, c, Rotation, Origin, Scale, Effects, LayerDepth);
    }

    public void Draw(float angle, Vector2 origin)
    {
        Region.Draw(Position, Color, angle, origin, Scale, Effects, LayerDepth);
    }

    // Turns the sprite about the middle of its frame WITHOUT moving it - Position
    // still means the top left corner, which every hitbox assumes.
    //
    // SpriteBatch puts the ORIGIN at the position it is given, so the position is
    // pushed to the middle by exactly as much as the origin pulls it back.
    public void DrawRotated(float rotation, Color color)
    {
        if (rotation == 0f)
        {
            Draw(color);
            return;
        }

        Vector2 middle = new(Region.Width * 0.5f, Region.Height * 0.5f);

        Region.Draw(Position + middle * Scale, color, rotation, middle, Scale, Effects, LayerDepth);
    }
}
