using System.Collections.Generic;

namespace BloodScroll;

//
// KEEPS THE CLIMB ENDLESS
//
// mobWaves.json describes the first stretch by hand. Past the last layer written
// there this takes over: every BOSS_EVERY layers is a boss layer, cycling the
// roster and getting harder each lap.
//
// Returns NULL for a normal layer, so those fall back to the WaveData formula.
//

public static class BossSchedule
{
    private const int BOSS_EVERY = 3;

    // Which layer within each group of BOSS_EVERY is the boss
    // (2 keeps the rhythm of the hand authored layers 2, 5, 8)
    private const int BOSS_OFFSET = 2;

    // HOW BOSSES GET TOUGHER IS NOT DECIDED HERE. BossTally counts each boss's
    // OWN appearances, and every boss goes through it, hand authored included.

    // One entry per boss fight: the boss itself plus whatever it fights alongside.
    // Fireboss goes last because the player already fought it on the hand
    // authored layers - the first generated boss should be something new.
    private static readonly List<Dictionary<MobType, int>> BossRoster =
    [
        new() { [MobType.SpiderQueen] = 1, [MobType.Spider] = 2 },
        new() { [MobType.Moth] = 1, [MobType.Cocoon] = 1, [MobType.Butterfly] = 2 },
        new() { [MobType.ShadowTwin] = 1, [MobType.PurpleBat] = 3 },
        new() { [MobType.Hive] = 1 },
        new() { [MobType.Fireboss] = 1, [MobType.Bat] = 4 },
    ];

    //
    // THE PRIZES THAT CAN ONLY BE WON ONCE
    //
    // One per boss, in this order, then never again. The last gun goes first -
    // the hand authored layers already hand out guns 1 and 2.
    //
    // NOT cycled: these carry ABSOLUTE numbers, and a second "IncreasedHP = 900"
    // on a player past 900 would take HP off him. Past the end of this list the
    // bosses pay out in increments - see Upgrade.
    //
    private static readonly List<GiftData> GiftPool =
    [
        new() { GunID = 3 },
        new() { Shield = 150 },
        new() { FireRate = 1.25f },
        new() { IncreasedHP = 900 },
        new() { LifeSteal = 4 },
        new() { DoubleJump = true },
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
    // The gun moves on each lap, so all four are brought along evenly. Speed and
    // damage land on the same gun in the same lap.
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
    private static GiftData GiftFor(int bossNumber) =>
        bossNumber < GiftPool.Count
            ? GiftPool[bossNumber]
            : Upgrade(bossNumber - GiftPool.Count);

    public static WaveData Generate(int layerIndex, int maxAuthoredLayerIndex)
    {
        // The hand written layers own everything up to and including their last entry
        if (layerIndex <= maxAuthoredLayerIndex)
            return null;

        if (layerIndex % BOSS_EVERY != BOSS_OFFSET)
            return null;

        // Counted from the FIRST generated boss, so the roster always starts at
        // its first entry no matter how far the hand authored layers reach
        int bossNumber = (layerIndex - FirstBossLayerAfter(maxAuthoredLayerIndex)) / BOSS_EVERY;

        WaveData wave = new(layerIndex)
        {
            LayerType = LayerType.Boss,
            WavesToBeat = 1,
            Gifts = GiftFor(bossNumber)
        };

        // A boss layer is only the boss and its escort, so start from nothing
        wave.Mobs.Clear();
        foreach (var (type, count) in BossRoster[bossNumber % BossRoster.Count])
            wave.Mobs[type] = count;

        return wave;
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
