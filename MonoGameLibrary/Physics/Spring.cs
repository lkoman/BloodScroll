using System;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary;

//
// OMEJEVANJE - A DAMPED SPRING BETWEEN A FIXED POINT AND ONE PARTICLE
//
// The pendulum-on-a-wall case: one particle tied to an Anchor that never moves.
// The tie is a spring, not a rod, so the particle can be pulled out of place,
// wobble and settle back.
//
// It pulls towards a REST POINT, not just towards the anchor - a purely radial
// spring would let the end drift anywhere on a circle. The rest point is a
// direction plus a length so the two can be driven separately:
//   - turn RestDirection and the whole thing swings round
//   - grow RestLength and it stretches along that direction
//
//   F = -k * (position - rest point)   Hooke's law
//     - c * velocity                   damping
//     + whatever ApplyForce was given  outside push
//
// SEMI-IMPLICIT integration (velocity first, then position with the NEW
// velocity) - stays stable at stiffnesses where plain Euler blows up.
//
// Draws nothing. See TiledRope for the flower's stem.
//

public class Spring
{
    // The fixed end. Pinned to the wall, the platform, the ceiling.
    public Vector2 Anchor { get; set; }

    // The moving end
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }

    // k - how hard it pulls itself back into shape. Bigger is snappier.
    public float Stiffness { get; set; } = 120f;

    // c - how much of the movement is bled off every frame. Around
    // 2 * sqrt(Stiffness * Mass) it stops dead without overshooting; below
    // that it bounces past and comes back.
    public float Damping { get; set; } = 6f;

    public float Mass { get; set; } = 1f;

    // Where the spring WANTS its moving end: this far from the anchor,
    // in this direction. Direction is kept normalised by the setter.
    public float RestLength { get; set; } = 0f;

    private Vector2 restDirection = -Vector2.UnitY; // up, because up is negative
    public Vector2 RestDirection
    {
        get => restDirection;
        set
        {
            if (value == Vector2.Zero)
                return;

            value.Normalize();
            restDirection = value;
        }
    }

    // A hard ceiling on how far the moving end may ever get from the anchor,
    // whatever the forces say. This is the rope inside the spring: the stalk
    // has only so much of itself to give.
    public float MaxLength { get; set; } = float.MaxValue;

    private Vector2 externalForce = Vector2.Zero;

    public Spring(Vector2 anchor)
    {
        Anchor = anchor;
        Position = anchor;
    }

    // Where the moving end would sit if nothing were disturbing it
    public Vector2 RestPosition => Anchor + restDirection * RestLength;

    public Vector2 Offset => Position - Anchor;
    public float Length => Offset.Length();

    // Which way the connection currently points, anchor -> moving end.
    // Falls back to the rest direction while the two ends sit on top of each
    // other, so a stalk that has not grown yet still knows which way is up.
    public Vector2 Direction
    {
        get
        {
            Vector2 offset = Offset;
            float length = offset.Length();

            if (length < 0.0001f)
                return restDirection;

            return offset / length;
        }
    }

    // How far it currently is from where it wants to be. Callers use this to
    // tell "still travelling" from "arrived and settled".
    public float StretchFromRest => (Position - RestPosition).Length();

    // A push that lasts this frame only - a gust, a hit, the sideways nudge
    // that keeps an idle stalk swaying. Cleared by Update once it is used.
    public void ApplyForce(Vector2 force)
    {
        externalForce += force;
    }

    public void Update(float dt)
    {
        Vector2 displacement = Position - RestPosition;

        Vector2 force = -Stiffness * displacement
                        - Damping * Velocity
                        + externalForce;

        externalForce = Vector2.Zero;

        Velocity += force / Mass * dt;
        Position += Velocity * dt;

        HoldToMaxLength();
    }

    // The length limit is enforced after the fact, by moving the end back onto
    // the circle and throwing away the part of its velocity that was carrying
    // it outwards. Keeping the sideways part is what lets a stalk at full
    // stretch still swing round the anchor instead of sticking to one spot.
    private void HoldToMaxLength()
    {
        Vector2 offset = Offset;
        float length = offset.Length();

        if (length <= MaxLength || length < 0.0001f)
            return;

        Vector2 direction = offset / length;

        Position = Anchor + direction * MaxLength;

        float outward = Vector2.Dot(Velocity, direction);
        if (outward > 0f)
            Velocity -= direction * outward;
    }

    // Puts the moving end somewhere and stops it dead. For setting the thing
    // up, or for snapping it back after a state change - never mid swing.
    public void SnapTo(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
        externalForce = Vector2.Zero;
    }

    // Moves the fixed end and carries the moving end along with it, so a
    // spring whose anchor is re-placed does not fling itself across the level
    public void MoveAnchor(Vector2 anchor)
    {
        Vector2 delta = anchor - Anchor;

        Anchor = anchor;
        Position += delta;
    }

    // Turns the rest direction towards a point in the world. This is the aim:
    // point it at the player and raise the stiffness and the end is thrown
    // that way, because the spring is now badly out of shape.
    public void AimRestAt(Vector2 target)
    {
        RestDirection = target - Anchor;
    }

    // Convenience for the common "aim there and reach for it" in one line.
    // The reach is capped at MaxLength - it cannot lunge further than it is.
    public void ReachFor(Vector2 target)
    {
        Vector2 toTarget = target - Anchor;
        float distance = toTarget.Length();

        if (distance < 0.0001f)
            return;

        RestDirection = toTarget;
        RestLength = MathF.Min(distance, MaxLength);
    }
}
