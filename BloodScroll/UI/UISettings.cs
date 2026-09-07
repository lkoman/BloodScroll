using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BloodScroll;

public static class UISettings
{
    // Button sizes. Wide, because the settings buttons carry a label AND a value
    // on one line. There is no card size - the gift card measures itself (Card).
    public static Vector2 buttonSize = new(700, 100);
    public static Vector2 smallButtonSize = new(256, 92);

    // FONTS
    public static SpriteFont titleFont = Core.Content.Load<SpriteFont>("Fonts/TitleFont");
    public static SpriteFont fontBig = Core.Content.Load<SpriteFont>("Fonts/FontBig");
    public static SpriteFont fontUI = Core.Content.Load<SpriteFont>("Fonts/FontUI");
    public static SpriteFont buttonFont = Core.Content.Load<SpriteFont>("Fonts/ButtonFont");
}