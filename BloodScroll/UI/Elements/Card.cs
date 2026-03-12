using Microsoft.Xna.Framework;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace BloodScroll;

public class Card
{
    private Vector2 _pos;
    private Rectangle _rect;
    private Texture2D _texture;

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

    private Color _shade = Globals.DarkRed * 0.9f;

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

        _texture = new Texture2D(device, 1, 1);
        _texture.SetData([Color.White]);

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

    public void Show(string text)
    {
        _giftText = text;
        _visible = true;
        Globals.PAUSE = true;
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

        Globals.SpriteBatch.Draw(_texture, _rect, _shade);

        // Title
        string title = "GIFT FOR YOU";

        Globals.SpriteBatch.DrawString(
            UISettings.buttonFont,
            title,
            new Vector2(
                _pos.X + (_rect.Width - UISettings.buttonFont.MeasureString(title).X) / 2,
                _pos.Y + 40
            ),
            Color.White
        );

        // Gift text
        Globals.SpriteBatch.DrawString(
            UISettings.fontUI,
            _giftText,
            new Vector2(
                _pos.X + (_rect.Width - UISettings.fontUI.MeasureString(_giftText).X) / 2,
                _pos.Y + 140
            ),
            Color.White
        );

        _acceptButton.Draw(UISettings.buttonFont);
    }
}