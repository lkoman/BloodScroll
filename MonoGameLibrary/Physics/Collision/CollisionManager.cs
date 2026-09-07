using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System;

namespace MonoGameLibrary;

public static class CollisionManager
{
    // BOUNDING CIRCLE
    //
    // One overload each: AnimatedSprite IS a Sprite, so a second copy taking
    // that type was the same body compiled twice.
    public static Circle SetBoundingCircle(Sprite sprite)
    {
        return new Circle(
            (int)(sprite.Position.X + (sprite.Width * 0.5f)),
            (int)(sprite.Position.Y + (sprite.Height * 0.5f)),
            (int)(sprite.Width * 0.5f));
    }

    public static Circle UpdateBoundingCircle(Circle bounds, Sprite sprite)
    {
        bounds.SetPosition(
            (int)(sprite.Position.X + (sprite.Width * 0.5f)),
            (int)(sprite.Position.Y + (sprite.Height * 0.5f))
        );
        return bounds;
    }

    // SET / UPDATE A SHRUNKEN BOUNDING RECTANGLE
    //
    // Sprites carry a lot of empty space - the bat is 192x128 and most of that
    // is wing and air. Using the whole frame as the hitbox is what makes hits
    // feel unfair. Scale shrinks the box towards the middle of the sprite:
    // (0.6f, 0.7f) keeps 60% of the width and 70% of the height, centred.
    public static Rectangle SetBoundingRectangle(Sprite sprite, Vector2 scale)
    {
        return ScaleAroundCentre(SetBoundingRectangle(sprite), scale);
    }

    public static Rectangle UpdateBoundingRectangle(Rectangle bounds, Sprite sprite, Vector2 scale)
    {
        return ScaleAroundCentre(UpdateBoundingRectangle(bounds, sprite), scale);
    }

    private static Rectangle ScaleAroundCentre(Rectangle full, Vector2 scale)
    {
        int width = (int)(full.Width * scale.X);
        int height = (int)(full.Height * scale.Y);

        return new Rectangle(
            full.X + (full.Width - width) / 2,
            full.Y + (full.Height - height) / 2,
            width,
            height);
    }

    // THE WHOLE SPRITE FRAME AS A BOX
    public static Rectangle SetBoundingRectangle(Sprite sprite)
    {
        return new Rectangle(
            (int)sprite.Position.X,
            (int)sprite.Position.Y,
            (int)sprite.Width,
            (int)sprite.Height);
    }

    // Size as well as position, otherwise a sprite that swaps to a bigger
    // animation keeps its old box
    public static Rectangle UpdateBoundingRectangle(Rectangle bounds, Sprite sprite)
    {
        bounds.X = (int)sprite.Position.X;
        bounds.Y = (int)sprite.Position.Y;
        bounds.Width = (int)sprite.Width;
        bounds.Height = (int)sprite.Height;
        return bounds;
    }

    // COLLISION CIRCLE - RECTANGLE
    public static bool CircleIntersectsRectangle(Circle c, Rectangle r)
    {
        int closestX = Math.Clamp(c.X, r.X, r.X + r.Width);
        int closestY = Math.Clamp(c.Y, r.Y, r.Y + r.Height);

        int dx = c.X - closestX;
        int dy = c.Y - closestY;

        return dx * dx + dy * dy <= c.Radius * c.Radius;
    }
}