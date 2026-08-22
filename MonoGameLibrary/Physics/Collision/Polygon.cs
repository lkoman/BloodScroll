using System;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary;

//
// CONVEX POLYGON HITBOX - SEPARATING AXIS THEOREM
//
// A rectangle is a poor fit for a lot of shapes: a spider is mostly legs, a
// moth is mostly wing. This gives those a hitbox that follows the drawing.
//
// SAT in one sentence: two convex shapes are apart if and only if there is
// some line onto which their shadows do not overlap. Only the edge normals of
// the two shapes can be that line, so we project both onto each normal and
// look for a gap. One gap is enough to prove they do not touch; if no normal
// gives a gap, they are overlapping.
//
// Only valid for CONVEX polygons - a concave shape has to be split into
// convex pieces first.
//

public struct Polygon
{
    // In world space, wound consistently (order matters for the normals)
    private readonly Vector2[] vertices;
    private Vector2 offset;

    // For shapes that turn (the spider queen aiming her ram). Turning a convex
    // shape leaves it convex, so SAT below needs no changes at all.
    private float rotation;
    private Vector2 pivot;

    public Polygon(Vector2[] localVertices)
    {
        vertices = localVertices;
        offset = Vector2.Zero;
        rotation = 0f;
        pivot = Vector2.Zero;
    }

    public readonly int Count => vertices?.Length ?? 0;

    // Vertex i in world space
    public readonly Vector2 this[int i] => Turned(vertices[i]) + offset;

    public void SetPosition(Vector2 position)
    {
        offset = position;
    }

    // radians clockwise on screen, about a point given in the same local space
    // as the vertices
    public void SetRotation(float radians, Vector2 about)
    {
        rotation = radians;
        pivot = about;
    }

    private readonly Vector2 Turned(Vector2 vertex)
    {
        if (rotation == 0f)
            return vertex;

        Vector2 arm = vertex - pivot;

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        return pivot + new Vector2(
            arm.X * cos - arm.Y * sin,
            arm.X * sin + arm.Y * cos);
    }

    // A box as a polygon, so rectangles can be tested against real shapes
    public static Polygon FromRectangle(Rectangle r)
    {
        var poly = new Polygon(
        [
            new Vector2(r.Left,  r.Top),
            new Vector2(r.Right, r.Top),
            new Vector2(r.Right, r.Bottom),
            new Vector2(r.Left,  r.Bottom),
        ]);

        return poly;
    }

    // Axis aligned box that contains the whole polygon. Cheap to compare, so
    // it is worth checking before running the full test.
    public readonly Rectangle BoundingBox()
    {
        if (Count == 0)
            return Rectangle.Empty;

        float minX = this[0].X, maxX = this[0].X;
        float minY = this[0].Y, maxY = this[0].Y;

        for (int i = 1; i < Count; i++)
        {
            Vector2 v = this[i];

            if (v.X < minX) minX = v.X;
            if (v.X > maxX) maxX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.Y > maxY) maxY = v.Y;
        }

        return new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
    }

    public readonly bool Intersects(Polygon other)
    {
        if (Count == 0 || other.Count == 0)
            return false;

        // Broad phase: if the boxes miss, the shapes definitely miss
        if (!BoundingBox().Intersects(other.BoundingBox()))
            return false;

        // A gap on ANY of the two shapes' normals proves they are apart
        return !HasGapOnAnyNormal(this, other)
            && !HasGapOnAnyNormal(other, this);
    }

    public readonly bool Intersects(Rectangle rectangle)
    {
        return Intersects(FromRectangle(rectangle));
    }

    // Circle against polygon. Same idea, plus one extra axis: the one running
    // from the circle centre to the nearest vertex, which is the direction a
    // circle can slip past a corner on.
    public readonly bool Intersects(Circle circle)
    {
        if (Count == 0)
            return false;

        Vector2 centre = new(circle.X, circle.Y);

        for (int i = 0; i < Count; i++)
        {
            Vector2 edge = this[(i + 1) % Count] - this[i];
            Vector2 axis = Normal(edge);

            if (GapOnAxis(this, centre, circle.Radius, axis))
                return false;
        }

        Vector2 nearest = NearestVertex(centre);
        Vector2 cornerAxis = nearest - centre;

        if (cornerAxis.LengthSquared() > 0.0001f)
        {
            cornerAxis.Normalize();

            if (GapOnAxis(this, centre, circle.Radius, cornerAxis))
                return false;
        }

        return true;
    }

    // True if projecting both polygons onto any normal of "a" leaves a gap
    private static bool HasGapOnAnyNormal(Polygon a, Polygon b)
    {
        for (int i = 0; i < a.Count; i++)
        {
            Vector2 edge = a[(i + 1) % a.Count] - a[i];
            Vector2 axis = Normal(edge);

            (float minA, float maxA) = Project(a, axis);
            (float minB, float maxB) = Project(b, axis);

            if (maxA < minB || maxB < minA)
                return true;
        }

        return false;
    }

    private static bool GapOnAxis(Polygon poly, Vector2 centre, float radius, Vector2 axis)
    {
        (float minP, float maxP) = Project(poly, axis);

        float centreOnAxis = Vector2.Dot(centre, axis);
        float minC = centreOnAxis - radius;
        float maxC = centreOnAxis + radius;

        return maxP < minC || maxC < minP;
    }

    // The shadow the polygon casts on one axis
    private static (float min, float max) Project(Polygon poly, Vector2 axis)
    {
        float min = Vector2.Dot(poly[0], axis);
        float max = min;

        for (int i = 1; i < poly.Count; i++)
        {
            float p = Vector2.Dot(poly[i], axis);

            if (p < min) min = p;
            else if (p > max) max = p;
        }

        return (min, max);
    }

    private readonly Vector2 NearestVertex(Vector2 point)
    {
        Vector2 best = this[0];
        float bestDistance = Vector2.DistanceSquared(best, point);

        for (int i = 1; i < Count; i++)
        {
            float d = Vector2.DistanceSquared(this[i], point);

            if (d >= bestDistance)
                continue;

            bestDistance = d;
            best = this[i];
        }

        return best;
    }

    // Perpendicular to an edge, unit length. This is the candidate axis.
    private static Vector2 Normal(Vector2 edge)
    {
        Vector2 normal = new(-edge.Y, edge.X);

        if (normal.LengthSquared() > 0.0001f)
            normal.Normalize();

        return normal;
    }

    // Builds a convex outline inside a sprite frame from fractions of its size,
    // so a shape can be described once and reused at any sprite size.
    //   (0.5, 0f) is top centre, (1f, 0.5f) is right middle, and so on.
    public static Polygon FromFractions(Vector2[] fractions, float width, float height)
    {
        var verts = new Vector2[fractions.Length];

        for (int i = 0; i < fractions.Length; i++)
            verts[i] = new Vector2(fractions[i].X * width, fractions[i].Y * height);

        return new Polygon(verts);
    }
}
