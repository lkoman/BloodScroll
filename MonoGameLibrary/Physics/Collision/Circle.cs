using System;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary;

public struct Circle(int x, int y, int radius) : IEquatable<Circle> 
{
    private static readonly Circle s_empty = new();

    // The x and y of the center of this circle.
    public int X = x, Y = y;
    public readonly int Radius = radius;
    public readonly Point Location => new(X, Y); // Gets the location of the center of this circle.
    public static Circle Empty => s_empty; // Gets a circle with X=0, Y=0, and Radius=0.
    public readonly bool IsEmpty => X == 0 && Y == 0 && Radius == 0; // Gets a value that indicates whether this circle has a radius of 0 and a location of (0, 0).
    public readonly int Top => Y - Radius;
    public readonly int Bottom => Y + Radius;
    public readonly int Left => X - Radius;
    public readonly int Right => X + Radius;

    public void SetPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    public readonly bool Intersects(Circle other)
    {
        int radiiSquared = (Radius + other.Radius) * (Radius + other.Radius);
        float distanceSquared = Vector2.DistanceSquared(Location.ToVector2(), other.Location.ToVector2());
        return distanceSquared < radiiSquared;
    }

    public override readonly bool Equals(object obj) => obj is Circle other && Equals(other);
    public readonly bool Equals(Circle other) => X == other.X && Y == other.Y && Radius == other.Radius;
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Radius);

    public static bool operator ==(Circle lhs, Circle rhs) => lhs.Equals(rhs);
    public static bool operator !=(Circle lhs, Circle rhs) => !lhs.Equals(rhs);
    
}
