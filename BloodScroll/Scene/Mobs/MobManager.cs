using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace BloodScroll;

//
// Handles spawning, updating, drawing of mobs ON A LAYER
//
// Adding a new mob means two things: a value in the MobType enum,
// and one line in the Registry below. Nothing else in here changes.
//

public class MobManager
{
    // WHAT EACH MOB TYPE NEEDS WHEN IT SPAWNS
    //   PreSpawnAsleep  - created with the whole layer, then woken one wave at a time
    //   NeedsPlatform   - placed on a platform at layer generation instead of in a wave
    //   NeedsGround     - walks the ground slab, so it can only exist on the ground layer
    //   PreSpawnAtLayer - scenery: hung at layer generation and simply there from
    //                     the moment the player walks in, never part of a wave
    private record MobSpec(Func<IMob> Create, bool PreSpawnAsleep = false, bool NeedsPlatform = false, bool NeedsGround = false, bool PreSpawnAtLayer = false);

    private static readonly Dictionary<MobType, MobSpec> Registry = new()
    {
        [MobType.Bat]       = new(() => new Bat(), PreSpawnAsleep: true),
        [MobType.PurpleBat] = new(() => new PurpleBat(), PreSpawnAsleep: true),
        [MobType.Slime]     = new(() => new Slime()),
        [MobType.Crab]      = new(() => new Crab(), NeedsGround: true),
        [MobType.Jellyfish] = new(() => new JellyFish()),
        [MobType.Fireball]  = new(() => new Fireball()),
        [MobType.Flower]    = new(() => new Flower(), NeedsPlatform: true),
        [MobType.Spider]    = new(() => new Spider()),
        [MobType.Butterfly] = new(() => new Butterfly()),
        [MobType.GreenBat]  = new(() => new GreenBat(), PreSpawnAsleep: true),
        [MobType.BlackSpider] = new(() => new BlackSpider()),

        [MobType.Fireboss]    = new(() => new Fireboss()),
        [MobType.SpiderQueen] = new(() => new SpiderQueen()),
        [MobType.Moth]        = new(() => new Moth()),
        [MobType.Cocoon]      = new(() => new Cocoon(), PreSpawnAtLayer: true),
        [MobType.ShadowTwin]  = new(() => new ShadowTwin()),
        [MobType.Hive]        = new(() => new Hive()),
    };

    // Anything wider than this is the ground slab, not a jumpable ledge
    private const float MAX_PLATFORM_MOB_WIDTH = 500f;

    // Mobs spawn in this order every wave. It is fixed on purpose: spawning draws
    // from the same seeded RNG as level generation, so a stable order keeps runs
    // reproducible. New mobs go on the end.
    private static readonly MobType[] SpawnOrder =
    [
        MobType.Jellyfish,
        MobType.Crab,
        MobType.Slime,
        MobType.Fireball,
        MobType.Fireboss,
        MobType.Flower,
        MobType.Spider,
        MobType.Butterfly,
        MobType.BlackSpider,
        MobType.SpiderQueen,
        MobType.Moth,
        MobType.Cocoon,
        MobType.ShadowTwin,
        MobType.Hive,
        MobType.Bat,
        MobType.PurpleBat,
        MobType.GreenBat,
    ];

    // LIST OF REFERENCES TO ALL MOBS
    public readonly List<IMob> mobs = [];

    public WaveData waveData;

    // Pre-created dormant mobs, plus how many of each we have already woken
    private readonly Dictionary<MobType, List<ISleepingMob>> sleepers = [];
    private readonly Dictionary<MobType, int> wokenCount = [];

    // Mobs asked for mid-update (a hive spitting out adds). Added after the
    // update loop finishes so we never grow the list we are iterating.
    private readonly List<IMob> pendingSpawns = [];

    private Vector2 playerPos;

    // MOB WAVES
    public bool wavesTriggered = false;
    public bool AllEnemiesBeaten = false;

