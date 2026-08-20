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
    public static Vector2 buttonSize = new(512, 96);
    public static Vector2 smallButtonSize = new(256, 96);
    public static Vector2 cardSize = new(512, 768);

    // FONTS
    public static SpriteFont titleFont = Core.Content.Load<SpriteFont>("Fonts/TitleFont");
    public static SpriteFont fontBig = Core.Content.Load<SpriteFont>("Fonts/FontBig");
    public static SpriteFont fontUI = Core.Content.Load<SpriteFont>("Fonts/FontUI");
    public static SpriteFont buttonFont = Core.Content.Load<SpriteFont>("Fonts/ButtonFont");
}