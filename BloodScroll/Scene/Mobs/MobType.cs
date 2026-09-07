namespace BloodScroll;

//
// Every mob the game can spawn. Used as the key in WaveData.Mobs
// (so mobWaves.json writes "Mobs": { "Bat": 4 }) and in MobManager's registry.
//

public enum MobType
{
    // BASIC
    Bat,
    PurpleBat,
    Slime,
    Crab,
    Jellyfish,
    Fireball,
    Flower,
    Spider,
    Butterfly,
    GreenBat,
    GreenSpider,

    // BOSSES
    Fireboss,
    SpiderQueen,
    Moth,
    Cocoon,
    ShadowTwin,
    Hive
}
