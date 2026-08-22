using System;
using System.Dynamic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

public static class UISettings
{
    // Sizes (button, card)
    //
    // The menu button is wider than it used to be because the settings-style
    // buttons carry their value on the right hand side now, and a label and a
    // value sharing 512 pixels left the pair squeezed down to fit.
    //
    // There is no card size any more - the gift card measures itself from
    // whatever it has been asked to announce (see Card).
    public static Vector2 buttonSize = new(700, 100);
    public static Vector2 smallButtonSize = new(256, 92);

    // FONTS
    public static SpriteFont titleFont = Core.Content.Load<SpriteFont>("Fonts/TitleFont");
    public static SpriteFont fontBig = Core.Content.Load<SpriteFont>("Fonts/FontBig");
    public static SpriteFont fontUI = Core.Content.Load<SpriteFont>("Fonts/FontUI");
    public static SpriteFont buttonFont = Core.Content.Load<SpriteFont>("Fonts/ButtonFont");
}