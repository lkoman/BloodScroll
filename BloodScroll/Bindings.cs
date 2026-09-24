using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

//
// WHICH KEY DOES WHAT
//
// Every keyboard action the player can rebind, and the key each one is on right
// now. Player reads its keys from here and never names one itself, so a key
// changed on the CONTROLS page is the key the game listens to on the next frame.
//
// THE MOUSE IS NOT IN HERE. Shooting, the sword and the wheel stay where they
// are - only the keyboard can be changed.
//
// ONE KEY PER ACTION, plus two that never move: the arrow keys always walk,
// whatever A and D have been swapped for. Escape always pauses. Neither can be
// given to anything else, so the player can never bind their way out of the
// pause menu or lose the one way of moving that works on every keyboard.
//
// A KEY ALREADY IN USE IS SWAPPED, not refused. Putting jump on F hands the old
// jump key to pick up, so no action is ever left with nothing on it and no key
// ever does two things at once.
//

public enum GameAction
{
    MoveLeft,
    MoveRight,
    Jump,
    Sprint,
    DashLeft,
    DashRight,
    Interact,
    SwitchWeapon,
}

public static class Bindings
{
    // Indexed by GameAction - keep the two in the same order
    private static readonly Keys[] DEFAULTS =
    [
        Keys.A,
        Keys.D,
        Keys.Space,
        Keys.LeftShift,
        Keys.Q,
        Keys.E,
        Keys.F,
        Keys.G,
    ];

    private static readonly Keys[] current = (Keys[])DEFAULTS.Clone();

    public static int Count => DEFAULTS.Length;

    public static Keys Get(GameAction action) => current[(int)action];

    //
    // IS THE ACTION HELD
    //
    // Both shifts, both controls and both alts count as the same key - the
    // binding is stored as the left one (see Normalise), and a player holding
    // the right shift to sprint should not have to know that.
    //
    public static bool IsDown(KeyboardState keys, GameAction action)
    {
        Keys key = current[(int)action];

        if (keys.IsKeyDown(key) || keys.IsKeyDown(Twin(key)))
            return true;

        // The arrows, which walk no matter what
        return action switch
        {
            GameAction.MoveLeft => keys.IsKeyDown(Keys.Left),
            GameAction.MoveRight => keys.IsKeyDown(Keys.Right),
            _ => false,
        };
    }

    // Keys that can never be given to an action. The debug toggles are here
    // too - a key that also shows hitboxes is a bug waiting for someone to find
    // it with the debug build.
    public static bool IsReserved(Keys key) => key is
        Keys.None or Keys.Escape or Keys.Left or Keys.Right or
        Keys.F1 or Keys.F3 or Keys.F4 or
        Keys.LeftWindows or Keys.RightWindows;

    //
    // PUT AN ACTION ON A NEW KEY
    //
    // Returns false for a reserved key and leaves everything as it was.
    // Whatever was already on the new key takes this action's old key.
    //
    public static bool Set(GameAction action, Keys key)
    {
        key = Normalise(key);

        if (IsReserved(key))
            return false;

        int index = (int)action;
        int holder = Array.IndexOf(current, key);

        if (holder >= 0 && holder != index)
            current[holder] = current[index];

        current[index] = key;
        return true;
    }

    public static void ResetToDefaults()
        => Array.Copy(DEFAULTS, current, DEFAULTS.Length);

    public static bool AreDefaults()
    {
        for (int i = 0; i < current.Length; i++)
            if (current[i] != DEFAULTS[i])
                return false;

        return true;
    }

    // The right hand copy of a doubled key is stored as the left one, so the
    // swap in Set sees them as the same key
    private static Keys Normalise(Keys key) => key switch
    {
        Keys.RightShift => Keys.LeftShift,
        Keys.RightControl => Keys.LeftControl,
        Keys.RightAlt => Keys.LeftAlt,
        _ => key,
    };

    private static Keys Twin(Keys key) => key switch
    {
        Keys.LeftShift => Keys.RightShift,
        Keys.LeftControl => Keys.RightControl,
        Keys.LeftAlt => Keys.RightAlt,
        _ => key,
    };

    //
    // WHAT A KEY IS CALLED ON SCREEN
    //
    // The font only has plain ASCII, and "OemComma" means nothing to anyone,
    // so the awkward ones are spelled out and everything else is its enum name
    // in capitals.
    //
    public static string Name(Keys key) => key switch
    {
        Keys.LeftShift or Keys.RightShift => "SHIFT",
        Keys.LeftControl or Keys.RightControl => "CTRL",
        Keys.LeftAlt or Keys.RightAlt => "ALT",
        Keys.Space => "SPACE",
        Keys.Enter => "ENTER",
        Keys.Back => "BACKSPACE",
        Keys.CapsLock => "CAPS LOCK",
        Keys.PageUp => "PAGE UP",
        Keys.PageDown => "PAGE DOWN",
        Keys.OemComma => ",",
        Keys.OemPeriod => ".",
        Keys.OemMinus => "-",
        Keys.OemPlus => "+",
        Keys.OemQuestion => "/",
        Keys.OemSemicolon => ";",
        Keys.OemQuotes => "'",
        Keys.OemOpenBrackets => "[",
        Keys.OemCloseBrackets => "]",
        Keys.OemPipe or Keys.OemBackslash => "\\",
        Keys.OemTilde => "~",
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
        >= Keys.NumPad0 and <= Keys.NumPad9 => "NUM " + (key - Keys.NumPad0),
        _ => key.ToString().ToUpperInvariant(),
    };

    public static string Name(GameAction action) => Name(Get(action));

    //
    // TO AND FROM THE SAVE FILE
    //
    // By NAME on both sides - action name to key name - so reordering either
    // enum never shuffles someone's saved keys onto the wrong actions.
    //
    public static Dictionary<string, string> ToSave()
    {
        var saved = new Dictionary<string, string>();

        for (int i = 0; i < current.Length; i++)
            saved[((GameAction)i).ToString()] = current[i].ToString();

        return saved;
    }

    // All or nothing. A file with a key that does not parse, a reserved key or
    // two actions on one key has been edited by hand or written by some other
    // version, and half applying it could leave an action with no way to press
    // it - so the defaults stand instead. Actions the file does not mention
    // keep their default, so a save from before an action existed still loads.
    public static void FromSave(Dictionary<string, string> saved)
    {
        if (saved == null)
            return;

        Keys[] loaded = (Keys[])DEFAULTS.Clone();

        foreach (var (actionName, keyName) in saved)
        {
            if (!Enum.TryParse(actionName, out GameAction action) || !Enum.IsDefined(action))
                continue;

            if (!Enum.TryParse(keyName, out Keys key) || !Enum.IsDefined(key) || IsReserved(Normalise(key)))
                return;

            loaded[(int)action] = Normalise(key);
        }

        for (int i = 0; i < loaded.Length; i++)
            if (Array.IndexOf(loaded, loaded[i]) != i)
                return;

        Array.Copy(loaded, current, loaded.Length);
    }
}
