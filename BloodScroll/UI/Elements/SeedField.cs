using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;

namespace BloodScroll;

//
// THE SEED FIELD
//
// One slab the size of a menu button carrying three things: the word SEED on
// the left, the number itself on the right, and a reroll square at the end.
//
// It is NOT a Button with a value. A difficulty button cycles through three
// answers and a click is the whole interaction; this one has a billion answers,
// so the number has to be typed. That means focus, a caret and a key reader,
// none of which a Button has any business growing.
//
// TWO HIT AREAS, ONE SLAB. Clicking the number takes focus and starts editing;
// clicking the reroll square picks a new number and does NOT take focus, so a
// player who only ever wants "give me a different run" never sees a caret.
//
// WHAT IT EDITS IS Globals.SEED ITSELF. There is no second copy to fall out of
// step with it - the field shows the seed the next run will be built from,
// whether that number was typed here, rolled here, or chosen on launch.
//

public class SeedField
{
    private const string LABEL = "SEED";

    // The reroll square at the right end, and the air kept around it
    private const int REROLL_INSET = 10;
    private const int REROLL_PAD = 12;

    // How much of the reroll square the icon is allowed to fill, leaving the
    // rest as air so the glyph does not sit against its own border
    private const float ICON_FILL = 0.56f;

    // Room kept clear at the left end. The same figure Button uses, so the word
    // SEED lines up with the labels stacked above and below it.
    private const int TEXT_PAD = 34;

    // The least gap ever left between the label and the number, so the two can
    // never read as one long word
    private const int LABEL_GAP = 24;

    // How long the caret spends on, and then off, in seconds
    private const float BLINK_SECONDS = 0.53f;

    private readonly int Width;
    private readonly int Height;

    private Rectangle _rect;
    private Rectangle _reroll;
    private Rectangle _entry;

    // 0 = resting, 1 = fully lit. The same walk a Button does.
    private float _hover = 0f;
    private float _rerollHover = 0f;

    // The number AS TYPED, which is not always a number: mid edit it may be
    // empty. Pushed to Globals.SEED on every change, so PLAY never has to ask.
    private string typed = "";
    private bool focused = false;
    private float caretTimer = 0f;

    // The glyph turns a quarter of the way round on each press, so a roll that
    // happens to land on a similar number still LOOKS like it did something
    private float spin = 0f;
    private float spinTimer = 0f;
    private const float SPIN_SECONDS = 0.35f;

    private KeyboardState lastKeys;

    // The digit keys, in both of the places a keyboard keeps them
    private static readonly Keys[] DigitRow =
    [
        Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4,
        Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9
    ];

    private static readonly Keys[] NumPad =
    [
        Keys.NumPad0, Keys.NumPad1, Keys.NumPad2, Keys.NumPad3, Keys.NumPad4,
        Keys.NumPad5, Keys.NumPad6, Keys.NumPad7, Keys.NumPad8, Keys.NumPad9
    ];

    public SeedField(Vector2 size)
    {
        Width = (int)size.X;
        Height = (int)size.Y;
    }

    // Whether the keyboard belongs to this field at the moment. The menu asks
    // so that ESC closing a caret is not also ESC leaving the screen.
    public bool Focused => focused;

    //
    // WHAT THE FIELD SHOWS
    //
    // While focused, exactly what has been typed - including nothing at all, so
    // the box can be cleared and started over without a 0 fighting the caret.
    // Unfocused, the seed itself, which is what the next run will use.
    //
    private string Display => focused ? typed : Globals.SEED.ToString();

    public void Place(Vector2 at)
    {
        _rect = new Rectangle((int)at.X, (int)at.Y, Width, Height);

        int side = Height - REROLL_INSET * 2;

        _reroll = new Rectangle(
            _rect.Right - REROLL_INSET - side,
            _rect.Y + REROLL_INSET,
            side,
            side);

        // Everything left of the reroll square is what you click to type into
        _entry = new Rectangle(
            _rect.X,
            _rect.Y,
            _reroll.Left - REROLL_PAD - _rect.X,
            _rect.Height);
    }

    //
    // THE FRAME'S WORK, IN THE ORDER IT HAS TO HAPPEN
    //
    // Focus is settled BEFORE the keys are read, or the very click that focuses
    // the field would be the frame a held key got swallowed by it.
    //
    public MouseCursor Update(IAudioService audio)
    {
        StepHover();
        StepSpin();

        bool clicked = Clicked();

        if (clicked && _reroll.Contains(Cursor()))
        {
            audio.PlaySound(AudioId.ButtonClick);
            Reroll();
        }
        else if (clicked && _entry.Contains(Cursor()))
        {
            audio.PlaySound(AudioId.ButtonClick);
            Focus();
        }

        // ANYWHERE ELSE PUTS IT AWAY, the PLAY button included - a click that
        // starts a run must not leave a caret blinking on the menu behind it
        else if (clicked)
        {
            Commit();
        }

        if (focused)
            ReadKeys();

        caretTimer += Globals.DT;

        return _rect.Contains(Cursor()) ? MouseCursor.Hand : MouseCursor.Arrow;
    }

