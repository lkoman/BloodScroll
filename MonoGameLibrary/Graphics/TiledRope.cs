using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

//
// DRAWS A CONNECTION AS ONE SHORT PIECE REPEATED ALONG IT
//
// A Spring is maths - two points and the pull between them. This turns that
// into something you can see WITHOUT the length ever being drawn as one
// stretched sprite: it lays copies of a single small piece end to end from one
// point to the other, turned so they run down the line, and adds a copy as the
// gap grows. Stretch the connection and more pieces appear, let it back and
// they go away again.
//
// That means one 4x8 drawing of a bit of stem covers a stalk of any length,
// at any angle, and never smears the way a scaled sprite would.
//
// HOW TO DRAW THE PIECE:
//   Draw it UPRIGHT, growing upwards, with the end that joins the piece below
//   it at the BOTTOM of the frame. Keep it narrow and short - the shorter the
//   piece, the smoother the curve it can follow.
//

public class TiledRope
{
    private readonly AnimatedSprite piece;

    // How much of itself each piece is laid back over the one before it.
    // A little overlap hides the seams and stops a gap opening up when the
    // rope is turning. 0 lays them exactly end to end.
    public float Overlap { get; set; } = 0.2f;

    public Vector2 Scale { get; set; } = Vector2.One;

    // A long enough rope would happily ask for a thousand pieces. This is the
    // stop on that - past it the pieces are simply spread further apart.
    public int MaxPieces { get; set; } = 96;

    public TiledRope(AnimatedSprite piece)
    {
        this.piece = piece;
    }

    // Length of one piece measured along the rope, after scaling and overlap
    public float Step => MathF.Max(piece.Region.Height * Scale.Y * (1f - Overlap), 1f);

    // Runs the piece's own animation, if it has one. Every piece of the rope
    // is the same sprite, so they all animate together.
    public void Update()
    {
        piece.Update();
    }

    public void Draw(Vector2 from, Vector2 to, Color color)
    {
        Vector2 along = to - from;
        float distance = along.Length();

        if (distance < 0.0001f)
            return;

        Vector2 direction = along / distance;

        // The art points up; the rope points wherever it points. Up is
        // -PiOver2 as an angle, so this is the turn from one to the other.
        float rotation = MathF.Atan2(direction.Y, direction.X) + MathHelper.PiOver2;

        float step = Step;
        int count = (int)MathF.Ceiling(distance / step);

        if (count > MaxPieces)
        {
            count = MaxPieces;
            step = distance / count;
        }

        // Bottom middle of the frame, so a piece placed at a point on the line
        // hangs back down the line towards the piece before it
        Vector2 origin = new(piece.Region.Width * 0.5f, piece.Region.Height);

        for (int i = 1; i <= count; i++)
        {
            // The last one is pinned to the far end instead of being allowed
            // to overshoot it, which keeps the join under the head clean
            float along1D = MathF.Min(i * step, distance);

            piece.Region.Draw(
                from + direction * along1D,
                color,
                rotation,
                origin,
                Scale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
