using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

//
// LIBRARY FOR MOVEMENT
//

public static class MovementUtils
{
    public static (Vector2, Vector2) MoveTowardsTarget(Vector2 pos, Vector2 target, Vector2 velocity, float speed, float MAX_SPEED)
    {
        velocity += Vector2.Normalize(target - pos) * speed * Globals.DT;

        velocity.X = Math.Clamp(velocity.X, -MAX_SPEED, MAX_SPEED);
        velocity.Y = Math.Clamp(velocity.Y, -MAX_SPEED, MAX_SPEED);

        pos += velocity * Globals.DT;

        return (pos, velocity);
    }

    public static SpriteEffects FlipSprite(Vector2 velocity)
    {
        return velocity.X > 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
    }

    // Turns a sprite to look at something instead of at wherever it happens to
    // be drifting. A mob that stops to wind up an attack has no velocity to
    // read, so FlipSprite above would have it staring the wrong way.
    public static SpriteEffects FaceTowards(float fromX, float toX, SpriteEffects effects)
    {
        if (toX > fromX)
            return SpriteEffects.None;

        if (toX < fromX)
            return SpriteEffects.FlipHorizontally;

        return effects;
    }

    //
    // AIMING. Radians, 0 points right, growing CLOCKWISE on screen because up is
    // negative. Everything below stays wrapped to -PI..PI, so "turn the short
    // way round" is just a comparison.
    //

    public static float AngleTo(Vector2 from, Vector2 to)
    {
        Vector2 d = to - from;
        return MathF.Atan2(d.Y, d.X);
    }

    public static Vector2 FromAngle(float angle)
    {
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }

    // The shortest way round from one angle to the other, so a mob looking
    // just clockwise of its target turns back a hair instead of going the
    // long way round the circle
    public static float AngleDifference(float from, float to)
    {
        return MathHelper.WrapAngle(to - from);
    }

    // Swings `angle` towards `target` by at most this frame's worth of turn.
    // A mob that has to line up before it commits is what makes an attack
    // readable - the turn IS the telegraph.
    public static float TurnTowards(float angle, float target, float radiansPerSecond)
    {
        float difference = AngleDifference(angle, target);
        float step = radiansPerSecond * Globals.DT;

        if (MathF.Abs(difference) <= step)
            return MathHelper.WrapAngle(target);

        return MathHelper.WrapAngle(angle + MathF.Sign(difference) * step);
    }

    public static (Vector2, SpriteEffects, Vector2) MoveHorizontally(
        Vector2 pos, 
        SpriteEffects effects, 
        Vector2 target, 
        float speed)
    {

        if (target.X != 0 && target.X != Core.windowWidth)
        {
            int rand = Globals.R.Next(2);  // 0 or 1

            if (rand == 0)
            {
                target.X = Core.windowWidth; // DESNO
                effects = SpriteEffects.None;  
            }
            else
            {
                target.X = 0; // LEVO
                effects = SpriteEffects.FlipHorizontally;
            }
        }
        else {
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
        }
        
        pos = MOVE(pos, target, speed);

        return (pos, effects, target);
    }

    public static Vector2 MOVE(Vector2 pos, Vector2 target, float speed)
    {
        return pos + Vector2.Normalize(target - pos) * speed * Globals.DT;
    }

    public static Vector2 MoveForward(Vector2 pos, Vector2 direction, float speed)
    {
        pos += direction * speed * Globals.DT;
        return pos;
    }

    public static Vector2 BounceFromEdge(Vector2 velocity, Vector2 pos, float width)
    {
        if (pos.X <= 0 || pos.X >= Core.windowWidth - width)
            velocity.X *= -3;

        return velocity;
    }
}