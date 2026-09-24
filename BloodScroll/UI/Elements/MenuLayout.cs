using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

//
// WHERE EVERYTHING ON A MENU GOES
//
// Four screens - main, settings, pause, death - are the same object: a title,
// sometimes a line of small text, and a stack of buttons.
//
// Measures the whole block once and centres it, so a screen says WHAT is on it
// and never where. Adding a fifth button moves nothing by hand.
//
// Built fresh whenever the text changes rather than cached - the death screen's
// subtitle differs every run.
//

public readonly struct MenuLayout
{
    public readonly Rectangle Panel;
    public readonly Vector2 TitleAt;
    public readonly float TitleScale;
    public readonly Rectangle Divider;
    public readonly Vector2 SubtitleAt;
    public readonly float FirstButtonY;

    private readonly float _buttonHeight;

    // Gaps between the parts, top to bottom
    private const int TITLE_TO_DIVIDER = 20;
    private const int DIVIDER_HEIGHT = 4;
    private const int DIVIDER_WIDTH = 132;
    private const int DIVIDER_TO_SUBTITLE = 24;
    private const int SUBTITLE_TO_BUTTONS = 38;

    // When there is no subtitle the divider still needs air under it before the
    // first button, and it needs more than it would need before a line of text
    private const int DIVIDER_TO_BUTTONS = 44;

    // HOW WIDE THE PANEL IS. The widest thing on it, not just the buttons -
    // BLOOD SCROLL is nearly twice a button's width. Capped, and anything wider
    // than the cap is shrunk to fit (see TitleScale).
    public const int MAX_INNER_WIDTH = 760;

    public MenuLayout(string title, SpriteFont titleFont,
                      string subtitle, SpriteFont subtitleFont,
                      int buttonCount, Vector2 buttonSize)
        : this(title, titleFont,
               subtitle == null ? null : subtitleFont.MeasureString(subtitle),
               buttonCount, buttonSize)
    {
    }

    //
    // A BLOCK OF A GIVEN SIZE WHERE THE SUBTITLE WOULD GO
    //
    // For a screen that draws something of its own under the divider - the
    // controls table - and only needs the panel to leave room for it. The
    // block's top left is SubtitleAt, same as a subtitle's.
    //
    public MenuLayout(string title, SpriteFont titleFont,
                      Vector2? body,
                      int buttonCount, Vector2 buttonSize)
    {
        _buttonHeight = buttonSize.Y;

        Vector2 titleSize = titleFont.MeasureString(title);
        bool hasBody = body.HasValue;
        float subtitleWidth = hasBody ? body.Value.X : 0f;
        float subtitleHeight = hasBody ? body.Value.Y : 0f;

        // The panel takes the widest of the three, then gives way to the cap
        float inner = MathHelper.Min(
            MAX_INNER_WIDTH,
            MathHelper.Max(buttonSize.X, MathHelper.Max(titleSize.X, subtitleWidth)));

        // Only ever shrinks. A short title is never blown up to fill the panel.
        TitleScale = MathHelper.Min(1f, inner / titleSize.X);

        float titleHeight = titleSize.Y * TitleScale;

        float buttonsHeight = buttonCount * buttonSize.Y
                            + (buttonCount - 1) * UITheme.ButtonGap;

        float belowDivider = !hasBody
            ? DIVIDER_TO_BUTTONS
            : DIVIDER_TO_SUBTITLE + subtitleHeight + SUBTITLE_TO_BUTTONS;

        float height = UITheme.PanelPadTop
                     + titleHeight
                     + TITLE_TO_DIVIDER + DIVIDER_HEIGHT
                     + belowDivider
                     + buttonsHeight
                     + UITheme.PanelPadBottom;

        float width = inner + UITheme.PanelPadX * 2;

        Panel = new Rectangle(
            (int)(Globals.VIRTUAL_WIDTH / 2f - width / 2f),
            (int)(Globals.VIRTUAL_HEIGHT / 2f - height / 2f),
            (int)width,
            (int)height);

        float centreX = Globals.VIRTUAL_WIDTH / 2f;
        float y = Panel.Y + UITheme.PanelPadTop;

        TitleAt = new Vector2(centreX - titleSize.X * TitleScale / 2f, y);
        y += titleHeight + TITLE_TO_DIVIDER;

        Divider = new Rectangle(
            (int)(centreX - DIVIDER_WIDTH / 2f),
            (int)y,
            DIVIDER_WIDTH,
            DIVIDER_HEIGHT);

        y += DIVIDER_HEIGHT;

        if (!hasBody)
        {
            SubtitleAt = Vector2.Zero;
            y += DIVIDER_TO_BUTTONS;
        }
        else
        {
            y += DIVIDER_TO_SUBTITLE;
            SubtitleAt = new Vector2(centreX - subtitleWidth / 2f, y);
            y += subtitleHeight + SUBTITLE_TO_BUTTONS;
        }

        FirstButtonY = y;
    }

    // Top left corner of the nth button in the stack
    public readonly Vector2 ButtonAt(int index, Vector2 buttonSize)
        => new(
            Globals.VIRTUAL_WIDTH / 2f - buttonSize.X / 2f,
            FirstButtonY + index * (_buttonHeight + UITheme.ButtonGap));

    //
    // THE WHOLE STACK, IN THE ORDER IT IS HANDED OVER
    //
    // Places each button, ticks its hover animation, and returns the cursor the
    // row wants - a hand if the mouse is over any of them.
    //
    public readonly MouseCursor PlaceButtons(IAudioService audio, Vector2 buttonSize, params Button[] buttons)
    {
        bool anyHovered = false;

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Place(ButtonAt(i, buttonSize));
            buttons[i].UpdateHoverColor(audio);

            anyHovered |= buttons[i].Hover();
        }

        return anyHovered ? MouseCursor.Hand : MouseCursor.Arrow;
    }

    //
    // ONE BUTTON, IN A SLOT NAMED BY HAND
    //
    // For a stack that is not all buttons. The main menu has a seed field
    // sitting in the middle of its four, and PlaceButtons above numbers what it
    // is given from zero - so passing it the four buttons would close the gap
    // the field is supposed to be standing in.
    //
    public readonly MouseCursor PlaceButton(IAudioService audio, Vector2 buttonSize, int index, Button button)
    {
        button.Place(ButtonAt(index, buttonSize));
        button.UpdateHoverColor(audio);

        return button.Hover() ? MouseCursor.Hand : MouseCursor.Arrow;
    }

    public static void DrawButtons(SpriteFont font, params Button[] buttons)
    {
        foreach (Button button in buttons)
            button.Draw(font);
    }

    // THE PANEL, THE SAME ON ALL FOUR SCREENS. Back to front: a shadow under it,
    // the shaded body, a hairline of light inside the top edge, the border.
    public readonly void DrawPanel()
    {
        RoundedRect.Glow(Panel, UITheme.Shadow, UITheme.RadiusPanel, 26, 0.30f);

        RoundedRect.FillGradient(Panel, UITheme.PanelTop, UITheme.PanelBottom, UITheme.RadiusPanel);

        // The catch light, inset so it sits on the face of the panel rather
        // than on its rim
        RoundedRect.Rect(
            new Rectangle(Panel.X + UITheme.RadiusPanel, Panel.Y + 2,
                          Panel.Width - UITheme.RadiusPanel * 2, 2),
            UITheme.PanelInnerLight);

        RoundedRect.Border(Panel, UITheme.PanelBorder * 0.85f, UITheme.RadiusPanel, UITheme.BorderWidth);
    }

    //
    // THE TITLE, AT WHATEVER SIZE IT ENDED UP FITTING AT
    //
    // Drawn HERE and not by each screen, so the scale the panel worked out is
    // the scale it is painted at.
    //
    // lit is for the two red titles (BLOOD SCROLL, YOU DIED) - a soft copy of
    // the word underneath in the accent colour.
    //
    public readonly void DrawTitle(SpriteFont font, string title, Color colour, bool lit = false)
    {
        if (lit)
            UITheme.DrawText(font, title, TitleAt + new Vector2(0f, 2f),
                UITheme.Accent * 0.35f, TitleScale, 0f);

        UITheme.DrawText(font, title, TitleAt, colour, TitleScale);
    }

    //
    // THE LINE OR TWO UNDER THE DIVIDER
    //
    // Centred one line at a time - a block is centred on its WIDEST line, which
    // leaves a short second line hanging off to the left.
    //
    // Takes a colour per line and reuses the last one for any line beyond that.
    //
    public readonly void DrawSubtitle(SpriteFont font, string subtitle, params Color[] colours)
    {
        if (subtitle == null || colours.Length == 0)
            return;

        float centreX = Globals.VIRTUAL_WIDTH / 2f;
        float y = SubtitleAt.Y;

        string[] lines = subtitle.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            Color colour = colours[i < colours.Length ? i : colours.Length - 1];

            UITheme.DrawTextCentred(font, lines[i], centreX, y, colour);
            y += font.LineSpacing;
        }
    }

    public readonly void DrawDivider()
    {
        // A short bar under the title, in the accent, with its own faint glow -
        // the one piece of pure colour on an otherwise cold panel
        RoundedRect.Glow(Divider, UITheme.AccentBright, 2, 8, 0.22f);
        RoundedRect.Fill(Divider, UITheme.AccentBright, 2);
    }
}
