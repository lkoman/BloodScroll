using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

public class Centipide : IMob, IDrawableLayer
{
    public int DrawLayer { get; set; } = 20;
    
    public Rectangle Bounds { get; set; }
    public int HP {get; set;} = 200;
    public int DAMAGE {get; set; } = 300;
    public int PointsOnKill {get; set; } = 50;
    public bool HittingPlayer {get; set;} = false;

    public AnimatedSprite _centipide;
    private float MOVEMENT_SPEED = 100f;
    public int SpawnX { get; set; } = 0;
    private Vector2 target = Vector2.Zero;

    // HIT TIMER
    private bool isHit = false;
    private float hitTimer = 0f;
    private const float hitDuration = 0.15f; // seconds
    Color drawColor = Color.White;

    public void LoadContent(Vector2 _, int spawnLayer)
    {        
        _centipide = new AnimatedSprite();
        _centipide = Globals.Enemies.CreateAnimatedSprite("centipide-animation");

        MOVEMENT_SPEED = Globals.R.Next(100, 500); // 100–400

        SetSpawn();

        Bounds = CollisionManager.SetBoundingRectangle(_centipide);
    }
    public void Update(Vector2 _, GameWorld __)
    {
        UpdateHitTimer();

        (_centipide.Position, _centipide.Effects, target) =
            MovementUtils.MoveHorizontally(_centipide.Position, _centipide.Effects, target, MOVEMENT_SPEED);
        
        Bounds = CollisionManager.UpdateBoundingRectangle(Bounds, _centipide);

        _centipide.Update();
    }
    public void Draw()
    {
        drawColor = isHit ? Globals.Red : Color.White;
        _centipide.Draw(drawColor);
    }

    public void TakeDamage(int damage, IAudioService audio)
    {
        HP -= damage;
        audio.PlaySound(AudioId.BatSqueak);
        
        isHit = true;
        hitTimer = hitDuration;
    }

    public void SetSpawn()
    {
        int rand = Globals.R.Next(2);  // 0 or 1

        target.Y = BloodScroll.groundHeight - _centipide.Height / 2;
        if (rand == 0)
        {
            _centipide.Effects = SpriteEffects.FlipHorizontally;
            target.X = Core.windowWidth;
        }
        target.X = 0;

        _centipide.Position = target;
    }

    // Hit for changing color when mob is hit
    private void UpdateHitTimer()
    {
        if (isHit)
        {
            hitTimer -= Globals.DT;
            if (hitTimer <= 0f)
                isHit = false;
        }
    }

    public void BounceFromFloor()
    {
        //velocity.Y *= -3;
    }
}