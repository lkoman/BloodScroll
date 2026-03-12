using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;

namespace BloodScroll;

public class Button
{
    public Vector2 pos;
    public Rectangle _rect;
    private readonly float transparency = 1f;
    private Color _shade;
    private string _text;
    public string Text => _text;
    private Texture2D _texture;
    private int Width;
    private int Height;
    private readonly int offset_od_prejšnjega_buttona = 16;
    private bool hoveringThisButton = false;

    public void LoadContent(string text, Vector2 size, GraphicsDevice device)
    {
        _shade = Globals.DarkGray * transparency;

        _text = text;
        Width = (int)size.X;
        Height = (int)size.Y;

        // Create 1x1 white texture for rectangle
        _texture = new Texture2D(device, 1, 1);
        _texture.SetData([Color.White]);
    }

    public void LoadContent(string text, Vector2 p, Vector2 size, GraphicsDevice device)
    {
        _shade = Globals.DarkGray * transparency;

        _text = text;
        Width = (int)size.X;
        Height = (int)size.Y;
        pos = p;

        _rect = new Rectangle((int)pos.X, (int)pos.Y, Width, Height);

        // Create 1x1 white texture for rectangle
        _texture = new Texture2D(device, 1, 1);
        _texture.SetData([Color.White]);
    }

    public void Draw(SpriteFont font)
    {
        // Draw rectangle
        Globals.SpriteBatch.Draw(_texture, _rect, _shade);

        // Draw text centered
        Vector2 textSize = font.MeasureString(_text);
        Vector2 textPos = new(
            _rect.X + _rect.Width / 2 - textSize.X / 2,
            _rect.Y + _rect.Height / 2 - textSize.Y / 2
        );

        Globals.SpriteBatch.DrawString(font, _text, textPos, Color.White);
    }

    public bool UpdateHoverColor(IAudioService audioService)
    {
        if (Hover())
        {
            if (!hoveringThisButton)
            {
                audioService.PlaySound(AudioId.ButtonHover);
                hoveringThisButton = true;   
            }

            _shade = Globals.Gray * transparency;
            return true;
        }

        // No hover
        hoveringThisButton = false;
        _shade = Globals.DarkGray * transparency;
        return false;
    }

    public bool ButtonClicked(IAudioService audioService)
    {
        if (!Hover())
            return false;

        if (Click())
        {
            audioService.PlaySound(AudioId.ButtonClick);
            return true;
        }
        return false;

    }

    public bool Hover()
    {
        return Globals.Cursor.Intersects(
                    new Rectangle(_rect.X - (int)Globals.CameraOffset.X, 
                    _rect.Y - (int)Globals.CameraOffset.Y, 
                    _rect.Width, 
                    _rect.Height));
    }

    private static bool Click()
    {
        return Globals.MouseState.LeftButton == ButtonState.Pressed &&
               Globals.LastMouseState.LeftButton == ButtonState.Released;
    }

    public void ChangeText(string newText)
    {
        _text = newText;
    }

    public void SetOrder(int order)
    {
        pos = new Vector2(
            Globals.VIRTUAL_WIDTH / 2 - Width / 2,
            Globals.VIRTUAL_HEIGHT / 2 + Height * order + offset_od_prejšnjega_buttona * order
        );

        _rect = new Rectangle((int)pos.X, (int)pos.Y, Width, Height);
    }
}