using System;
using System.Collections.Generic;

namespace BloodScroll;

//
// HOW MANY TIMES THE PLAYER HAS ALREADY FOUGHT EACH BOSS
//
// The climb never ends, so the boss roster comes round again and again. The
// player does not come round with it: by his second spider queen he has more
// HP, a shield, a second gun, life steal and a faster trigger than he had at
// his first. A boss with the HP it was written with is a boss that gets easier
// every time it appears, which makes the middle of a long run feel like the
// game has given up.
//
// So each boss TYPE is counted separately and gets tougher on each of ITS own
// appearances. Counting per type rather than per boss layer matters: meeting
// the moth for the first time on layer 20 should be meeting the moth as it was
// written, not a moth scaled up by every fight that happened before it.
//
//   1st time  what the mob asked for in its own constructor
//   2nd time  half again - noticeably longer, still the same fight
//   3rd time  more than double - it now outlasts the gear that beat it twice
//   and up from there, faster each time
//
// The growth accelerates on purpose. The player's own power does too: the
// gifts stack, and life steal plus a shield that regrows is worth far more on
// a long fight than on a short one.
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
