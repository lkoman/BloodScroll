using System;
using System.Collections.Generic;

namespace BloodScroll;

//
// HOW MANY TIMES THE PLAYER HAS ALREADY FOUGHT EACH BOSS
//
// The climb never ends, so the roster comes round again and again while the
// player keeps gaining HP, shields, guns and life steal.
//
// Each boss TYPE is counted SEPARATELY and gets tougher on each of ITS own
// appearances - per type, not per boss layer, so meeting the moth for the first
// time on layer 20 is the moth as it was written.
//
//   1st time  what the mob asked for in its own constructor
//   2nd time  half again
//   3rd time  more than double, and up from there, faster each time
//
// The growth accelerates because the player's own power does.
//

public static class BossTally
{
    // The straight part of the growth: how much of the base HP each fresh
    // appearance is worth on its own
    private const float PER_APPEARANCE = 0.4f;

    // And the part that curves. Small, because it is squared - this is what
    // turns "a bit more" on the second meeting into "quite a bit more" on the
    // third and beyond.
    private const float ACCELERATION = 0.1f;

    private static readonly Dictionary<MobType, int> appearances = [];

    // Every run starts the roster over. Gifts are earned again from scratch,
    // so the bosses have to be too.
    public static void Reset() => appearances.Clear();

    // Counts this appearance and hands back what to multiply its HP by.
    // Called once, as the boss is spawned.
    public static float NextScale(MobType type)
    {
        int seen = appearances.TryGetValue(type, out int n) ? n : 0;
        appearances[type] = seen + 1;

        return 1f + PER_APPEARANCE * seen + ACCELERATION * seen * seen;
    }
}
