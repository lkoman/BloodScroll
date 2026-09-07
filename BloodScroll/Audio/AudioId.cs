public enum AudioId
{
    // AUDIO
    MenuMusic,
    GameMusic,
    BossMusic,

    //
    // SOUNDS
    //
    // Menu
    ButtonClick,
    ButtonHover,

    // Player
    PlayerGun,
    PlayerJump,
    PlayerHit,

    // The sword. NO FILE FOR IT YET - see AudioService, where every other
    // sound is loaded. PlaySound looks the id up and finds nothing, so the
    // swing is simply silent until one is dropped in.
    SwordSwing,

    // Monsters
    BatSqueak,

    // Bosses
    FireHit,

}
