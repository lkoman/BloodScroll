using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

//
// THE DEBUG KEYS
//
// For testing, not for playing. One file and four call sites, so it is easy to
// take out of the build.
//
// THE WHOLE THING IS OFF UNLESS THE SOURCE SAYS OTHERWISE. Set ENABLED below,
// rebuild, and the keys come alive; shipped as it stands, a player leaning on
// the function row finds nothing at all. There is deliberately NO key that
// turns this on - it used to be F2, which meant the invulnerability was one
// keystroke away for anybody who went looking, and a high score table is worth
// nothing if beating it is a matter of finding the right function key.
//
//   F1   HITBOXES      draws every collision box and outline in the world
//   F3   ALL WEAPONS   unlocks the whole arsenal on the spot
//   F4   SHIELD        hands over a shield, same one the boss gives
//
// F1 is a TOGGLE and survives a restart. F3 and F4 are one shot grants, and a
// new run takes them away with everything else the player earned.
//
// INVULNERABLE is its own switch, set in the source beside ENABLED, so the guns
// can be had WITHOUT it. While it is on, the guns and shield are handed over on
// EVERY RESTART (see ApplyToRun), because a new run clears both.
//
// IT IS INVULNERABILITY, NOT INFINITE HP. The shield, the poison floor and the
// death check all read HP, so a huge number would quietly change three other
// systems. Refusing the damage at the door leaves HP untouched instead.
//
public static class DebugMode
{
    //
    // THE ONE SWITCH, AND IT IS IN THE SOURCE
    //
    // static readonly rather than const on purpose: a const false would make
    // everything guarded by it unreachable, and the compiler would rightly
    // warn about every line of it.
    //
    private static readonly bool ENABLED = false;

    // Set this alongside ENABLED when the run should also be unkillable.
    // Separate, because reading the hitboxes or trying a late gun is usually
    // wanted WITHOUT taking the danger out of the game at the same time.
    private static readonly bool INVULNERABLE = true;

    // Damage never reaches the player while this is on. See Player.TakeDamage
    // and Player.UpdatePoison - those are the only two ways HP ever goes down.
    public static bool Invulnerable => ENABLED && INVULNERABLE;

    // Off by default. It is a tool for tuning the HitboxScale on a mob - the
    // box should hug the creature, not the empty space around it.
    public static bool ShowHitboxes => ENABLED && showHitboxes;

    private static bool showHitboxes = false;

    // The same capacity the boss hands out (see BossSchedule), so what F4 gives
    // you is the real gift and not a debug-only version of it that behaves
    // differently from the one the player will actually get.
    private const int DEBUG_SHIELD = 100;

    private static KeyboardState lastKeyState;

    public static void Update(Player player, WeaponsManager weapons)
    {
        if (!ENABLED)
            return;

        KeyboardState keyState = Keyboard.GetState();

        if (Pressed(keyState, Keys.F1))
            showHitboxes = !showHitboxes;

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
    // The invulnerability is a source switch and needs no help, but the guns
    // and shield are STATE that Restart has just cleared, so they are put back
    // here.
    //
    public static void ApplyToRun(Player player, WeaponsManager weapons)
    {
        if (!Invulnerable)
            return;

        weapons.UnlockAllWeapons();
        player.GrantShield(DEBUG_SHIELD);
    }

    // The press, never the hold - otherwise a toggle would flicker sixty times
    // a second for as long as the key is down
    private static bool Pressed(KeyboardState keyState, Keys key) =>
        keyState.IsKeyDown(key) && lastKeyState.IsKeyUp(key);
}
