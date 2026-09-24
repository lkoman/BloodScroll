using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

//
// CONTROLS
//
// Every binding in the game, in two groups. The keyboard ones on top can be
// changed: click a row, press the new key. The ones underneath - the arrows,
// escape and everything on the mouse - are fixed, and drawn muted so they do
// not look like something that can be clicked.
//
// TWO COLUMNS MEETING IN THE MIDDLE. The actions are pushed right up against the
// centre line and the keys hang off the other side of it, so every key sits
// next to what it does however long either of them is.
//
// SIZED ONCE, IN LoadContent. The font never changes, so neither does the
// table. It is shrunk as a whole if it would not fit on the screen - fourteen
// rows under a title is more than the other menus ever have to hold.
//
// EVERY CHANGE IS SAVED THE MOMENT IT IS MADE, like the toggles on SETTINGS.
//

public class ControlsMenu
{
    private const string TITLE = "CONTROLS";

    // One label per GameAction, in the same order
    private static readonly string[] ACTION_NAMES =
    [
        "MOVE LEFT",
        "MOVE RIGHT",
        "JUMP",
        "SPRINT",
        "DASH LEFT",
        "DASH RIGHT",
        "PICK UP / DROP",
        "SWITCH WEAPON",
    ];

    // Keep in step with Bindings, WeaponsManager and Sword - this is only a
    // description of what they read, not where they read it from
    private static readonly (string Key, string Action)[] FIXED =
    [
        ("LEFT / RIGHT ARROW", "MOVE"),
        ("ESC",                "PAUSE"),
        ("LEFT MOUSE",         "SHOOT (HOLD)"),
        ("LEFT MOUSE AGAIN",   "DETONATE SHELL"),
        ("RIGHT MOUSE",        "SWORD"),
        ("MOUSE WHEEL",        "SWITCH WEAPON"),
    ];

    // The line above the table, which is also where the page talks back
    private const string HINT_IDLE = "CLICK A KEY TO CHANGE IT";
    private const string HINT_WAITING = "PRESS A KEY   -   ESC TO CANCEL";
    private const string HINT_REFUSED = "THAT KEY IS RESERVED";
    private const string WAITING_TEXT = "PRESS A KEY...";

    private const float HINT_SCALE = 0.7f;
    private const float HINT_TO_TABLE = 18f;

    // Air between the key column and the action column
    private const float COLUMN_GAP = 48f;

    // A row is a touch taller than its line, so the hover band has a margin
    private const float ROW_HEIGHT = 1.12f;

    // How much of a row's height is left empty between the two groups
    private const float GROUP_GAP = 0.7f;

    // The least air kept between the panel and the top and bottom of the screen
    private const float SCREEN_MARGIN = 30f;

    // The hover band reaches this far past the text on each side
    private const int ROW_PAD_X = 14;

    private const float REFUSED_SECONDS = 1.6f;

    private Button ButtonReset, ButtonBack;

    // Measured once in LoadContent - see the header
    private MenuLayout layout;
    private float scale;
    private float columnWidth;
    private float rowHeight;

    // Which action is waiting for its new key, or -1. Clicking a row is what
    // sets it, pressing a key or escape is what clears it.
    private int waitingFor = -1;

    // The row under the mouse, so the hover tick plays on arrival only
    private int hoveredRow = -1;

    private float refusedTimer;
    private float blink;

    private KeyboardState lastKeys;

    private static SpriteFont Font => UISettings.fontUI;

    public void LoadContent()
    {
        ButtonReset = new Button("RESET", UISettings.smallButtonSize);
        ButtonBack = new Button("BACK", UISettings.smallButtonSize);

        Measure();
    }

    //
    // THE SIZE OF EVERYTHING
    //
    // Both columns are as wide as the widest thing that could ever go in
    // EITHER of them, because they meet in the middle. The prompt shown while
    // waiting for a key is counted too, so the panel never jumps when it shows.
    //
    private void Measure()
    {
        float widest = Font.MeasureString(WAITING_TEXT).X;

        foreach (string name in ACTION_NAMES)
            widest = MathF.Max(widest, Font.MeasureString(name).X);

        foreach (var (key, action) in FIXED)
            widest = MathF.Max(widest, MathF.Max(Font.MeasureString(key).X, Font.MeasureString(action).X));

        float naturalWidth = widest * 2f + COLUMN_GAP;
        float naturalHeight = BodyHeight(1f);

        // How tall the panel is with nothing in the body - whatever is left of
        // the screen after that is what the table may take
        var empty = new MenuLayout(TITLE, UISettings.fontBig, new Vector2(naturalWidth, 0f), 1, UISettings.smallButtonSize);
        float room = Globals.VIRTUAL_HEIGHT - SCREEN_MARGIN * 2f - empty.Panel.Height;

        scale = MathF.Min(1f, MathF.Min(
            room / naturalHeight,
            MenuLayout.MAX_INNER_WIDTH / naturalWidth));

        columnWidth = widest * scale;
        rowHeight = Font.LineSpacing * ROW_HEIGHT * scale;

        layout = new MenuLayout(TITLE, UISettings.fontBig,
            new Vector2(naturalWidth * scale, BodyHeight(scale)),
            1, UISettings.smallButtonSize);
    }

