using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

//
// THE DEBUG KEYS
//
// For testing, not for playing. One file and four call sites, so it is easy to
// take out of the build.
//
//   F1   HITBOXES      draws every collision box and outline in the world
//   F2   DEBUG MODE    no damage, the whole arsenal, and a shield
//   F3   ALL WEAPONS   unlocks the whole arsenal on the spot
//   F4   SHIELD        hands over a shield, same one the boss gives
//
// F1 and F2 are TOGGLES and survive a restart. F3 and F4 are one shot grants,
// and a new run takes them away with everything else the player earned.
//
// F2 turns on what F3 and F4 do as well as the invulnerability. They stay as
// their own keys so the guns can be had WITHOUT the invulnerability.
//
// While F2 is on, the guns and shield are handed over ON EVERY RESTART (see
// ApplyToRun), because a new run clears both.
//
// IT IS INVULNERABILITY, NOT INFINITE HP. The shield, the poison floor and the
// death check all read HP, so a huge number would quietly change three other
// systems. Refusing the damage at the door leaves HP untouched instead.
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

            // ON hands over the kit there and then, so it works mid run.
            //
            // OFF takes NOTHING BACK - there is no telling which guns came from
            // it and which were won from a boss. Restart is what clears them.
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
    // Called at the END of every Restart, once the player and guns are back to
    // what a fresh run starts with.
    //
    // The invulnerability is a flag and survives on its own, but the guns and
    // shield are STATE that Restart has just cleared, so they are put back here.
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
