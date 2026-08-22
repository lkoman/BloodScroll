namespace BloodScroll;

//
// What happens when a mob touches the player.
// Dispatched in CollisionResponse.HandlePlayerMobCollision.
//

public enum OnTouch
{
    Nothing,
    HurtPlayer,
    Explode,
    SlowPlayer,
    HealPlayer,

    // Hurts AND poisons - the green bat. Both, because the poison is the slow
    // half and something has to make walking into one hurt straight away.
    PoisonPlayer
}