    private static float BodyHeight(float scale)
    {
        int rows = ACTION_NAMES.Length + FIXED.Length;

        return Font.LineSpacing * HINT_SCALE * scale
             + HINT_TO_TABLE * scale
             + (rows + GROUP_GAP) * Font.LineSpacing * ROW_HEIGHT * scale;
    }

    private float TableTop => layout.SubtitleAt.Y + Font.LineSpacing * HINT_SCALE * scale + HINT_TO_TABLE * scale;

    // The clickable band of a rebindable row, in the same space the buttons
    // are hit tested in
    private Rectangle RowRect(int row)
    {
        float centreX = Globals.VIRTUAL_WIDTH / 2f;
        float halfWidth = columnWidth + COLUMN_GAP * scale / 2f + ROW_PAD_X;

        return new Rectangle(
            (int)(centreX - halfWidth),
            (int)(TableTop + row * rowHeight),
            (int)(halfWidth * 2f),
            (int)rowHeight);
    }

    // True on the frame BACK is clicked. SettingsMenu owns the page and puts
    // itself back in front.
    public bool Update(IAudioService audio, out MouseCursor desiredCursor)
    {
        KeyboardState keys = Keyboard.GetState();

        blink += Globals.DT;
        refusedTimer = MathF.Max(0f, refusedTimer - Globals.DT);

        if (waitingFor >= 0)
            ListenForKey(keys, audio);

        PlaceButtons(audio, out bool buttonHovered);

        int row = RowUnderMouse();
        if (row != hoveredRow && row >= 0)
            audio.PlaySound(AudioId.ButtonHover);
        hoveredRow = row;

        desiredCursor = buttonHovered || row >= 0 ? MouseCursor.Hand : MouseCursor.Arrow;

        bool back = false;

        if (ButtonBack.ButtonClicked(audio))
        {
            waitingFor = -1;
            back = true;
        }
        else if (ButtonReset.ButtonClicked(audio))
        {
            waitingFor = -1;

            Bindings.ResetToDefaults();
            SaveManager.Save();
        }
        else if (Clicked())
        {
            // A row starts listening. A click anywhere else stops it - the
            // player has clearly changed their mind.
            if (row >= 0)
            {
                audio.PlaySound(AudioId.ButtonClick);
                waitingFor = row;
                refusedTimer = 0f;
                blink = 0f;
            }
            else
                waitingFor = -1;
        }

        lastKeys = keys;
        return back;
    }

    //
    // THE NEXT KEY PRESSED IS THE NEW BINDING
    //
    // A fresh press only. Anything already held when the row was clicked -
    // shift, from sprinting into the menu - is not what the player chose.
    //
    private void ListenForKey(KeyboardState keys, IAudioService audio)
    {
        if (keys.IsKeyDown(Keys.Escape) && lastKeys.IsKeyUp(Keys.Escape))
        {
            waitingFor = -1;
            return;
        }

        foreach (Keys key in keys.GetPressedKeys())
        {
            if (lastKeys.IsKeyDown(key))
                continue;

            if (Bindings.Set((GameAction)waitingFor, key))
            {
                audio.PlaySound(AudioId.ButtonClick);
                waitingFor = -1;
                SaveManager.Save();
            }
            else
                refusedTimer = REFUSED_SECONDS;

            return;
        }
    }

    // Side by side under the table, RESET on the left
    private void PlaceButtons(IAudioService audio, out bool anyHovered)
    {
        Vector2 size = UISettings.smallButtonSize;
        float left = Globals.VIRTUAL_WIDTH / 2f - size.X - UITheme.ButtonGap / 2f;

        ButtonReset.Place(new Vector2(left, layout.FirstButtonY));
        ButtonBack.Place(new Vector2(left + size.X + UITheme.ButtonGap, layout.FirstButtonY));

        ButtonReset.UpdateHoverColor(audio);
        ButtonBack.UpdateHoverColor(audio);

        anyHovered = ButtonReset.Hover() || ButtonBack.Hover();
    }

