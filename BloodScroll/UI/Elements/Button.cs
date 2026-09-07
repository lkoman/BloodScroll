using Microsoft.Xna.Framework;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Input;
using System;
using Microsoft.Xna.Framework.Graphics;

namespace BloodScroll;

//
// A MENU BUTTON
//
// A rounded slab that warms towards red under the mouse, with an accent bar
// growing in at the left edge and the label sliding right to make room.
//
// ALL THE STATE IS ONE NUMBER, _hover, walking between 0 and 1. Every part of
// the drawing reads it - colours, border, glow, bar width, label slide, size -
// so the button can never be half in one look and half in another.
//
// AN OPTIONAL VALUE ON THE RIGHT, for buttons that are a setting you cycle
// (difficulty, sound, fullscreen) rather than a command.
//

public class Button
{
    public Vector2 pos;
    public Rectangle _rect;

    private string _text;
    public string Text => _text;

    // The right hand side, when this button is a setting rather than a command
    private string _value;
    public string Value => _value;

    private int Width;
    private int Height;

    private bool hoveringThisButton = false;

    // 0 = resting, 1 = fully lit. Everything in Draw is a function of this.
    private float _hover = 0f;

    // Held down, drawn a touch smaller so the click has a physical answer
    private bool _pressed = false;

    // How much bigger the button gets at full hover. Small - this is a lift off
    // the panel, not a zoom.
    private const float HOVER_SCALE = 0.02f;

    // The accent bar that grows in at the left edge
    private const int BAR_WIDTH = 6;
    private const int BAR_INSET = 14;
    private const float BAR_HEIGHT_SHARE = 0.46f;

    // Room kept clear at each end for the accent bar, and the least gap ever
    // left between a label and its value so the two never read as one word
    private const int TEXT_PAD = 34;
    private const int VALUE_GAP = 28;

    // A button needs no GraphicsDevice of its own - it is drawn entirely out of
    // RoundedRect, which owns the one white pixel and the baked corners. Where
    // it GOES is not its business either; the menu that owns it calls Place
    // with whatever MenuLayout worked out.
    public Button(string text, Vector2 size)
    {
        _text = text;
        Width = (int)size.X;
        Height = (int)size.Y;
    }

    //
    // DRAWING, FROM THE BACK FORWARDS
    //
    public void Draw(SpriteFont font)
    {
        // The button grows about its own middle, so the stack around it does
        // not shuffle when one of them lights up
        int grow = (int)(Width * HOVER_SCALE * _hover);
        int growY = (int)(Height * HOVER_SCALE * _hover);

        if (_pressed)
        {
            grow -= (int)(Width * 0.012f);
            growY -= (int)(Height * 0.012f);
        }

        Rectangle r = new(
            _rect.X - grow / 2,
            _rect.Y - growY / 2,
            _rect.Width + grow,
            _rect.Height + growY);

        // Sits on the panel when resting, lifts and starts glowing red as it
        // comes up
        RoundedRect.Glow(r, UITheme.Shadow, UITheme.RadiusButton, 10, 0.22f * (1f - _hover * 0.5f));

        if (_hover > 0.01f)
            RoundedRect.Glow(r, UITheme.AccentBright, UITheme.RadiusButton, 16, 0.20f * _hover);

        Color top = Color.Lerp(UITheme.ButtonTop, UITheme.ButtonHoverTop, _hover);
        Color bottom = Color.Lerp(UITheme.ButtonBottom, UITheme.ButtonHoverBottom, _hover);

        RoundedRect.FillGradient(r, top, bottom, UITheme.RadiusButton);

        // Catch light along the top edge, brighter while hovered
        RoundedRect.Rect(
            new Rectangle(r.X + UITheme.RadiusButton, r.Y + 2, r.Width - UITheme.RadiusButton * 2, 2),
            Color.White * (0.07f + 0.06f * _hover));

        RoundedRect.Border(r,
            Color.Lerp(UITheme.ButtonBorder * 0.8f, UITheme.ButtonBorderHover, _hover),
            UITheme.RadiusButton,
            UITheme.BorderWidth);

        DrawAccentBar(r);
        DrawLabel(font, r);
    }

