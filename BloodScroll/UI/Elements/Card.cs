using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace BloodScroll;

//
// THE GIFT CARD
//
// Drawn in code and MEASURED BEFORE IT IS DRAWN - the panel is exactly as tall
// as the title, the list and the button need. Nothing has to be counted by hand
// to add a gift.
//
// It fades up and settles into place rather than appearing between one frame
// and the next. It pauses the game while it is up.
//

public class Card
{
    private Button _acceptButton;

    private bool _visible;
    public bool IsVisible
    {
        get => _visible;
        set
        {
            // Every showing is a fresh arrival, so the entrance replays even if
            // the last card was dismissed a moment ago
            if (value && !_visible)
                _appear = 0f;

            _visible = value;
        }
    }

    private string _giftText = "";

    public string GiftText
    {
        get => _giftText;
        set => _giftText = value;
    }

    // 0 the instant it is shown, 1 once it has fully arrived
    private float _appear = 0f;

    private const string TITLE = "A GIFT FOR YOU";

    // How wide the card is, and how much clear card is left either side of the
    // list of what you won
    private const int WIDTH = 640;
    private const int TEXT_MARGIN = 48;

    // The parts of the card, top to bottom
    private const int PAD_TOP = 44;
    private const int TITLE_TO_DIVIDER = 18;
    private const int DIVIDER_HEIGHT = 4;
    private const int DIVIDER_WIDTH = 108;
    private const int DIVIDER_TO_LIST = 32;
    private const int LIST_TO_BUTTON = 40;
    private const int PAD_BOTTOM = 40;

    // The little accent capsule sitting above the title, which is the only
    // thing that says "this is a reward" before the words are read
    private const int TAB_WIDTH = 88;
    private const int TAB_HEIGHT = 6;
    private const int TAB_TO_TITLE = 26;

    // The entrance: how long it takes, and how far up the card slides into
    // place over that time
    private const float APPEAR_TIME = 0.18f;
    private const float APPEAR_RISE = 26f;

    public void LoadContent()
    {
        _acceptButton = new Button("OK!", UISettings.smallButtonSize);
    }

    public MouseCursor Update(IAudioService audio)
    {
        if (!_visible)
            return MouseCursor.Arrow;

        _appear = MathHelper.Clamp(_appear + Globals.DT / APPEAR_TIME, 0f, 1f);

        // Placed against the card AS IT IS BEING DRAWN, entrance offset and
        // all, so during the slide the OK button is clickable where it is
        // visible rather than where it is about to end up
        _acceptButton.Place(ButtonAt(Arrived(Measure())));

        bool hovering = _acceptButton.UpdateHoverColor(audio);

        if (_acceptButton.ButtonClicked(audio))
        {
            _visible = false;
            Globals.PAUSE = false;
        }

        return hovering ? MouseCursor.Hand : MouseCursor.Arrow;
    }

    // HOW BIG THE CARD HAS TO BE. Measured in BOTH Update and Draw off the same
    // strings, so the OK button is hit tested exactly where it was painted.
    private Rectangle Measure()
    {
        float height = PAD_TOP
                     + TAB_HEIGHT + TAB_TO_TITLE
                     + TitleHeight
                     + TITLE_TO_DIVIDER + DIVIDER_HEIGHT + DIVIDER_TO_LIST
                     + ListHeight()
                     + LIST_TO_BUTTON
                     + UISettings.smallButtonSize.Y
                     + PAD_BOTTOM;

        return new Rectangle(
            (int)(Globals.VIRTUAL_WIDTH / 2f - WIDTH / 2f),
            (int)(Globals.VIRTUAL_HEIGHT / 2f - height / 2f),
            WIDTH,
            (int)height);
    }

    // The heading is measured in the button font, which is the biggest font on
    // the card - it is the one line most likely to be wider than the card is,
    // so it gets shrunk to fit exactly like the gifts under it
    private static float TitleScale => FitScale(UISettings.buttonFont, TITLE, 1f);

    private static float TitleHeight
        => UISettings.buttonFont.MeasureString(TITLE).Y * TitleScale;

    //
    // WHAT IS ON THE LIST, AND WHICH KIND OF LINE EACH ONE IS
    //
    // RewardService writes two sorts: a gift starts with "- ", anything else is
    // small print under it. Told apart HERE, so measuring and painting can never
    // disagree about how tall a line is.
    //
    private readonly record struct GiftLine(string Text, float Scale, bool IsGift)
    {
        public readonly float Height => UISettings.fontUI.LineSpacing * Scale + (IsGift ? GIFT_GAP : NOTE_GAP);
    }

    // The small print is written smaller as well as dimmer - two gifts in a row
    // should be the thing the eye jumps between
    private const float NOTE_SCALE = 0.78f;
    private const int GIFT_GAP = 10;
    private const int NOTE_GAP = 6;