    // Only the rebindable rows answer the mouse
    private int RowUnderMouse()
    {
        for (int i = 0; i < ACTION_NAMES.Length; i++)
        {
            Rectangle r = RowRect(i);
            r.Offset(-(int)Globals.CameraOffset.X, -(int)Globals.CameraOffset.Y);

            if (Globals.Cursor.Intersects(r))
                return i;
        }

        return -1;
    }

    private static bool Clicked()
        => Globals.MouseState.LeftButton == ButtonState.Pressed &&
           Globals.LastMouseState.LeftButton == ButtonState.Released;

    public void Draw()
    {
        layout.DrawPanel();

        layout.DrawTitle(UISettings.fontBig, TITLE, UITheme.TextPrimary);

        layout.DrawDivider();

        DrawHint();

        for (int i = 0; i < ACTION_NAMES.Length; i++)
            DrawBindingRow(i);

        // The fixed group starts a little further down, with a hairline in the
        // gap so the two read as separate lists
        float gapTop = TableTop + ACTION_NAMES.Length * rowHeight;
        float fixedTop = gapTop + GROUP_GAP * rowHeight;

        Rectangle rule = RowRect(0);
        RoundedRect.Rect(
            new Rectangle(rule.X + ROW_PAD_X, (int)(gapTop + (fixedTop - gapTop) / 2f), rule.Width - ROW_PAD_X * 2, 1),
            UITheme.PanelBorder * 0.5f);

        for (int i = 0; i < FIXED.Length; i++)
            DrawRow(FIXED[i].Key, FIXED[i].Action, fixedTop + i * rowHeight,
                UITheme.TextMuted, UITheme.TextMuted * 0.8f);

        MenuLayout.DrawButtons(UISettings.buttonFont, ButtonReset, ButtonBack);
    }

    private void DrawHint()
    {
        string hint = HINT_IDLE;
        Color colour = UITheme.TextMuted;

        if (refusedTimer > 0f)
        {
            hint = HINT_REFUSED;
            colour = UITheme.AccentBright;
        }
        else if (waitingFor >= 0)
        {
            hint = HINT_WAITING;
            colour = UITheme.TextPrimary;
        }

        UITheme.DrawTextCentred(Font, hint, Globals.VIRTUAL_WIDTH / 2f, layout.SubtitleAt.Y,
            colour, HINT_SCALE * scale);
    }

    private void DrawBindingRow(int i)
    {
        Rectangle r = RowRect(i);
        bool waiting = waitingFor == i;

        if (waiting)
        {
            RoundedRect.Fill(r, UITheme.ButtonHoverBottom * 0.85f, 6);
            RoundedRect.Border(r, UITheme.ButtonBorderHover, 6, 1);
        }
        else if (hoveredRow == i)
            RoundedRect.Fill(r, UITheme.ButtonHoverBottom * 0.55f, 6);

        string key = Bindings.Name((GameAction)i);
        Color keyColour = UITheme.AccentBright;

        if (waiting)
        {
            key = WAITING_TEXT;

            // A slow pulse, so the row is plainly the one listening
            float pulse = 0.6f + 0.4f * MathF.Abs(MathF.Cos(blink * 3f));
            keyColour = UITheme.TextOnHover * pulse;
        }

        DrawRow(key, ACTION_NAMES[i], r.Y, keyColour, UITheme.TextPrimary);
    }

    // One line of the table at the given row top. A key name too long for its
    // column - there are a few, SCROLL LOCK and the like - is shrunk to fit it
    // rather than running into the panel's edge.
    private void DrawRow(string key, string action, float top, Color keyColour, Color actionColour)
    {
        float centreX = Globals.VIRTUAL_WIDTH / 2f;
        float y = top + (rowHeight - Font.LineSpacing * scale) / 2f;

        float actionWidth = Font.MeasureString(action).X * scale;

        UITheme.DrawText(Font, action,
            new Vector2(centreX - COLUMN_GAP * scale / 2f - actionWidth, y),
            actionColour, scale);

        float keyWidth = Font.MeasureString(key).X * scale;
        float keyScale = keyWidth > columnWidth ? scale * columnWidth / keyWidth : scale;

        UITheme.DrawText(Font, key,
            new Vector2(centreX + COLUMN_GAP * scale / 2f,
                        y + (Font.LineSpacing * (scale - keyScale)) / 2f),
            keyColour, keyScale);
    }
}
