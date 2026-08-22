using System;
using Genbox.VelcroPhysics.MonoGame.DebugView;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace MonoGameLibrary.Debug;

public class DebugRenderer
{
    // PRIMITIVE BATCH
    public Matrix proj;
    public Matrix view;
    public PrimitiveBatch pb;
    private readonly int circleSegments = 32;

    public void LoadContent(GraphicsDevice gd)
    {
        proj = Matrix.CreateOrthographicOffCenter(
            0,
            Globals.VIRTUAL_WIDTH,
            Globals.VIRTUAL_HEIGHT,
            0,
            0f,
            1f
        );

        view = Matrix.Identity;

        pb = new PrimitiveBatch(gd);
        pb.SetProjection(ref proj);
    }

    public void DrawRect(Rectangle r, Color c)
    {
        Vector2 tl = new(r.Left,  r.Top);
        Vector2 tr = new(r.Right, r.Top);
        Vector2 br = new(r.Right, r.Bottom);
        Vector2 bl = new(r.Left,  r.Bottom);

        pb.AddVertex(tl, c, PrimitiveType.LineList);
        pb.AddVertex(tr, c, PrimitiveType.LineList);

        pb.AddVertex(tr, c, PrimitiveType.LineList);
        pb.AddVertex(br, c, PrimitiveType.LineList);

        pb.AddVertex(br, c, PrimitiveType.LineList);
        pb.AddVertex(bl, c, PrimitiveType.LineList);

        pb.AddVertex(bl, c, PrimitiveType.LineList);
        pb.AddVertex(tl, c, PrimitiveType.LineList);
    }

    public void DrawPolygon(Polygon poly, Color c) => DrawPolygon(poly, Vector2.Zero, c);

    // The overlay draws in screen space while hitboxes live in world space,
    // so everything drawn here has to be shifted by the camera first
    public void DrawPolygon(Polygon poly, Vector2 offset, Color c)
    {
        if (poly.Count < 2)
            return;

        for (int i = 0; i < poly.Count; i++)
        {
            pb.AddVertex(poly[i] + offset, c, PrimitiveType.LineList);
            pb.AddVertex(poly[(i + 1) % poly.Count] + offset, c, PrimitiveType.LineList);
        }
    }

    public void DrawCircle(Circle bounds, Color circleColor)
    {
        Vector2 center = new(bounds.X, bounds.Y);
        float radius = bounds.Radius;
        float inc = MathF.Tau / circleSegments;
        for (int i = 0; i < circleSegments; i++)
        {
            float a1 = i * inc;
            float a2 = (i + 1) * inc;

            Vector2 p1 = center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * bounds.Radius;
            Vector2 p2 = center + new Vector2(MathF.Cos(a2), MathF.Sin(a2)) * bounds.Radius;

            pb.AddVertex(p1, circleColor, PrimitiveType.LineList);
            pb.AddVertex(p2, circleColor, PrimitiveType.LineList);
        }
    }

}