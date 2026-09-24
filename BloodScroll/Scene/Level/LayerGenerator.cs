using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;


namespace BloodScroll;

//
// LIBRARY LAYER / PLATFORM GENERATOR
// - na layer 0 je en velik platform,
// - nato se v layer neskončno generirata dve poti (ena na levi strani ekrana, druga na desni)
// - vsak platform se generira randomly, toliko daleč, da player še lahko skoči nanjo iz prejšnje platforme
//
// THE GEOMETRY (world space, up is negative)
//
//   ceiling  y = -N * windowHeight        <- the camera cuts here
//   ...      one layer, exactly one screen tall
//   seam     y = -(N-1) * windowHeight    <- shared edge with the layer below
//
// The camera shows one whole layer, and collision only tests the platforms of
// the layer the player's FEET are in. Three rules follow:
//
//   - no platform may sit so high that a player on it has his head cut off by
//     the ceiling,
//   - a platform's TOP EDGE decides which layer it belongs to, so the platform
//     straddling the seam must keep its top edge above it,
//   - the last platform of a layer must be within one jump of the first
//     platform of the next, or the climb is over.
//
// These pull against each other: head room + platform height is 156px and one
// full jump is 156px, so there is NO slack - which is why the seam platform may
// hang over a little. See SEAM_OVERHANG.
//

public class LayerGenerator
{
    // The only layer with a slab of ground running the whole width of the
    // screen. Everything above it is small platforms and air, which is why the
    // mobs that WALK can only live down here - see MobManager.
    public const int GROUND_LAYER_INDEX = 0;

    // A platform placed at the exact apex of a jump cannot be landed on: the
    // landing test only fires once the player is falling again, and by then he
    // has already dropped a few pixels. Every reach used here is that much
    // shorter than the real one.
    private const float JUMP_MARGIN = 8f;

    // He also leaves a platform from a standstill and needs a few frames to get
    // up to speed, which costs him the start of every gap.
    private const float RUN_MARGIN = 40f;

    // Kept between the left and the right path, so the two can never touch or
    // overlap and switching sides always means a real jump.
    private const float PATH_GAP = 64f;

    // THE SWEEP
    // Left to itself a path climbs almost straight up: every hop may go a long
    // way sideways, but a random one usually does not, and the platforms end up
    // stacked. So each path is given a side of its band to walk towards, and
    // keeps it until it gets there - which turns the climb into a zig zag from
    // one edge of the band to the other.

    // How much of what the jump can reach is thrown away on the side the path
    // is walking away from. The landing spot is still random, it is just always
    // random well over on the side the path is heading for.
    private const float SWEEP_BIAS = 0.75f;

    // Near enough to the edge it was walking towards to call it arrived, and
    // turn round.
    private const float SWEEP_ARRIVED = 100f;

    // The chance a path turns round early anyway, so no two crossings of the
    // band come out the same width.
    private const float SWEEP_EARLY_TURN = 0.06f;

    // Which way each path is walking right now: +1 towards the right edge of
    // its band, -1 towards the left. Only ever reset on the ground layer, which
    // is generated once per run.
    private static int[] sweepDir = [];

    // How far the platform on the seam may hang into the layer below. Without
    // some overhang the seam jump would have to be pixel perfect (see above),
    // with too much the platform disappears under the bottom of the screen
    // while the player is standing on it. Half a platform is the compromise.
    private const float SEAM_OVERHANG = 16f;

    // Room a player standing on a platform needs above it, so the ceiling never
    // cuts him in half. Taken from the sprite instead of hard coded - the old
    // 102 here was the height of a player sprite two versions old.
    private static float HeadRoom => Player.PLAYER_HEIGHT;

    // How far one hop may climb. The minimum is what keeps a platform from
    // landing on the head of a player standing on the one below it.
    private static float MinRise => Player.PLAYER_HEIGHT;
    private static float MaxRise => Player.PLAYER_MAX_JUMP_Y - JUMP_MARGIN;

    // Every small platform is the same size, so the atlas is only asked once
    private static Vector2 smallSize = Vector2.Zero;
    private static Vector2 SmallSize
    {
        get
        {
            if (smallSize == Vector2.Zero)
            {
                Platform p = new Platform().GenerateNewPlatform("small-platform", Vector2.Zero);
                smallSize = new Vector2(p.Width, p.Height);
            }

            return smallSize;
        }
    }

