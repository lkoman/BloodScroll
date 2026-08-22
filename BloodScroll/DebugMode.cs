using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

//
// THE DEBUG KEYS
//
// Everything here is for testing the game, not for playing it. It lives in one
// place so there is exactly one list of what the function keys do, and so the
// day this has to come out of the build it is one file and four call sites.
//
//   F1   HITBOXES      draws every collision box and outline in the world
//   F2   DEBUG MODE    no damage, the whole arsenal, and a shield
//   F3   ALL WEAPONS   unlocks the whole arsenal on the spot
//   F4   SHIELD        hands over a shield, same one the boss gives
//
// F1 and F2 are TOGGLES and survive a restart - they are switches you leave in
// a position while you work on something. F3 and F4 are one shot grants, and a
// new run takes them away again along with everything else the player earned,
// because that is what starting a run means.
//
// DEBUG MODE IS THE WHOLE KIT AT ONCE - it turns on what F3 and F4 do as well
// as the invulnerability, so getting to the interesting part of the game is one
// key rather than three. F3 and F4 stay as their own keys because the point of
// them is being able to have the guns WITHOUT the invulnerability: a fight you
// can actually lose is the only way to tell whether a gun is any good.
//
// The guns and the shield are handed over ON EVERY RESTART while it is on (see
// ApplyToRun), because a new run takes both away - otherwise debug mode would
// quietly stop meaning anything the first time you died.
//
// DEBUG MODE IS INVULNERABILITY, NOT INFINITE HP. Infinite HP would mean a
// number in the HUD that never moves and a health bar that is always full,
// which looks exactly like a bug - and the shield, the poison floor and the
// death check all read HP, so a huge number quietly changes how three other
// systems behave. Refusing the damage at the door leaves all of them alone: HP
// simply never changes, everything else in the game runs exactly as written.
//
public static class DebugMode
{
    // Damage never reaches the player while this is on. See Player.TakeDamage
    // and Player.UpdatePoison - those are the only two ways HP ever goes down.
    public static bool Invulnerable { get; private set; } = false;

    // Off by default. It is a tool for tuning the HitboxScale on a mob - the
    // box should hug the creature, not the empty space around it.
    public static bool ShowHitboxes { get; private set; } = false;

    // The same capacity the boss hands out (see mobWaves.json), so what F4
    // gives you is the real gift and not a debug-only version of it that
    // behaves differently from the one the player will actually get.
    private const int DEBUG_SHIELD = 100;

    private static KeyboardState lastKeyState;

    public static void Update(Player player, WeaponsManager weapons)
    {
        KeyboardState keyState = Keyboard.GetState();

        if (Pressed(keyState, Keys.F1))
            ShowHitboxes = !ShowHitboxes;

        if (Pressed(keyState, Keys.F2))
        {
            Invulnerable = !Invulnerable;

            // Switching it ON hands over the kit there and then, so it works in
            // the middle of a run and not only from the next one.
            //
            // Switching it OFF takes NOTHING BACK. By the time you turn it off
            // there is no telling which guns were a gift from it and which were
            // won from a boss twenty layers ago, and stripping a player of
            // something he earned is far worse than leaving him a shield he
            // did not. Restart is the thing that clears it, same as everything.
            if (Invulnerable)
                Grant(player, weapons);
        }

        if (Pressed(keyState, Keys.F3))
            weapons.UnlockAllWeapons();

        if (Pressed(keyState, Keys.F4))
            player.GrantShield(DEBUG_SHIELD);

        lastKeyState = keyState;
    }

    //
    // Called at the end of every Restart, once the player and the guns have
    // been wiped back to what a fresh run starts with.
    //
    // This is what makes the toggle mean something across a run boundary: the
    // invulnerability is a flag and survives on its own, but the guns and the
    // shield are STATE that Restart has just cleared, so they have to be put
    // back or debug mode would be two thirds gone after the first death.
    //
    public static void ApplyToRun(Player player, WeaponsManager weapons)
    {
        if (!Invulnerable)
            return;

        Grant(player, weapons);
    }

    // Everything debug mode gives that is not the invulnerability itself.
    // Exactly what F3 and F4 do, so the three keys can never drift apart.
    private static void Grant(Player player, WeaponsManager weapons)
    {
        weapons.UnlockAllWeapons();
        player.GrantShield(DEBUG_SHIELD);
    }

    // The press, never the hold - otherwise a toggle would flicker sixty times
    // a second for as long as the key is down
    private static bool Pressed(KeyboardState keyState, Keys key) =>
        keyState.IsKeyDown(key) && lastKeyState.IsKeyUp(key);
}
