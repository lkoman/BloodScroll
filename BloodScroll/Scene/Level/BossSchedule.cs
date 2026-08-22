using System.Collections.Generic;

namespace BloodScroll;

//
// KEEPS THE CLIMB ENDLESS
//
// mobWaves.json describes the first stretch of the game by hand. Past the last
// layer written there, this takes over: every BOSS_EVERY layers is a boss layer,
// cycling through the roster and getting harder each time round.
//
// Returns null for a normal layer, which is exactly what the old code got from
// FirstOrDefault, so those layers still fall back to the WaveData formula.
//

public static class BossSchedule
{
    private const int BOSS_EVERY = 3;

    // Which layer within each group of BOSS_EVERY is the boss
    // (2 keeps the rhythm of the hand authored layers 2, 5, 8)
    private const int BOSS_OFFSET = 2;

    // HOW BOSSES GET TOUGHER IS NOT DECIDED HERE ANY MORE.
    //
    // This used to scale every boss by how many times the ROSTER had come
    // round, which meant a boss the player was meeting for the first time on a
    // late layer arrived pre-scaled by fights it had nothing to do with.
    // BossTally counts each boss's own appearances instead, and every boss in
    // the game goes through it - hand authored ones included.

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

    // Rotated through so no two bosses in a row hand out the same prize.
    // The last gun goes first: the hand authored layers hand out guns 1 and 2,
    // so the stun gun is the one thing left the player has not seen, and it
    // should not be sitting behind four other prizes.
    private static readonly List<GiftData> GiftPool =
    [
        new() { GunID = 3 },
        new() { Shield = 150 },
        new() { FireRate = 1.25f },
        new() { IncreasedHP = 900 },
        new() { LifeSteal = 4 },
        new() { DoubleJump = true },
    ];

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
            Gifts = GiftPool[bossNumber % GiftPool.Count]
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
