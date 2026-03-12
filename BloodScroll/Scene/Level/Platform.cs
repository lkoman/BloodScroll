using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

public class Platform : IDrawableLayer
{
    public int DrawLayer { get; set; } = 10;

    // BIG PLATFORMS
    public Sprite sprite;
    public Rectangle bounds;
    public Vector2 position;
    public float Width, Height;

    public Platform GenerateNewPlatform(string spriteName, Vector2 pos)
    {
        sprite = Globals.World.CreateSprite(spriteName);

        position = pos;
        sprite.Position = pos;

        Width = sprite.Width;
        Height = sprite.Height;

        // Bounding rectangle
        bounds = CollisionManager.SetBoundingRectangle(sprite);

        return this;
    }

    public void SetPlatformEffects(SpriteEffects effects)
    {
        sprite.Effects = effects;
    }

    public void Draw()
    {
        sprite.Draw();
    }
}