    private static IEnumerable<GiftLine> Lines(string giftText)
    {
        if (string.IsNullOrEmpty(giftText))
            yield break;

        foreach (string raw in giftText.Split('\n'))
        {
            string line = raw.Trim();

            if (line.Length == 0)
                continue;

            bool gift = line.StartsWith("- ");

            // The dash is what marked it as a gift; it has done its job and
            // the accent colour says the same thing more quietly
            string text = gift ? line[2..] : line;

            yield return new GiftLine(
                text,
                FitScale(UISettings.fontUI, text, gift ? 1f : NOTE_SCALE),
                gift);
        }
    }

    private float ListHeight()
    {
        float total = 0f;

        foreach (GiftLine line in Lines(_giftText))
            total += line.Height;

        return total;
    }

    // THE ENTRANCE. Starts low and rises into place, squared off so it
    // decelerates rather than stopping dead.
    private float Eased => 1f - (1f - _appear) * (1f - _appear);

    private Rectangle Arrived(Rectangle card)
    {
        card.Y += (int)(APPEAR_RISE * (1f - Eased));
        return card;
    }

    private Vector2 ButtonAt(Rectangle card)
        => new(
            Globals.VIRTUAL_WIDTH / 2f - UISettings.smallButtonSize.X / 2f,
            card.Bottom - PAD_BOTTOM - UISettings.smallButtonSize.Y);

    public void Draw()
    {
        if (!_visible)
            return;

        Rectangle card = Arrived(Measure());

        // Fading up on the same curve it is rising on
        float alpha = Eased;

        // A red halo around the whole thing. The card is the only friendly
        // interruption in the game and it is allowed to look like an event.
        RoundedRect.Glow(card, UITheme.Shadow, UITheme.RadiusPanel, 30, 0.34f * alpha);
        RoundedRect.Glow(card, UITheme.Accent, UITheme.RadiusPanel, 22, 0.16f * alpha);

        RoundedRect.FillGradient(card,
            UITheme.PanelTop * alpha,
            UITheme.PanelBottom * alpha,
            UITheme.RadiusPanel);

        RoundedRect.Rect(
            new Rectangle(card.X + UITheme.RadiusPanel, card.Y + 2,
                          card.Width - UITheme.RadiusPanel * 2, 2),
            UITheme.PanelInnerLight * alpha);

        RoundedRect.Border(card, UITheme.PanelBorder * (0.85f * alpha),
            UITheme.RadiusPanel, UITheme.BorderWidth);

        float centreX = card.X + card.Width / 2f;
        float y = card.Y + PAD_TOP;

        // The accent tab
        Rectangle tab = new((int)(centreX - TAB_WIDTH / 2f), (int)y, TAB_WIDTH, TAB_HEIGHT);
        RoundedRect.Glow(tab, UITheme.AccentBright, TAB_HEIGHT / 2, 8, 0.26f * alpha);
        RoundedRect.Fill(tab, UITheme.AccentBright * alpha, TAB_HEIGHT / 2);

        y += TAB_HEIGHT + TAB_TO_TITLE;

        UITheme.DrawTextCentred(UISettings.buttonFont, TITLE, centreX, y,
            UITheme.TextPrimary * alpha, TitleScale);

        y += TitleHeight + TITLE_TO_DIVIDER;

        Rectangle divider = new((int)(centreX - DIVIDER_WIDTH / 2f), (int)y,
                                DIVIDER_WIDTH, DIVIDER_HEIGHT);
        RoundedRect.Fill(divider, UITheme.Accent * (0.8f * alpha), 2);

        y += DIVIDER_HEIGHT + DIVIDER_TO_LIST;

        DrawGiftText(centreX, y, alpha);

        _acceptButton.Draw(UISettings.buttonFont);
    }

    //
    // THE LIST OF WHAT YOU WON, ONE LINE AT A TIME
    //
    // Line by line, not as one block - DrawString centres a block on its WIDEST
    // line, which leaves the short ones hanging off to the left.
    //
    // Each line is centred on its own and shrunk if it is too wide for the card.
    //
    private void DrawGiftText(float centreX, float y, float alpha)
    {
        foreach (GiftLine line in Lines(_giftText))
        {
            UITheme.DrawTextCentred(
                UISettings.fontUI,
                line.Text,
                centreX,
                y,
                (line.IsGift ? UITheme.AccentBright : UITheme.TextMuted) * alpha,
                line.Scale);

            y += line.Height;
        }
    }

    // Only ever shrinks - a short line is never blown up to fill the card
    private static float FitScale(SpriteFont font, string line, float wanted)
        => MathHelper.Min(wanted, (WIDTH - TEXT_MARGIN * 2) / font.MeasureString(line).X);
}
