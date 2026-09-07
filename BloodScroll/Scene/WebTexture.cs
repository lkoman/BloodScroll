using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// THE WEB ITSELF, BAKED PIXEL BY PIXEL
//
// There is no web art in any of the atlases (the patches on the ground borrow
// a platform sprite), so the strands are worked out here - see BakedTexture,
// which owns the cache and the pixel walk.
//
// An orb web: spokes out from the middle, rings around it that sag inwards
// between the spokes the way a real one does.
//
// Two things wear one - the big one the player is wrapped in while he is stuck
// (WebOverlay) and the little one a spider spits (Bullet) - so every size is
// baked once and then handed out.
//

public static class WebTexture
{
    private const float STRAND = 1.7f;      // half thickness of a strand, in pixels
    private const float SAG = 0.06f;        // how far the rings droop between spokes

    // The spokes all meet in the middle, so they are faded in from the hub
    // outwards - without this the centre is a white blob
    private const float HUB = 0.14f;

    // Where the rings sit, as a share of the radius
    private static readonly float[] FullRings = [0.34f, 0.55f, 0.76f, 0.95f];
    private static readonly float[] LooseRings = [0.42f, 0.72f, 0.96f];

    private const int FULL_SPOKES = 8;
    private const int LOOSE_SPOKES = 6;

    // A web only has so much room for detail. Under this the eight spokes and
    // four rings are closer together than the strands are thick, and the whole
    // thing fills in into a white disc - so a small web is woven loose instead.
    private const int LOOSE_UNDER = 96;

    private static readonly TextureCache cache = new(Bake);

    // size is how wide the web comes out, in pixels
    public static Texture2D Get(int size) => cache.Get(size);

    private static Texture2D Bake(int size)
    {
        bool loose = size < LOOSE_UNDER;
        int spokes = loose ? LOOSE_SPOKES : FULL_SPOKES;
        float[] rings = loose ? LooseRings : FullRings;

        float spokeStep = MathF.Tau / spokes;

        return BakedTexture.Mask(size, (dx, dy, radius) =>
        {
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            float r = dist / radius;

            if (r > 1f)
                return 0f;

            float angle = MathF.Atan2(dy, dx);

            // Distance to the nearest spoke, measured along the arc so the
            // strand stays the same thickness all the way out
            float spokeDist = MathF.Abs(MathF.IEEERemainder(angle, spokeStep)) * dist;
            float spoke = Falloff(spokeDist) * MathHelper.Clamp((r - HUB) / HUB, 0f, 1f);

            // Pulled tight where a spoke holds them, sagging inwards in between
            float sag = 1f - SAG * (1f - MathF.Cos(angle * spokes));

            float ring = 0f;
            foreach (float ringRadius in rings)
                ring = MathF.Max(ring, Falloff(MathF.Abs(dist - ringRadius * sag * radius)));

            // Softened towards the rim so the web does not end on a cut circle
            float rim = MathHelper.Clamp((1f - r) / 0.08f, 0f, 1f);

            return MathF.Max(spoke, ring) * rim;
        });
    }

    // One pixel of softness on either side of a strand, so the lines are not jagged
    private static float Falloff(float distance)
    {
        return MathHelper.Clamp(1f - distance / STRAND, 0f, 1f);
    }
}
