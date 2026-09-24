using System.Collections.Generic;

namespace BloodScroll;

//
// EVERY BOSS LAYER IN THE GAME
//
// mobWaves.json describes the GROUND LAYER by hand and nothing else. From
// layer 1 up this owns the climb: every BOSS_EVERY layers is a boss layer,
// walking the loop below and starting it over at the top.
//
// Returns NULL for an ordinary layer, so those fall back to the WaveData
// formula and get the usual swarm.
//
// THE ORDER IS FIXED, AND IT IS FIXED ON PURPOSE. It used to be shuffled per
// seed, which meant a run could open on the hive and close on the fireballs -
// the climb got easier as the player got stronger, which is backwards. The five
// rungs are in the order they are hard in, so every loop ramps.
//
// THE LOOP IS THE UNIT OF PROGRESS. Each time round, every rung is scaled up
// from the last (see LoopScale) and the four fighting rungs pay out the next
// gift. The hive ends the loop and pays the score instead.
//

public static class BossSchedule
{
    private const int BOSS_EVERY = 3;

    // Which layer within each group of BOSS_EVERY is the boss. 2 keeps the
    // rhythm the hand authored layers used to have: 2, 5, 8, ...
    private const int BOSS_OFFSET = 2;

    //
    // THE LOOP, EASIEST FIRST
    //
    // A null is a rung with NO BOSS on it - the fireballs that open every loop.
    // It is still a boss LAYER: boss music, a gift at the end, and a room with
    // nothing in it but the fight. It is just a room the player can win.
    //
    // The hive is last because it is hardest, and because ending on it is what
    // makes the score bonus mean "you cleared the whole thing".
    //
    private static readonly MobType?[] Loop =
    [
        null,                   // four fireballs, and nothing else
        MobType.Fireboss,       // with the bat swarm
        MobType.Moth,           // with her cocoon
        MobType.SpiderQueen,    // with her spiders
        MobType.Hive,           // alone, and it doubles the score
    ];

    // The hive rung, which ends the loop. Always the last one - the whole shape
    // of the thing is "hardest last, and clearing it is worth something".
    private static int LoopEnd => Loop.Length - 1;

    //
    // WHAT IS ON EACH RUNG
    //
    // THE WHOLE POPULATION OF THE ROOM, boss included. WaveData.MergeFrom wipes
    // the normal swarm for a boss layer, so nothing arrives that is not written
    // here - a boss fight is only ever the boss and what belongs to it.
    //
    private static Dictionary<MobType, int> Population(MobType? boss, int layerIndex)
        => boss switch
        {
            // THE WARM UP. No boss, no swarm - four homing hazards in an empty
            // room. It is the rung that teaches the player what a boss layer
            // looks like before one of them is standing in it.
            null => new() { [MobType.Fireball] = FIREBALL_RUNG },

            // HIS BATS. The fireballs are the rung before him, and giving him
            // them as well would make the two rungs the same fight twice.
            MobType.Fireboss => new()
            {
                [MobType.Fireboss] = 1,
                [MobType.Bat] = FIREBOSS_BATS,
            },

            // ALONE, WITH HER OWN COCOON. The cocoon is not an escort - it is
            // the thing she comes out of and retreats into, and without one on
            // the layer she never stops attacking (see Moth.HasShelter). One
            // each, because two moths sharing one would fight over whether it
            // is open or shut.
            MobType.Moth => new()
            {
                [MobType.Moth] = Moths(layerIndex),
                [MobType.Cocoon] = Moths(layerIndex),
            },

            // HER SPIDERS AND NOTHING ELSE. Not the green one - that roots the
            // player to the spot, and being unable to move while she lines up
            // a ram is not a fight, it is a cutscene.
            MobType.SpiderQueen => new()
            {
                [MobType.SpiderQueen] = 1,
                [MobType.Spider] = SPIDER_QUEEN_SPIDERS,
            },

            // ALONE, AND IT STAYS THAT WAY. It fills its own room - the hive
            // spits out bats, purple bats and fireballs on its own timer, so
            // anything written here would be arriving on top of that.
            MobType.Hive => new() { [MobType.Hive] = 1 },

            _ => new() { [boss.Value] = 1 },
        };

    private const int FIREBALL_RUNG = 4;
    private const int FIREBOSS_BATS = 5;

    // THREE, BECAUSE THREE IS ALL IT CAN BE. WaveData.Cap holds the ceiling
    // spider to three wherever the count is written, boss layer included -
    // asking for more here would quietly get three anyway and leave this line
    // lying about the fight.
    private const int SPIDER_QUEEN_SPIDERS = 3;

    // WHERE THE SECOND MOTH STARTS TURNING UP. Late, because two of them is a
    // genuinely different fight: each has her own cocoon to hide in, so there
    // is twice as much of the arena the player cannot safely stand in.
    private const int SECOND_MOTH_FROM_LAYER = 30;

    private static int Moths(int layerIndex)
        => layerIndex >= SECOND_MOTH_FROM_LAYER ? 2 : 1;

    //
    // HOW MUCH HARDER EACH LOOP IS THAN THE LAST
    //
    // Applied as the LAYER's HpScale, so it lifts everything in the room -
    // the escorts and the bossless fireball rung included. Those get nothing
    // from BossTally, which only ever counted bosses, so without this the
    // fireballs on loop five would be the fireballs from loop one.
    //
    // IT COMPOUNDS WITH BossTally ON THE BOSSES THEMSELVES, which already grow
    // on each of their own appearances - and with a fixed order an appearance
    // IS a loop. So a boss on loop N is carrying both curves at once. That is
    // deliberate, and this is the constant to pull down if the back half of a
    // long run starts feeling like a wall rather than a climb.
    //
    private const float LOOP_HP_STEP = 0.25f;