    // How thick a jumpable ledge is. Read by the player, because it is what
    // decides how fast he is allowed to fall - see Player.MaxFallSpeed.
    public static float SmallPlatformHeight => SmallSize.Y;

    public static Sprite GetBackground(int currentLayerIndex, Vector2 layerOffset)
    {
        string backgroundName = "background6";
        if (currentLayerIndex <= 5)
        {
            backgroundName = "background" + currentLayerIndex;
        }

        Sprite _background = Globals.Backgrounds.CreateSprite(backgroundName);
        _background.Position -= layerOffset;

        return _background;
    }

    // lastPlatformPos holds, per path, the platform the previous layer signed
    // off with. Every path carries on from its own.
    public static (List<Platform>, Vector2[]) GeneratePlatforms(int currentLayerIndex, Vector2[] lastPlatformPos)
    {
        List<Platform> platforms = [];

        // A fresh run starts on the ground layer, so that is where the sweep
        // starts over as well
        if (currentLayerIndex == GROUND_LAYER_INDEX || sweepDir.Length != lastPlatformPos.Length)
            ResetSweep(lastPlatformPos.Length);

        // The ground slab only exists on the layer the player starts on
        if (currentLayerIndex == GROUND_LAYER_INDEX)
            GenerateBigPlatforms(platforms);

        for (int path = 0; path < lastPlatformPos.Length; path++)
        {
            lastPlatformPos[path] = GeneratePath(
                platforms,
                currentLayerIndex,
                lastPlatformPos[path],
                path,
                lastPlatformPos.Length);
        }

        return (platforms, lastPlatformPos);
    }

    // One path (one side of the screen) through one layer, bottom to top.
    // Returns the platform the next layer has to carry on from.
    private static Vector2 GeneratePath(List<Platform> platforms, int layerIndex, Vector2 previousGateway, int path, int pathCount)
    {
        float ceiling = -layerIndex * Core.windowHeight;
        float seam = ceiling + Core.windowHeight;

        // Where the climb starts: the ground slab on layer 0, the platform the
        // layer below signed off with everywhere else.
        Vector2 from = layerIndex == GROUND_LAYER_INDEX
            ? GroundLaunch(path, pathCount)
            : previousGateway;

        // THE PLATFORM ON THE SEAM
        // The only jump in the layer that starts in the layer below, so it is
        // measured against the seam and not against the ceiling. Its top edge
        // stays above the seam no matter what: that is the half the player
        // stands on, and the only half this layer's collision code looks at.
        float highest = from.Y - MaxRise;
        float lowest = Math.Min(from.Y - MinRise, seam - SmallSize.Y + SEAM_OVERHANG);

        Vector2 pos = PlacePlatform(platforms, from, Rand(highest, lowest), path, pathCount);

        // THE GATEWAY
        // The platform that hands the player over to the next layer. It has to
        // clear his head under the ceiling, and still be close enough to it that
        // the seam platform of the next layer is inside a single jump.
        float gatewayHighest = ceiling + HeadRoom;
        float gatewayLowest = ceiling - SmallSize.Y + SEAM_OVERHANG + MaxRise;

        // Only if the jump ever gets nerfed below what the geometry needs. Being
        // visible wins over being reachable - a jump that is a few pixels too
        // long is a hard jump, a player with no head is a broken game.
        if (gatewayLowest < gatewayHighest)
            gatewayLowest = gatewayHighest;

        float gateway = Rand(gatewayHighest, gatewayLowest);

        // THE CLIMB IN BETWEEN
        foreach (float rise in SplitClimb(pos.Y - gateway))
            pos = PlacePlatform(platforms, pos, pos.Y - rise, path, pathCount);

        return pos;
    }

    // Cuts the climb into hops the player can actually make: never longer than
    // one jump, and spread over as many platforms as still fit.
    private static IEnumerable<float> SplitClimb(float climb)
    {
        if (climb <= 0f)
            yield break;

        int fewest = Math.Max((int)Math.Ceiling(climb / MaxRise), 1);
        int most = (int)Math.Floor(climb / MinRise);

        int steps = most > fewest ? Globals.R.Next(fewest, most + 1) : fewest;

        float left = climb;

        for (int remaining = steps; remaining > 0; remaining--)
        {
            int after = remaining - 1;

            // Whatever is left over has to stay coverable by the hops that
            // follow, so each hop is bounded by what those can still take.
            float lo = Math.Max(MinRise, left - after * MaxRise);
            float hi = Math.Min(MaxRise, left - after * MinRise);

            // Going over MaxRise breaks the climb, going under MinRise only
            // makes for a cramped one, so the ceiling wins when they cross.
            float rise = Math.Max(hi >= lo ? Rand(lo, hi) : hi, 0f);

            left -= rise;
            yield return rise;
        }
    }

