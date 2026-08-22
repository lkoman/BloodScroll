using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// WHERE EVERYTHING ON A MENU GOES
//
// Four screens - main, settings, pause, death - are all the same object: a
// title, sometimes a line of small text under it, and a stack of buttons. They
// used to each work their own positions out, which is why the pause screen sat
// at a different height from the main menu and the death screen at a third.
//
// This measures the whole thing once and centres it, so a screen is described
// by WHAT is on it and never by where. Adding a fifth button to a menu moves
// nothing by hand - the block just gets taller and re-centres itself.
//
// It is built fresh whenever the text changes rather than cached, because the
// subtitle on the death screen is different every run and the high score line
// on the main menu changes width the moment you beat it.
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

    //
    // HOW WIDE THE PANEL IS
    //
    // Wide enough for the widest thing standing on it, not just for the
    // buttons. BLOOD SCROLL in the title font is nearly twice the width of a
    // button, and a panel sized only to its buttons had the title hanging out
    // over both edges of it.
    //
    // Capped, though, or the main menu would be a panel almost as wide as the
    // screen. Anything wider than the cap is shrunk to fit instead - which for
    // the title means it is drawn a little smaller, and it is still by far the
    // biggest thing on the screen.
    private const int MAX_INNER_WIDTH = 760;

    public MenuLayout(string title, SpriteFont titleFont,
                      string subtitle, SpriteFont subtitleFont,
                      int buttonCount, Vector2 buttonSize)
    {
        _buttonHeight = buttonSize.Y;

        Vector2 titleSize = titleFont.MeasureString(title);
        float subtitleWidth = subtitle == null ? 0f : subtitleFont.MeasureString(subtitle).X;
        float subtitleHeight = subtitle == null ? 0f : subtitleFont.MeasureString(subtitle).Y;

        // The panel takes the widest of the three, then gives way to the cap
        float inner = MathHelper.Min(
            MAX_INNER_WIDTH,
            MathHelper.Max(buttonSize.X, MathHelper.Max(titleSize.X, subtitleWidth)));

        // Only ever shrinks. A short title is never blown up to fill the panel.
        TitleScale = MathHelper.Min(1f, inner / titleSize.X);

        float titleHeight = titleSize.Y * TitleScale;

        float buttonsHeight = buttonCount * buttonSize.Y
                            + (buttonCount - 1) * UITheme.ButtonGap;

        float belowDivider = subtitle == null
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

        if (subtitle == null)
        {
            SubtitleAt = Vector2.Zero;
            y += DIVIDER_TO_BUTTONS;
        }
        else
        {
            y += DIVIDER_TO_SUBTITLE;
            SubtitleAt = new Vector2(centreX - subtitleFont.MeasureString(subtitle).X / 2f, y);
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
    // THE PANEL ITSELF, AND EVERYTHING THAT IS THE SAME ON ALL FOUR SCREENS
    //
    // Drawn as: a shadow spread under it so it floats, the shaded body, a
    // hairline of light just inside the top edge to catch it, and the border.
    // The accent stripe across the top is what ties the panel to the divider
    // and to whichever button is hovered.
    //
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
    // Drawn through here rather than by each screen, because the scale the
    // panel worked out has to be the scale it is painted at - a screen that
    // reached for DrawString itself would put a full size title on a panel
    // measured for a shrunk one and hang it out over both edges again.
    //
    // lit is for the two red titles, BLOOD SCROLL and YOU DIED: a soft copy of
    // the word underneath its own accent colour, so it reads as lit from behind
    // rather than painted on.
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
    // Centred one line at a time rather than handed to DrawString as a block:
    // a block is centred on its WIDEST line, which leaves a short second line
    // hanging off to the left. The death screen writes two lines of different
    // lengths in different colours, so this takes a colour per line and falls
    // back to the last one it was given for any line beyond that.
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