    // The bar at the left edge, which is the whole hover in one mark: zero wide
    // when resting, a bright capsule at full hover
    private void DrawAccentBar(Rectangle r)
    {
        if (_hover <= 0.02f)
            return;

        int height = (int)(r.Height * BAR_HEIGHT_SHARE * _hover);

        if (height < 2)
            return;

        Rectangle bar = new(
            r.X + BAR_INSET,
            r.Y + r.Height / 2 - height / 2,
            BAR_WIDTH,
            height);

        RoundedRect.Glow(bar, UITheme.AccentBright, BAR_WIDTH / 2, 6, 0.30f * _hover);
        RoundedRect.Fill(bar, UITheme.AccentBright * (0.55f + 0.45f * _hover), BAR_WIDTH / 2);
    }

    //
    // THE LABEL, AND THE VALUE BESIDE IT
    //
    // A plain button centres its label. One with a value pins the two to
    // opposite ends instead, or they drift as the value changes width.
    //
    // BOTH ARE SHRUNK TOGETHER until they fit - "DIFFICULTY" and "BABY MODE" are
    // wider than the button they share, and scaling them apart would leave the
    // pair different sizes.
    //
    private void DrawLabel(SpriteFont font, Rectangle r)
    {
        Color textColour = Color.Lerp(UITheme.TextPrimary, UITheme.TextOnHover, _hover);
        Vector2 size = font.MeasureString(_text);

        float slide = UITheme.HoverTextSlide * _hover;

        if (_value == null)
        {
            // Nothing to collide with, so the only limit is the button's edges
            float only = Fits(size.X, r.Width - TEXT_PAD * 2);

            UITheme.DrawText(font, _text,
                new Vector2(r.X + r.Width / 2f - size.X * only / 2f,
                            Centred(r, size.Y, only)),
                textColour, only);
            return;
        }

        Vector2 valueSize = font.MeasureString(_value);

        float scale = Fits(size.X + valueSize.X, r.Width - TEXT_PAD * 2 - VALUE_GAP);
        float y = Centred(r, size.Y, scale);

        UITheme.DrawText(font, _text, new Vector2(r.X + TEXT_PAD + slide, y), textColour, scale);

        UITheme.DrawText(font, _value,
            new Vector2(r.Right - TEXT_PAD - valueSize.X * scale, y),
            Color.Lerp(UITheme.AccentBright, UITheme.TextOnHover, _hover * 0.4f),
            scale);
    }

    // Only ever shrinks - a short label is never blown up to fill the button
    private static float Fits(float wanted, float available)
        => wanted <= 0f ? 1f : MathHelper.Min(1f, available / wanted);

    private static float Centred(Rectangle r, float textHeight, float scale)
        => r.Y + r.Height / 2f - textHeight * scale / 2f;

    // THE HOVER, WALKED ONE FRAME AT A TIME. Sets no colour despite the name -
    // it moves _hover on and Draw works the rest out.
    public bool UpdateHoverColor(IAudioService audioService)
    {
        bool over = Hover();

        // Guarded against a zero speed so the constant can be tuned to taste
        // without the button ever dividing by it
        float step = Globals.DT / MathF.Max(0.0001f, UITheme.HoverSpeed);

        _hover = MathHelper.Clamp(_hover + (over ? step : -step), 0f, 1f);

        _pressed = over && Globals.MouseState.LeftButton == ButtonState.Pressed;

        if (over)
        {
            // The tick only ever fires on the frame the mouse arrives, not
            // every frame it stays
            if (!hoveringThisButton)
            {
                audioService.PlaySound(AudioId.ButtonHover);
                hoveringThisButton = true;
            }

            return true;
        }

        hoveringThisButton = false;
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

    // Null puts the button back to a plain centred label
    public void SetValue(string value)
    {
        _value = value;
    }

    // Where the menu that owns this button has decided it goes
    public void Place(Vector2 at)
    {
        pos = at;
        _rect = new Rectangle((int)at.X, (int)at.Y, Width, Height);
    }
}
