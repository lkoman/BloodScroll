using System;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE THREE DIFFICULTIES, WRITTEN ONCE
//
// MEDIUM IS THE GAME. Every constant below is the MEDIUM figure. Baby is that
// same figure bent one step down and hell is it bent one step up, so there is
// no second and third copy of the tuning to keep in step - retuning the game
// means editing the medium number and letting the other two follow it.
//
// Globals.DIFFICULTY is 0 baby / 1 medium / 2 hell, and Step turns that into
// -1 / 0 / +1. Every formula here is built on Step rather than on the setting
// itself, which is what makes medium the thing the other two are measured
// against instead of one end of a scale.
//
// NONE OF THESE IS A BIG LEVER. Each one moves a little: a fifth more HP on a
// mob, a quarter more bite, a swarm that thickens a bit faster, bosses that
// grow a bit harder with the climb. Four small nudges all leaning the same way
// is what makes hell hell - any one of them on its own would only be a
// nuisance, and one big multiplier would make the difference feel arbitrary.
//

public static class Difficulty
{
    // -1 baby, 0 medium, +1 hell
    private static int Step => Globals.DIFFICULTY - 1;

    //
    // AN ORDINARY MOB IS THE SAME MOB ALL THE WAY UP
    //
    // A bat on layer 40 has exactly the HP and exactly the bite of a bat on
    // layer 1. What makes the late layers hard is how MANY of them arrive (see
    // Count) - the swarm IS the difficulty curve. A bat that also quietly grew
    // would mean the player's guns falling behind for a reason he can never
    // see happening, and every mob in this game is meant to be legible.
    //
    private const float MOB_HP_STEP = 0.2f;
    private const float MOB_DAMAGE_STEP = 0.25f;

    public static float MobHp => 1f + MOB_HP_STEP * Step;
    public static float MobDamage => 1f + MOB_DAMAGE_STEP * Step;

    //
    // A BOSS IS THE OPPOSITE, AND GROWS WITH THE CLIMB
    //
    // A boss is a fight rather than a hazard, and the player who reaches layer
    // 30 is not the player who reached layer 5 - he has more HP, a shield, four
    // guns and life steal. A boss written once and never scaled is a boss that
    // gets easier every time the roster comes round.
    //
    // Per layer, on medium. Small figures because they compound with BossTally,
    // which is already making each boss tougher on each of ITS own appearances -
    // this is the climb's share of it, not the whole of it.
    //
    private const float BOSS_HP_PER_LAYER = 0.035f;
    private const float BOSS_DAMAGE_PER_LAYER = 0.025f;

    //
    // HOW FAST ANYTHING TIED TO THE LAYER GROWS
    //
    // The second half of what the difficulty does, and the more important half.
    // The numbers above shift where the game STARTS; this shifts how steeply it
    // climbs from there, which is what the player actually feels over a long
    // run. Baby climbs at just over half the rate, hell at about half again.
    //
    private const float GROWTH_STEP = 0.45f;
    private static float Growth => 1f + GROWTH_STEP * Step;

    // Both take the layer the boss is standing on, so a boss met late is a
    // harder boss than the same one met early
    public static float BossHp(int layerIndex) =>
        1f + BOSS_HP_PER_LAYER * Growth * Math.Max(0, layerIndex);

    public static float BossDamage(int layerIndex) =>
        1f + BOSS_DAMAGE_PER_LAYER * Growth * Math.Max(0, layerIndex);

    //
    // HOW FAST THE ROOMS FILL UP
    //
    // Kept separate from Growth above because a mob count is a small whole
    // number and a multiplier that reads well on a boss's HP bar is far too
    // coarse on "three bats or four". Same shape, gentler.
    //
    private const float COUNT_GROWTH_STEP = 0.4f;
    private static float CountGrowth => 1f + COUNT_GROWTH_STEP * Step;

    //
    // HOW MANY OF A MOB A LAYER ASKS FOR
    //
    //   baseCount  what layer 0 sends on medium
    //   progress   how far into the climb this layer is, in whatever steps that
    //              particular mob has always grown on
    //   cap        the most this mob may ever send at once
    //
    // The difficulty does two things to it, and they are different things on
    // purpose: it shifts the starting number by one either way, and it speeds
    // up or slows down the growth. Baby therefore starts with one fewer AND
    // falls further behind the longer the run goes on, which is what "slower"
    // has to mean for a game with no ending.
    //
    public static int Count(int baseCount, int progress, int cap = int.MaxValue)
    {
        int wanted = baseCount + Step + (int)MathF.Round(progress * CountGrowth);

        return Math.Clamp(wanted, 0, cap);
    }

    //
    // A DAMAGE FIGURE WITH THE DIFFICULTY IN IT
    //
    // Never rounds a hit that was meant to land down to nothing: baby mode
    // makes things hurt less, it does not switch a mob's attack off. A figure
    // that was already zero stays zero - that is a mob which deliberately does
    // no contact damage (the jellyfish, the butterfly), not a rounding error.
    //
    public static int Scale(int damage, float scale)
    {
        if (damage <= 0)
            return damage;

        return Math.Max(1, (int)MathF.Round(damage * scale));
    }
}
