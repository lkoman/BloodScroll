using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System;

namespace MonoGameLibrary;

public static class CollisionManager
{
    // SET / CREATE BOUNDING CIRCLE
    public static Circle SetBoundingCircle(AnimatedSprite animatedSprite)
    {
        return new Circle(
            (int)(animatedSprite.Position.X + (animatedSprite.Width * 0.5f)),
            (int)(animatedSprite.Position.Y + (animatedSprite.Height * 0.5f)),
            (int)(animatedSprite.Width * 0.5f));
    }
    public static Circle SetBoundingCircle(Sprite sprite)
    {
        return new Circle(
            (int)(sprite.Position.X + (sprite.Width * 0.5f)),
            (int)(sprite.Position.Y + (sprite.Height * 0.5f)),
            (int)(sprite.Width * 0.5f));
    }

    // UPDATE BOUNDING CIRCLE
    public static Circle UpdateBoundingCircle(Circle bounds, AnimatedSprite animatedSprite)
    {
        bounds.SetPosition(
            (int)(animatedSprite.Position.X + (animatedSprite.Width * 0.5f)),
            (int)(animatedSprite.Position.Y + (animatedSprite.Height * 0.5f))
        );
        return bounds;
    }
    public static Circle UpdateBoundingCircle(Circle bounds, Sprite sprite)
    {
        bounds.SetPosition(
            (int)(sprite.Position.X + (sprite.Width * 0.5f)),
            (int)(sprite.Position.Y + (sprite.Height * 0.5f))
        );
        return bounds;
    }

    // SET / CREATE BOUNDING RECTANGLE
    public static Rectangle SetBoundingRectangle(AnimatedSprite animatedSprite)
    {
        return new Rectangle(
            (int)animatedSprite.Position.X,
            (int)animatedSprite.Position.Y,
            (int)animatedSprite.Width,
            (int)animatedSprite.Height);
    }
    public static Rectangle SetBoundingRectangle(Sprite sprite)
    {
        return new Rectangle(
            (int)sprite.Position.X,
            (int)sprite.Position.Y,
            (int)sprite.Width,
            (int)sprite.Height);
    }

    // UPDATE BOUNDING RECTANGLE
    public static Rectangle UpdateBoundingRectangle(Rectangle bounds, float x, float y)
    {
        bounds.X = (int)x;
        bounds.Y = (int)y;
        return bounds;
    }
    public static Rectangle UpdateBoundingRectangle(Rectangle bounds, AnimatedSprite animatedSprite)
    {
        bounds.X = (int)animatedSprite.Position.X;
        bounds.Y = (int)animatedSprite.Position.Y;
        return bounds;
    }
    public static Rectangle UpdateBoundingRectangle(Rectangle bounds, Sprite sprite)
    {
        bounds.X = (int)sprite.Position.X;
        bounds.Y = (int)sprite.Position.Y;
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