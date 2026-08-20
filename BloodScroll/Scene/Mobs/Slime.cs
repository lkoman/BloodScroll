using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

public class Slime : IMob, IDrawableLayer
{
    public int DrawLayer { get; set; } = 20;
    
    public Rectangle Bounds { get; set; }
    public int HP {get; set;} = 25;
    public int DAMAGE {get; set; } = 25;
    public int PointsOnKill {get; set; } = 50;
    public bool HittingPlayer {get; set;} = false;
    public string ON_TOUCH {get; set; } = "hurt_player";

    public AnimatedSprite _slime;
    public AnimatedSprite _slime_idle;
    public AnimatedSprite _slime_sqish;
    private float MOVEMENT_SPEED;
    private const int MAX_SPEED = 800, MIN_SPEED = 600;
    public int SpawnX { get; set; } = 0;
    private Vector2 target = Vector2.Zero;
    private int targetOffset = 200;
    private int SpawnLayer;

    private Vector2 velocity;

    // HIT TIMER
    private bool isHit = false;
    private float hitTimer = 0f;
    private const float hitDuration = 0.15f; // seconds
    Color drawColor = Color.White;

    public void LoadContent(Vector2 _, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        _slime_idle = new AnimatedSprite();
        _slime_idle = Globals.Enemies.CreateAnimatedSprite("slime-animation");

        //_slime_sqish = new AnimatedSprite();
        //_slime_sqish = Globals.Enemies.CreateAnimatedSprite("slime-sqish-animation");

        _slime = _slime_idle;

        SetSpawnAndTarget();

        Bounds = CollisionManager.SetBoundingRectangle(_slime);
    }
    public void Update(Vector2 _, GameWorld __)
    {
        ChangeDirection();
        UpdateHitTimer();

        (_slime.Position, velocity) =
            MovementUtils.MoveTowardsTarget(_slime.Position, target, velocity, MOVEMENT_SPEED, MOVEMENT_SPEED * 1.2f);

        _slime.Effects = MovementUtils.FlipSprite(velocity, _slime.Effects);

        Bounds = CollisionManager.UpdateBoundingRectangle(Bounds, _slime);

        _slime.Update();
    }

    private void ChangeDirection()
    {
        if (_slime.Position.X > 0 && _slime.Position.X < Globals.VIRTUAL_WIDTH - _slime.Width)
            return;
        
        velocity.X *= -2; // hitrejši odboj
        SetTarget();
    }
    public void Draw()
    {
        drawColor = isHit ? Globals.Red : Color.White;
        _slime.Draw(drawColor);
    }

    public void TakeDamage(int damage, IAudioService audio)
    {
        HP -= damage;
        audio.PlaySound(AudioId.BatSqueak);
        
        isHit = true;
        hitTimer = hitDuration;
    }

    public void SetSpawnAndTarget()
    {
        _slime.Position = new(
            0,
            -Core.windowHeight * SpawnLayer + Globals.R.Next(0, Globals.VIRTUAL_HEIGHT - (int)_slime.Height)
        );
        target = new(Globals.VIRTUAL_WIDTH, _slime.Position.Y + Globals.R.Next(-50, 50));

        MOVEMENT_SPEED = Globals.R.Next(MIN_SPEED, MAX_SPEED + 100);
    }

    private void SetTarget()
    {
        if (target.X == 0)
        {
            target.X = Globals.VIRTUAL_WIDTH;
            _slime.Position = new(1, _slime.Position.Y); // prevent the slime from getting caught in the wall if velocity is too big
        }
        else
        {
            target.X = 0;
            _slime.Position = new(Globals.VIRTUAL_WIDTH - 1 - _slime.Width, _slime.Position.Y); // prevent the slime from getting caught in the wall if velocity is too big
        }
        
        target.Y = _slime.Position.Y + Globals.R.Next(-targetOffset, targetOffset);
        
        velocity.Y = 0;
        MOVEMENT_SPEED = Globals.R.Next(MIN_SPEED, MAX_SPEED + 100);
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
        velocity.Y *= -3;
    }

    public void Explode() {}
}