    //
    // THE BOSS
    //
    // A boss layer is about ONE mob, and the escort it comes with is scenery
    // with teeth. Clearing the layer used to mean killing every last bat the
    // boss had brought along, which turned the end of a good boss fight into a
    // couple of minutes of chasing stragglers round an empty room.
    //
    // bossSpawned is what tells "the boss is dead" apart from "the boss has not
    // turned up yet" - both look like an empty list of bosses.
    private bool bossSpawned = false;
    public bool BossBeaten => bossSpawned && !mobs.Any(m => m.IsBoss);

    public MobManager(int currentLayerIndex, WaveData data)
    {
        waveData = new(currentLayerIndex);
        waveData.MergeFrom(data);
    }

    public void Update(IPlayer player, int layerIndex, GameWorld gameWorld)
    {
        playerPos = player.Position;

        WaveTimer(layerIndex, player.Position);

        // Update all mobs and remove if killed
        for (int i = mobs.Count - 1; i >= 0; i--)
        {
            IMob mob = mobs[i];
            mob.Update(player, gameWorld);

            if (mob.HP <= 0)
            {
                mobs.RemoveAt(i);
                Globals.POINTS += mob.PointsOnKill;

                // Only mobs the player actually killed pay out life steal. A
                // jellyfish burning its own fuse down and a butterfly finishing
                // its heal both die on their own, and healing off those would
                // turn the two mobs you are told to avoid into a health tap.
                if (mob.GivesLifeSteal)
                    player.OnMobKilled();
            }
        }

        DrainPendingSpawns();

        // Butterflies and untriggered flowers do not hold the layer hostage
        AllEnemiesBeaten = !mobs.Any(m => m.CountsAsEnemy);
    }

    public List<IDrawableLayer> GetDrawables()
    {
        var list = new List<IDrawableLayer>();

        // Add all monsters
        foreach (var m in mobs)
            list.Add(m);

        return list;
    }

    // Called by mobs that spawn other mobs while the update loop is running.
    // The point is where the middle of the new mob lands, because what asks
    // for one is a mob letting it out of the middle of itself.
    public void RequestSpawn(MobType type, Vector2 centre, int spawnLayer)
    {
        if (!Registry.TryGetValue(type, out var spec))
            return;

        // Same rule as a wave spawn - see GenerateWave
        if (spec.NeedsGround && spawnLayer != LayerGenerator.GROUND_LAYER_INDEX)
            return;

        IMob mob = spec.Create();
        mob.LoadContent(playerPos, spawnLayer);
        mob.MoveCentreTo(centre);
        mob.ScaleHP(waveData.HpScale);

        // Anything spawned this way arrives awake - it was asked for, not
        // pre placed to be woken later
        if (mob is ISleepingMob sleeper)
            sleeper.WakeUp(playerPos);

        pendingSpawns.Add(mob);
    }

    private void DrainPendingSpawns()
    {
        if (pendingSpawns.Count == 0)
            return;

        foreach (IMob mob in pendingSpawns)
            AddMob(mob);

        pendingSpawns.Clear();
    }

    private void WaveTimer(int layerIndex, Vector2 playerPos)
    {
        // TIMER THAT COUNTS DOWN TO THE NEXT WAVE
        waveData.WavesTimer += Globals.DT;

        if (waveData.WavesTimer >= waveData.WavesTimerSeconds)
        {
            if (waveData.WavesToBeat >= 1)
            {
                GenerateWave(layerIndex, playerPos);
                waveData.WavesToBeat --;
            }
            waveData.WavesTimer = 0.0f;
        }
    }

    // Dormant mobs for every wave at once, done when the layer is built
    public void GenerateAllSleepingMobs(int layerIndex)
    {
        sleepers.Clear();
        wokenCount.Clear();

        int waves = waveData.WavesToBeat ?? 0;

        foreach (MobType type in SpawnOrder)
        {
            if (!Registry.TryGetValue(type, out var spec) || !spec.PreSpawnAsleep)
                continue;

            var pool = new List<ISleepingMob>();
            sleepers[type] = pool;
            wokenCount[type] = 0;

            for (int i = 0; i < waves * waveData.MobCount(type); i++)
            {
                var mob = (ISleepingMob)spec.Create();
                mob.LoadContent(playerPos, layerIndex);

                pool.Add(mob);
                AddMob(mob);
            }
        }
    }

