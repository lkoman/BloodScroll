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

    //
    // HITTING A ROUND SCORE
    //
    // ONE FILE, NOT TWO. drumroll.wav carries the roll AND the cymbal that
    // ends it, so there is nothing to play at the pop - the crash is already
    // coming out of the speakers on the frame the confetti goes off, because
    // the animation is cut to the length of the roll. See ScoreCelebration.
    //
    ScoreDrumroll,

}
