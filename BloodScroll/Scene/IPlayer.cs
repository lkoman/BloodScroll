using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace BloodScroll;

public interface IPlayer
{
    int HP { get; set; }
    int MaxHP { get; }
    float Bottom { get; }
    float Height { get; }
    float Width { get; }
    bool InAir { get; }
    Vector2 Position { get; }

    //
    // HIS TWO HITBOXES, AND THERE IS NO THIRD.
    //
    // The sprite frame used to be offered here as well and everything reached
    // for it, which is how a hood 120 pixels across ended up deciding whether
    // his feet were on a ledge. Ask for whichever of these two actually
    // answers your question.
    //

    // The body, as an outline. Every mob, shot, web and blast is tested
    // against this - see the note on HURTBOX_SHAPE in Player.
    Polygon HurtBox { get; }

    // Only as wide as his feet. Platforms, and nothing else: it is what
    // decides whether there is anything under him to stand on.
    Rectangle FootingBounds { get; }

    // GIFT STATE (shown in the HUD)
    int Shield { get; }
    int ShieldMax { get; }
    bool HasDoubleJump { get; }
    bool CanJumpAgain { get; }
    bool IsSlowed { get; }

    //
    // THE FLOWER BOMB
    //
    // One pair of hands, one bomb. E does whichever of the two things is
    // possible: put down the bomb you are holding, or pluck a flower if you
    // are not holding one.
    //
    // The key press is offered around rather than acted on where it is read,
    // because more than one thing wants it and only one of them may have it.
    // Whoever takes it first wins, and the world asks before the flowers do -
    // so putting a bomb down always beats picking another one up.
    bool HasBomb { get; }
    bool TryTakeInteract();
    void GiveBomb();
    void UseBomb();

    void GrantShield(int capacity);
    void GrantDoubleJump();
    void GrantLifeSteal(int hpPerKill);
    void RefillShield();

    void Heal();
    void Heal(int amount);
    void IncreaseMaxHP(int newMaxHP);
    void TakeDamage(int damage, IAudioService audio);
    void ApplySlow(float factor, float seconds);

    //
    // POISON (the green bat)
    //
    // Bleeds HP away for a while and CANNOT KILL - it stops at a floor. It is
    // there to make a fight worse, not to end it: dying to a debuff ticking on
    // an empty screen would take the death out of the player's hands, and every
    // other way of dying in this game is something he can see coming.
    bool IsPoisoned { get; }
    void ApplyPoison(int totalDamage, float seconds);

    // Rooted to the spot (the green spider's web). No steering, no jumping.
    bool IsRooted { get; }
    void Root(float seconds);
    void Knockback(Vector2 direction, float force);
    void OnMobKilled();
    void PlacePlayerOnPlatform(float platformY);
    void SetPlayerInAir(bool b);
    bool IsPlayerStandingOnPlatform(Rectangle platform);
}
