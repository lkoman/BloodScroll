using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// ONE PLACE THAT DECIDES WHAT THE UI LOOKS LIKE
//
// Every colour, corner and gap the menus use. Re-tune the interface here rather
// than in five menu files.
//
// Built out of the game's own palette: the surfaces are Globals.DarkGray pushed
// towards black, and the accent IS Globals.Red.
//

public static class UITheme
{
    // SURFACES - the panels the menus sit on. Each is a Top to Bottom gradient,
    // and the difference between the two is small so it does not read as a
    // button.
    public static readonly Color PanelTop = new(34, 43, 48);
    public static readonly Color PanelBottom = new(19, 24, 28);

    // A hairline round the panel, and a second fainter one inside it. Two lines
    // one pixel apart is what makes an edge look cut rather than drawn.
    public static readonly Color PanelBorder = new(78, 100, 107);
    public static readonly Color PanelInnerLight = Color.White * 0.06f;

    //
    // BUTTONS - three states, all the same shape
    //
    public static readonly Color ButtonTop = new(52, 66, 72);
    public static readonly Color ButtonBottom = new(33, 42, 47);

    // Hovering warms the button towards the accent instead of just lightening
    // it, so the row you are pointing at is the only red thing on the panel
    public static readonly Color ButtonHoverTop = new(112, 45, 45);
    public static readonly Color ButtonHoverBottom = new(66, 26, 28);

    public static readonly Color ButtonBorder = new(84, 106, 113);
    public static readonly Color ButtonBorderHover = new(214, 92, 88);

    // ACCENTS AND TEXT. Accent is the game's own red; AccentBright is the same
    // red lit from inside, for thin marks - the divider, the hover bar, a value.
    public static readonly Color Accent = Globals.Red;
    public static readonly Color AccentBright = new(224, 104, 96);

    public static readonly Color TextPrimary = new(238, 241, 238);
    public static readonly Color TextMuted = new(146, 165, 172);
    public static readonly Color TextOnHover = new(255, 236, 226);

    // The HUD's own colour. Flat white, no tint at all: it is the only text in
    // the game drawn over the playfield, and every point of brightness taken
    // off it is a word lost over a pale platform or a lit explosion.
    public static readonly Color TextBright = Color.White;

    // Under every panel and behind the text on a busy background
    public static readonly Color Shadow = new(0, 0, 0);

    // CORNERS. Big panels round hard, controls half as hard, tags almost to a
    // capsule. Do not add a fifth.
    public const int RadiusPanel = 28;
    public const int RadiusButton = 16;
    public const int RadiusChip = 12;
    public const int RadiusBar = 5;

    public const int BorderWidth = 2;

    //
    // SPACING
    //
    public const int PanelPadX = 64;
    public const int PanelPadTop = 52;
    public const int PanelPadBottom = 52;
    public const int ButtonGap = 20;

    // How long a button takes to finish lighting up, in seconds. Short enough
    // to feel like a response to the mouse, long enough to be a movement.
    public const float HoverSpeed = 0.13f;

    // How far the label slides right while the accent bar grows in beside it
    public const float HoverTextSlide = 10f;

    // TEXT WITH A SHADOW UNDER IT. Nothing in the UI can count on the thing
    // behind it being dark, so every string is drawn twice.
    public static void DrawText(SpriteFont font,
                                string text, Vector2 at, Color colour,
                                float scale = 1f, float shadowAlpha = 0.55f)
    {
        Globals.SpriteBatch.DrawString(font, text, at + new Vector2(2f * scale, 2f * scale),
            Shadow * shadowAlpha, 0f, Vector2.Zero, scale,
            SpriteEffects.None, 0f);

        Globals.SpriteBatch.DrawString(font, text, at, colour, 0f, Vector2.Zero, scale,
            SpriteEffects.None, 0f);
    }

    // The same, centred on a given middle line
    public static void DrawTextCentred(SpriteFont font,
                                       string text, float centreX, float y, Color colour,
                                       float scale = 1f)
    {
        float width = font.MeasureString(text).X * scale;
        DrawText(font, text, new Vector2(centreX - width / 2f, y), colour, scale);
    }

    //
    // TEXT WITH A BLACK EDGE ALL THE WAY ROUND IT
    //
    // DrawText's single corner shadow is enough on a panel, where what is behind
    // the word is known and dark. The HUD sits over the playfield, so it lays
    // the word down eight times in black, one step out in every direction, and
    // puts the real one on top.
    //
    private static readonly Vector2[] OutlineSteps =
    [
        new(-1f, -1f), new(0f, -1f), new(1f, -1f),
        new(-1f,  0f),               new(1f,  0f),
        new(-1f,  1f), new(0f,  1f), new(1f,  1f),
    ];

    // How far out the black sits, before the text's own scale is applied
    private const float OutlineWidth = 2f;

    public static void DrawTextOutlined(SpriteFont font,
                                        string text, Vector2 at, Color colour,
                                        float scale = 1f)
    {
        float step = OutlineWidth * scale;

        foreach (Vector2 direction in OutlineSteps)
            Globals.SpriteBatch.DrawString(font, text, at + direction * step,
                Shadow, 0f, Vector2.Zero, scale,
                SpriteEffects.None, 0f);

        Globals.SpriteBatch.DrawString(font, text, at, colour, 0f, Vector2.Zero, scale,
            SpriteEffects.None, 0f);
    }

    public static void DrawTextCentredOutlined(SpriteFont font,
                                               string text, float centreX, float y, Color colour,
                                               float scale = 1f)
    {
        float width = font.MeasureString(text).X * scale;
        DrawTextOutlined(font, text, new Vector2(centreX - width / 2f, y), colour, scale);
    }
}
