using System;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE THREE DIFFICULTIES, WRITTEN ONCE
//
// MEDIUM IS THE GAME. Every constant below is the MEDIUM figure; baby and hell
// are that same figure bent one step down or up. Retuning means editing the
// medium number and letting the other two follow.
//
// Globals.DIFFICULTY is 0 baby / 1 medium / 2 hell. Step turns that into
// -1 / 0 / +1, and every formula here is built on Step.
//
// Four small nudges all leaning the same way, not one big multiplier.
//

public static class Difficulty
{
    // -1 baby, 0 medium, +1 hell
    private static int Step => Globals.DIFFICULTY - 1;

    //
    // AN ORDINARY MOB IS THE SAME MOB ALL THE WAY UP
    //
    // A bat on layer 40 has the HP and bite of a bat on layer 1. What makes late
    // layers hard is how MANY arrive - see Count. The swarm IS the curve.
    //
    private const float MOB_HP_STEP = 0.2f;
    private const float MOB_DAMAGE_STEP = 0.25f;

    public static float MobHp => 1f + MOB_HP_STEP * Step;
    public static float MobDamage => 1f + MOB_DAMAGE_STEP * Step;

    //
    // A BOSS GROWS WITH THE CLIMB
    //
    // Per layer, on medium. Small figures because they COMPOUND with BossTally,
    // which already scales each boss per appearance - this is only the climb's
    // share of it.
    //
    private const float BOSS_HP_PER_LAYER = 0.035f;
    private const float BOSS_DAMAGE_PER_LAYER = 0.025f;

    //
    // HOW FAST ANYTHING TIED TO THE LAYER GROWS
    //
    // The numbers above shift where the game STARTS; this shifts how steeply it
    // climbs. Baby climbs at just over half the rate, hell at about half again.
    //
    private const float GROWTH_STEP = 0.45f;
    private static float Growth => 1f + GROWTH_STEP * Step;

    // Both take the layer the boss is standing on, so a boss met late is a
    // harder boss than the same one met early
    public static float BossHp(int layerIndex) =>
        1f + BOSS_HP_PER_LAYER * Growth * Math.Max(0, layerIndex);

    public static float BossDamage(int layerIndex) =>
        1f + BOSS_DAMAGE_PER_LAYER * Growth * Math.Max(0, layerIndex);

    // HOW FAST THE ROOMS FILL UP. Separate from Growth above - a mob count is a
    // small whole number, so it needs a gentler curve than a boss HP bar.
    private const float COUNT_GROWTH_STEP = 0.4f;
    private static float CountGrowth => 1f + COUNT_GROWTH_STEP * Step;

    //
    // HOW MANY OF A MOB A LAYER ASKS FOR
    //
    //   baseCount  what layer 0 sends on medium
    //   progress   how far into the climb this layer is, in that mob's own steps
    //   cap        the most this mob may ever send at once
    //
    // The difficulty shifts the STARTING number by one either way AND speeds up
    // or slows the growth, so baby starts lower and falls further behind.
    //
    public static int Count(int baseCount, int progress, int cap = int.MaxValue)
    {
        int wanted = baseCount + Step + (int)MathF.Round(progress * CountGrowth);

        return Math.Clamp(wanted, 0, cap);
    }

    //
    // A DAMAGE FIGURE WITH THE DIFFICULTY IN IT
    //
    // Never rounds a real hit down to nothing. A figure that was ALREADY zero
    // stays zero - that is a mob with no contact damage, not a rounding error.
    //
    public static int Scale(int damage, float scale)
    {
        if (damage <= 0)
            return damage;

        return Math.Max(1, (int)MathF.Round(damage * scale));
    }
}