    private void StepHover()
    {
        float step = Globals.DT / MathF.Max(0.0001f, UITheme.HoverSpeed);

        // A focused field stays lit whether or not the mouse is still on it -
        // it is the thing the keyboard is pointed at, and it should look it
        bool overField = _entry.Contains(Cursor()) || focused;
        bool overReroll = _reroll.Contains(Cursor());

        _hover = MathHelper.Clamp(_hover + (overField ? step : -step), 0f, 1f);
        _rerollHover = MathHelper.Clamp(_rerollHover + (overReroll ? step : -step), 0f, 1f);
    }

    private void StepSpin()
    {
        if (spinTimer <= 0f)
            return;

        spinTimer = MathF.Max(0f, spinTimer - Globals.DT);

        // Eased, so the glyph lands rather than stops
        float t = 1f - spinTimer / SPIN_SECONDS;

        spin = MathHelper.Lerp(0f, MathHelper.PiOver2, 1f - (1f - t) * (1f - t));
    }

    // A NEW NUMBER, AND NO CARET. Rerolling is the shortcut for players who do
    // not care what the seed is, so it must not drop them into a text field.
    private void Reroll()
    {
        Commit();

        Globals.SEED = Globals.NewSeed();

        spin = 0f;
        spinTimer = SPIN_SECONDS;
    }

    private void Focus()
    {
        if (focused)
            return;

        focused = true;

        // Starts on what is already there, so a small edit stays a small edit
        typed = Globals.SEED.ToString();
        caretTimer = 0f;

        // Taken fresh, or every key held down at the moment of the click reads
        // as having just been pressed
        lastKeys = Keyboard.GetState();
    }

    //
    // PUTTING THE FIELD AWAY
    //
    // AN EMPTY BOX IS NOT A SEED. Rather than refuse it, or quietly write 0 and
    // hand every player who cleared the box the same run, an empty field falls
    // back to whatever the seed was before the edit began.
    //
    public void Commit()
    {
        if (!focused)
            return;

        focused = false;

        if (typed.Length > 0 && int.TryParse(typed, out int value))
            Globals.SEED = Math.Clamp(value, 0, Globals.MAX_SEED);

        typed = "";
    }

    private void ReadKeys()
    {
        KeyboardState keys = Keyboard.GetState();

        for (int digit = 0; digit < 10; digit++)
        {
            if (Pressed(keys, DigitRow[digit]) || Pressed(keys, NumPad[digit]))
                Type((char)('0' + digit));
        }

        if (Pressed(keys, Keys.Back) && typed.Length > 0)
        {
            typed = typed[..^1];
            caretTimer = 0f;
            Push();
        }

        // Both of the ways a player says "done with this"
        if (Pressed(keys, Keys.Enter) || Pressed(keys, Keys.Escape))
            Commit();

        lastKeys = keys;
    }

    // MAX_SEED is nine digits, so nine is where typing stops. Silently - a box
    // that will not take a keystroke has already said so by not changing.
    private void Type(char digit)
    {
        if (typed.Length >= Globals.MAX_SEED.ToString().Length)
            return;

        typed += digit;
        caretTimer = 0f;

        Push();
    }

    // Kept live while typing, so the number on the menu and the number the run
    // would be built from are never two different things
    private void Push()
    {
        if (typed.Length > 0 && int.TryParse(typed, out int value))
            Globals.SEED = Math.Clamp(value, 0, Globals.MAX_SEED);
    }

    private bool Pressed(KeyboardState keys, Keys key)
        => keys.IsKeyDown(key) && lastKeys.IsKeyUp(key);

    //
    // THE MOUSE, BACK IN SCREEN SPACE
    //
    // Globals.MousePosition has the camera taken off it, because everything
    // that asks for it is aiming at the world. THIS SLAB IS NOT IN THE WORLD -
    // MenuLayout places it in screen coordinates - so the camera goes back on.
    //
    // It is not always zero on a menu, either: walking back to the main menu
    // from layer 30 leaves the offset sitting at thirty screens, and a field
    // tested against the raw value would be unclickable for the rest of the
    // session. Button.Hover solves the same problem the other way round, by
    // taking the camera off its rectangle instead.
    //
    private static Point Cursor()
        => new((int)(Globals.MousePosition.X + Globals.CameraOffset.X),
               (int)(Globals.MousePosition.Y + Globals.CameraOffset.Y));

    private static bool Clicked()
        => Globals.MouseState.LeftButton == ButtonState.Pressed &&
           Globals.LastMouseState.LeftButton == ButtonState.Released;