    // Scenery that belongs to the room rather than to a wave. Built with the
    // layer, so the player walks in on the cocoon already hanging there, shut,
    // before anything has come out of it - the room tells you what fight this
    // is before the fight starts.
    public void GenerateSceneryMobs(int layerIndex)
    {
        foreach (MobType type in SpawnOrder)
        {
            if (!Registry.TryGetValue(type, out var spec) || !spec.PreSpawnAtLayer)
                continue;

            for (int i = 0; i < waveData.MobCount(type); i++)
            {
                IMob mob = spec.Create();
                mob.LoadContent(playerPos, layerIndex);

                AddMob(mob);
            }
        }
    }

    // Mobs that sit on the terrain. Done when the layer is built, because they
    // need the platforms to exist first. Only small platforms count - a flower
    // on the 1920 wide ground slab would be silly.
    public void GeneratePlatformMobs(IReadOnlyList<Platform> platforms, int layerIndex)
    {
        foreach (MobType type in SpawnOrder)
        {
            if (!Registry.TryGetValue(type, out var spec) || !spec.NeedsPlatform)
                continue;

            int count = waveData.MobCount(type);
            if (count <= 0)
                continue;

            // Only the small jumpable platforms, never the wide ground slab
            var free = platforms.Where(p => p.Width <= MAX_PLATFORM_MOB_WIDTH).ToList();

            for (int i = 0; i < count && free.Count > 0; i++)
            {
                // Pick a platform and take it out of the running so two
                // flowers never end up stacked on the same ledge
                int pick = Globals.R.Next(free.Count);
                Platform platform = free[pick];
                free.RemoveAt(pick);

                var mob = (IPlatformMob)spec.Create();
                mob.LoadContent(playerPos, layerIndex);
                mob.PlaceOnPlatform(platform, platforms);

                AddMob(mob);
            }
        }
    }

    public void GenerateWave(int layerIndex, Vector2 playerPos)
    {
        foreach (MobType type in SpawnOrder)
        {
            int count = waveData.MobCount(type);
            if (count <= 0 || !Registry.TryGetValue(type, out var spec) ||
                spec.NeedsPlatform || spec.PreSpawnAtLayer)
                continue;

            // A crab walks the ground slab, and only the ground layer has one.
            // Higher up it would be pacing along the bottom of the screen on
            // thin air, so it simply does not turn up. This holds even if the
            // JSON asks for crabs on a layer above - there is nowhere to put them.
            if (spec.NeedsGround && layerIndex != LayerGenerator.GROUND_LAYER_INDEX)
                continue;

            if (spec.PreSpawnAsleep)
                WakeSleepers(type, count, playerPos);
            else
                SpawnMobs(type, spec, count, layerIndex);
        }
    }

    private void SpawnMobs(MobType type, MobSpec spec, int count, int spawnLayer)
    {
        for (int i = 0; i < count; i++)
        {
            IMob mob = spec.Create();
            mob.LoadContent(playerPos, spawnLayer);

            // A boss is scaled by how many times the player has already beaten
            // THAT boss (see BossTally); everything else takes the layer's own
            // scale, which is only ever set by hand in the JSON.
            mob.ScaleHP(mob.IsBoss
                ? waveData.HpScale * BossTally.NextScale(type)
                : waveData.HpScale);

            AddMob(mob);
        }
    }

    // Every mob in the game arrives through here, so this is the one place
    // that has to notice a boss turning up
    private void AddMob(IMob mob)
    {
        mobs.Add(mob);

        if (mob.IsBoss)
            bossSpawned = true;
    }

    private void WakeSleepers(MobType type, int count, Vector2 playerPos)
    {
        if (!sleepers.TryGetValue(type, out var pool))
            return;

        for (int i = 0; i < count; i++)
        {
            // The pool is sized from the wave settings at layer generation,
            // so a JSON tweak mid run could ask for more than we made
            if (wokenCount[type] >= pool.Count)
                return;

            pool[wokenCount[type]].WakeUp(playerPos);
            wokenCount[type] ++;
        }
    }
}
