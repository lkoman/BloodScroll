using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace BloodScroll;

public class Card
{
    private Vector2 _pos;
    private Rectangle _rect;
    private Sprite _cardSprite;

    private Button _acceptButton;

    private bool _visible;
    public bool IsVisible
    {
        get => _visible;
        set => _visible = value;
    }

    private string _giftText = "";

    public string GiftText
    {
        get => _giftText;
        set => _giftText = value;
    }

    public void LoadContent(GraphicsDevice device)
    {
        _pos = new Vector2(
            Globals.VIRTUAL_WIDTH / 2 - UISettings.cardSize.X / 2,
            Globals.VIRTUAL_HEIGHT / 2 - UISettings.cardSize.Y / 2
        );

        _rect = new Rectangle(
            (int)_pos.X,
            (int)_pos.Y,
            (int)UISettings.cardSize.X,
            (int)UISettings.cardSize.Y
        );

        _cardSprite = Globals.UI.CreateSprite("gift_card");
        _cardSprite.Position = _pos;

        Vector2 buttonPos = new(
            _pos.X + UISettings.cardSize.X / 2 - UISettings.smallButtonSize.X / 2,
            _pos.Y + UISettings.cardSize.Y - UISettings.smallButtonSize.Y * 1.5f
        );

        _acceptButton = new Button();
        _acceptButton.LoadContent(
            "OK!",
            buttonPos,
            UISettings.smallButtonSize,
            device
        );
    }

    public MouseCursor Update(IAudioService audio)
    {
        if (!_visible)
            return MouseCursor.Arrow;

        bool hovering = _acceptButton.UpdateHoverColor(audio);

        if (_acceptButton.ButtonClicked(audio))
        {
            _visible = false;
            Globals.PAUSE = false;
        }

        return hovering ? MouseCursor.Hand : MouseCursor.Arrow;
    }

    public void Draw()
    {
        if (!_visible)
            return;

        _cardSprite.Draw();

        // Title
        string title = "GIFT FOR YOU";

        Globals.SpriteBatch.DrawString(
            UISettings.buttonFont,
            title,
            new Vector2(
                _pos.X + (_rect.Width - UISettings.buttonFont.MeasureString(title).X) / 2,
                _pos.Y + 80
            ),
            Color.White
        );

        DrawGiftText();

        _acceptButton.Draw(UISettings.buttonFont);
    }

    //
    // THE LIST OF WHAT YOU WON, ONE LINE AT A TIME
    //
    // Drawn line by line rather than as one block, because a gift is allowed to
    // explain itself - "New Gun" is followed by the key that swaps to it - and
    // an explanation is a much longer line than a gift name. Passing the whole
    // thing to DrawString centred it on the WIDEST line, which left the short
    // ones hanging off to the left and let the long one run off the card.
    //
    // Each line is centred on its own, and any line too wide for the card is
    // shrunk until it fits. Nothing has to be counted by hand to add a gift.
    private void DrawGiftText()
    {
        if (string.IsNullOrEmpty(_giftText))
            return;

        float y = _pos.Y + TEXT_TOP;

        foreach (string line in _giftText.Split('\n'))
        {
            if (line.Length == 0)
                continue;

            Vector2 size = UISettings.fontUI.MeasureString(line);

            // Only ever shrinks - a short line is never blown up to fill the card
            float scale = MathHelper.Min(1f, (_rect.Width - TEXT_MARGIN * 2) / size.X);

            Globals.SpriteBatch.DrawString(
                UISettings.fontUI,
                line,
                new Vector2(_pos.X + (_rect.Width - size.X * scale) / 2, y),
                Color.White,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );

            y += size.Y * scale;
        }
    }

    // Where the list starts, and how much clear card is left either side of it
    private const float TEXT_TOP = 380;
    private const float TEXT_MARGIN = 48;
}