    //
    // DRAWING, BACK TO FRONT - the same pieces in the same order a Button uses,
    // so the field sits in the stack without looking like a visitor
    //
    public void Draw(SpriteFont font)
    {
        RoundedRect.Glow(_rect, UITheme.Shadow, UITheme.RadiusButton, 10, 0.22f * (1f - _hover * 0.5f));

        if (_hover > 0.01f)
            RoundedRect.Glow(_rect, UITheme.AccentBright, UITheme.RadiusButton, 16, 0.20f * _hover);

        Color top = Color.Lerp(UITheme.ButtonTop, UITheme.ButtonHoverTop, _hover);
        Color bottom = Color.Lerp(UITheme.ButtonBottom, UITheme.ButtonHoverBottom, _hover);

        RoundedRect.FillGradient(_rect, top, bottom, UITheme.RadiusButton);

        RoundedRect.Rect(
            new Rectangle(_rect.X + UITheme.RadiusButton, _rect.Y + 2,
                          _rect.Width - UITheme.RadiusButton * 2, 2),
            Color.White * (0.07f + 0.06f * _hover));

        // The border is what says the field is listening: the accent while it
        // has focus, the ordinary button edge the rest of the time
        RoundedRect.Border(_rect,
            focused
                ? UITheme.AccentBright
                : Color.Lerp(UITheme.ButtonBorder * 0.8f, UITheme.ButtonBorderHover, _hover),
            UITheme.RadiusButton,
            UITheme.BorderWidth);

        DrawLabel(font);
        DrawNumber(font);
        DrawReroll();
    }

    private void DrawLabel(SpriteFont font)
    {
        Vector2 size = font.MeasureString(LABEL);

        UITheme.DrawText(font, LABEL,
            new Vector2(_rect.X + TEXT_PAD, _rect.Y + _rect.Height / 2f - size.Y / 2f),
            Color.Lerp(UITheme.TextPrimary, UITheme.TextOnHover, _hover));
    }

    //
    // THE NUMBER, AND THE CARET AFTER IT
    //
    // Right aligned against the reroll square, which is where a Button puts its
    // value - so SEED reads as one more setting in the stack even though it is
    // the only one that can be typed into.
    //
    private void DrawNumber(SpriteFont font)
    {
        string text = Display;
        Vector2 size = font.MeasureString(text);

        // Never allowed to run back under the word SEED, however many digits
        float room = _entry.Right - (_rect.X + TEXT_PAD + font.MeasureString(LABEL).X + LABEL_GAP);
        float scale = size.X <= 0f || size.X <= room ? 1f : room / size.X;

        float x = _entry.Right - size.X * scale;
        float y = _rect.Y + _rect.Height / 2f - size.Y * scale / 2f;

        UITheme.DrawText(font, text, new Vector2(x, y),
            Color.Lerp(UITheme.AccentBright, UITheme.TextOnHover, _hover * 0.4f), scale);

        if (!focused)
            return;

        // On its own timer, reset by every keystroke, so the caret is always
        // showing at the moment something is actually typed
        if (caretTimer % (BLINK_SECONDS * 2f) > BLINK_SECONDS)
            return;

        RoundedRect.Rect(
            new Rectangle(_entry.Right + 3, (int)y, 2, (int)(size.Y * scale)),
            UITheme.AccentBright);
    }

    //
    // THE REROLL SQUARE
    //
    // The restart icon, drawn straight from its own file and TINTED rather
    // than recoloured - so it dims and lights with the square around it while
    // staying one image on disk.
    //
    // It is turned a quarter of the way round on each press, so a roll that
    // happens to land on a similar number still LOOKS like it did something.
    //
    private void DrawReroll()
    {
        RoundedRect.Fill(_reroll,
            Color.Lerp(UITheme.ButtonBottom, UITheme.AccentBright * 0.5f, _rerollHover),
            UITheme.RadiusChip);

        RoundedRect.Border(_reroll,
            Color.Lerp(UITheme.ButtonBorder, UITheme.ButtonBorderHover, _rerollHover),
            UITheme.RadiusChip,
            1);

        Texture2D icon = Globals.RerollIcon;

        if (icon == null)
            return;

        // Turned about its OWN MIDDLE, sitting on the middle of the square, so
        // the spin is a spin rather than a wobble around some other point
        Vector2 middle = new(icon.Width / 2f, icon.Height / 2f);
        float scale = _reroll.Width * ICON_FILL / icon.Width;

        Globals.SpriteBatch.Draw(
            icon,
            _reroll.Center.ToVector2(),
            null,
            Color.Lerp(UITheme.TextMuted, UITheme.TextOnHover, _rerollHover),
            spin,
            middle,
            scale,
            SpriteEffects.None,
            0f);
    }
}