    private static Vector2 PlacePlatform(List<Platform> platforms, Vector2 from, float y, int path, int pathCount)
    {
        Vector2 pos = new(PickX(from, y, path, pathCount), y);

        platforms.Add(new Platform());
        platforms.Last().GenerateNewPlatform("small-platform", pos);

        return pos;
    }

    // Sideways reach shrinks the higher the jump has to go, so the two are
    // picked together. The height is already fixed by the time we get here.
    private static float PickX(Vector2 from, float y, int path, int pathCount)
    {
        float reach = Math.Max(Player.MaxJumpRun(from.Y - y) - RUN_MARGIN, 0f);

        (float bandLeft, float bandRight) = PathBand(path, pathCount);

        float lo = Math.Max(from.X - reach, bandLeft);
        float hi = Math.Min(from.X + reach, bandRight);

        // Only when the jump starts from outside this path's band, which the
        // ground slab is wide enough to do
        if (hi < lo)
            return Math.Clamp(from.X, bandLeft, bandRight);

        int dir = sweepDir[path];

        // Somewhere in the far part of what this jump can still reach, on the
        // side the path is walking towards
        float x = dir > 0
            ? Rand(lo + (hi - lo) * SWEEP_BIAS, hi)
            : Rand(lo, hi - (hi - lo) * SWEEP_BIAS);

        bool arrived = dir > 0
            ? x >= bandRight - SWEEP_ARRIVED
            : x <= bandLeft + SWEEP_ARRIVED;

        if (arrived || Globals.R.NextDouble() < SWEEP_EARLY_TURN)
            sweepDir[path] = -dir;

        return x;
    }

    // Both paths set off towards the middle of the screen, so the run opens
    // with the two of them close enough to see across.
    private static void ResetSweep(int pathCount)
    {
        sweepDir = new int[pathCount];

        for (int path = 0; path < pathCount; path++)
            sweepDir[path] = path < pathCount / 2f ? 1 : -1;
    }

    // The slice of the screen a path owns, in platform LEFT edges. Paths get a
    // gap between them, so the platforms of one path can never touch the other.
    private static (float, float) PathBand(int path, int pathCount)
    {
        float slice = Core.windowWidth / pathCount;

        float left = path * slice + (path == 0 ? 0f : PATH_GAP / 2f);
        float right = (path + 1) * slice - SmallSize.X - (path == pathCount - 1 ? 0f : PATH_GAP / 2f);

        return (left, Math.Max(right, left));
    }

    // Layer 0 has no platform to carry on from - the climb starts anywhere
    // along this path's half of the ground slab.
    private static Vector2 GroundLaunch(int path, int pathCount)
    {
        Platform ground = new Platform().GenerateNewPlatform("platform", Vector2.Zero);

        (float left, float right) = PathBand(path, pathCount);

        return new Vector2(Rand(left, right), Core.windowHeight - ground.Height);
    }

    private static void GenerateBigPlatforms(List<Platform> platforms)
    {
        Platform bigPlatform = new Platform().GenerateNewPlatform("platform", Vector2.Zero);

        int numBigPlatforms = (int)Math.Ceiling(Core.windowWidth / bigPlatform.Width);

        // Create a row of numOfPlatforms number of platforms, every second one flipped horizontally
        for (int i = 0; i < numBigPlatforms; i++)
        {
            platforms.Add(new Platform());
            platforms.Last().GenerateNewPlatform(
                "platform",
                new Vector2(bigPlatform.Width * i, Core.windowHeight - bigPlatform.Height));

            if (i % 2 != 0)
            {
                platforms.Last().SetPlatformEffects(SpriteEffects.FlipHorizontally);
            }
        }
    }

    private static float Rand(float lo, float hi)
    {
        if (hi <= lo)
            return lo;

        return (float)(Globals.R.NextDouble() * (hi - lo) + lo);
    }
}
