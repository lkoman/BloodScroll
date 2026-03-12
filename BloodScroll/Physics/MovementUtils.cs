using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

using Vector2 = Microsoft.Xna.Framework.Vector2;

//
// LIBRARY FOR MOVEMENT
//

public static class MovementUtils
{
    public static (Vector2, Vector2) MoveTowardsTarget(Vector2 pos, Vector2 target, Vector2 velocity, float speed, float MAX_SPEED)
    {
        velocity += Vector2.Normalize(target - pos) * speed * Globals.DT;

        velocity.X = MyMath.Clamp(velocity.X, -MAX_SPEED, MAX_SPEED);
        velocity.Y = MyMath.Clamp(velocity.Y, -MAX_SPEED, MAX_SPEED);

        pos += velocity * Globals.DT;

        return (pos, velocity);
    }

    public static SpriteEffects FlipSprite(Vector2 velocity, SpriteEffects effects)
    {
        if (velocity.X > 1)
            effects = SpriteEffects.None;
        else
            effects = SpriteEffects.FlipHorizontally;
        return effects;
    }

    public static (Vector2, SpriteEffects, Vector2) MoveHorizontally(
        Vector2 pos, 
        SpriteEffects effects, 
        Vector2 target, 
        float speed)
    {
        // premika se levo
        if (pos.X <= 0)
        {
            target.X = Core.windowWidth; // DESNO
            effects = SpriteEffects.None;
        }
        // premika se desno
        else if (pos.X >= Core.windowWidth)
        {
            target.X = 0; // LEVO
            effects = SpriteEffects.FlipHorizontally;
        }
        
        pos += Vector2.Normalize(target - pos) * speed * Globals.DT;

        return (pos, effects, target);
    }

    public static Vector2 MoveForward(Vector2 pos, Vector2 direction, float speed)
    {
        pos += direction * speed * Globals.DT;
        return pos;
    }

    public static (Vector2, Vector2, bool) MoveForwardWithVelocity(
        Vector2 position, 
        Vector2 velocity, 
        Vector2 direction, 
        bool jerk, 
        float SPEED, 
        float MAX_SPEED)
    {
        velocity.Y -= direction.Y * SPEED * Globals.DT;
        velocity.X += direction.X * SPEED * Globals.DT;

        velocity.X = MyMath.Clamp(velocity.X, -MAX_SPEED, MAX_SPEED);
        velocity.Y = MyMath.Clamp(velocity.Y, -MAX_SPEED, MAX_SPEED);

        if (jerk)
        {
            velocity += direction * SPEED;
            jerk = false;
        }
        position += velocity * Globals.DT;

        return (position, velocity, jerk);
    }

    public static Vector2 RotateForAngle(Vector2 pos, float angle)
    {
        float a = angle * Globals.DT;

        float cos = (float)Math.Cos(a);
        float sin = (float)Math.Sin(a);

        float x = pos.X;
        float y = pos.Y;

        pos = new Vector2(
            x * cos - y * sin,
            x * sin + y * cos
        );

        return pos;
    }

    // Left and right
    public static bool HitEdge(Vector2 pos, float width)
    {
        if (pos.X <= 0 - Globals.CameraOffset.X ||
            pos.X >= Core.windowWidth - Globals.CameraOffset.X - width)
        {
            return true;
        }
        return false;
    }

    public static Vector2 BounceFromEdge(Vector2 velocity, Vector2 pos, float width)
    {
        if (pos.X <= 0 || pos.X >= Core.windowWidth - width)
        {
            velocity.X *= -3;
            return velocity;
        }

        return velocity;
    }
}