    private static float LoopScale(int loop) => 1f + LOOP_HP_STEP * loop;

    //
    // WHAT CLEARING A WHOLE LOOP IS WORTH
    //
    // The hive pays this INSTEAD of a gift. It multiplies the score already
    // banked rather than adding to it, so it is worth more the deeper the run
    // has gone - and it compounds, which is what makes a fifth loop worth
    // reaching rather than just survivable.
    //
    private const float LOOP_SCORE_MULTIPLIER = 2f;

    //
    // THE PRIZES THAT CAN ONLY BE WON ONCE
    //
    // One per FIGHTING rung - the hive is paid in score and takes nothing off
    // this list, so a loop hands out four of them.
    //
    // Every one carries an ABSOLUTE number, which is why they are never
    // cycled: a second "IncreasedHP = 900" on a player already past 900 would
    // be taking HP off him. Past the end of this list the bosses pay out in
    // increments instead - see Upgrade.
    //
    private static readonly List<GiftData> GiftPool =
    [
        new() { IncreasedHP = 600, Sword = true },
        new() { GunID = 1 },
        new() { IncreasedHP = 750, GunID = 2 },
        new() { Shield = 100 },
        new() { DoubleJump = true },
        new() { GunID = 3 },
        new() { Shield = 150 },
        new() { FireRate = 1.25f },
        new() { IncreasedHP = 900 },
        new() { LifeSteal = 4 },
    ];

    //
    // AND THE ONES THAT KEEP COMING
    //
    // Four gifts, cycled forever, each a small permanent step:
    //
    //   more HP        the player's ceiling keeps rising with the bosses'
    //   more shield    the same, for the part that grows back
    //   a faster gun   ONE gun, a notch quicker
    //   a stronger gun THE SAME gun, a notch harder hitting
    //
    // The gun moves on each lap, so all four are brought along evenly. Speed
    // and damage land on the same gun in the same lap.
    //
    // NONE OF THIS MOVES WITH THE DIFFICULTY - that scales the bosses, not what
    // the player is handed for beating one.
    //
    private const int UPGRADE_CYCLE = 4;

    // Small, because there is no last boss. Twenty of these is +2400 HP.
    private const int HP_STEP = 120;
    private const int SHIELD_STEP = 60;

    private static GiftData Upgrade(int upgradeNumber)
    {
        // Which gun this lap is improving, walked one step per full cycle
        int gun = upgradeNumber / UPGRADE_CYCLE % WeaponsManager.GunCount;

        return (upgradeNumber % UPGRADE_CYCLE) switch
        {
            0 => new GiftData { BonusHP = HP_STEP },
            1 => new GiftData { BonusShield = SHIELD_STEP },
            2 => new GiftData { FasterGun = gun },
            _ => new GiftData { StrongerGun = gun },
        };
    }

    // The unlocks first, in order, and increments for every boss after them
    private static GiftData GiftFor(int giftNumber) =>
        giftNumber < GiftPool.Count
            ? GiftPool[giftNumber]
            : Upgrade(giftNumber - GiftPool.Count);

    public static WaveData Generate(int layerIndex, int maxAuthoredLayerIndex)
    {
        // The hand written layers own everything up to and including their last
        // entry, which is now the ground layer alone
        if (layerIndex <= maxAuthoredLayerIndex)
            return null;

        if (layerIndex % BOSS_EVERY != BOSS_OFFSET)
            return null;

        // Counted from the FIRST generated boss, so the loop always starts on
        // its first rung however far the hand authored layers reach
        int bossNumber = (layerIndex - FirstBossLayerAfter(maxAuthoredLayerIndex)) / BOSS_EVERY;

        int rung = bossNumber % Loop.Length;
        int loop = bossNumber / Loop.Length;

        WaveData wave = new(layerIndex)
        {
            LayerType = LayerType.Boss,
            WavesToBeat = 1,
            Gifts = Reward(bossNumber, rung),

            // Lifts the whole room, loop by loop
            HpScale = LoopScale(loop),
        };

        // Written into an empty table rather than over the generated one -
        // MergeFrom is what clears the swarm, and it is the only thing that can
        wave.Mobs.Clear();

        foreach (var (type, count) in Population(Loop[rung], layerIndex))
            wave.Mobs[type] = count;

        return wave;
    }

    //
    // WHAT THIS RUNG PAYS
    //
    // The hive gets the score multiplier and nothing else. Every other rung
    // takes the next gift off the pool - and because the hive takes none, the
    // gift count is the boss count MINUS the loops finished behind it, which
    // is what keeps the sword on the very first fight of the game whatever the
    // loop length is set to.
    //
    private static GiftData Reward(int bossNumber, int rung)
    {
        if (rung == LoopEnd)
            return new GiftData { ScoreMultiplier = LOOP_SCORE_MULTIPLIER };

        return GiftFor(bossNumber - bossNumber / Loop.Length);
    }

    // The first layer past the hand authored ones that lands on the boss beat
    private static int FirstBossLayerAfter(int maxAuthoredLayerIndex)
    {
        int layer = maxAuthoredLayerIndex + 1;

        while (layer % BOSS_EVERY != BOSS_OFFSET)
            layer ++;

        return layer;
    }
}
