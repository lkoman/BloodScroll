using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

public class Bat : IMob, IDrawableLayer
{
    public int DrawLayer { get; set; } = 20;

    public bool batSleeping = true;
    public Rectangle Bounds { get; set; }
    public int HP { get; set; } = 50;
    public int DAMAGE { get; set; } = 25;
    public int PointsOnKill { get; set; } = 25;
    public bool HittingPlayer { get; set; } = false;
    public string ON_TOUCH {get; set; } = "hurt_player";
    
    private AnimatedSprite _bat;
    private AnimatedSprite _batSleeping;
    private const int SPEED_MIN = 100;
    private const int SPEED_MAX = 200;
    private float speed, max_speed;
    public Vector2 target = Vector2.Zero;
    private Vector2 velocity;
    private int SpawnLayer;

    private const int target_offset = 800; // kok bat kiksne ko se zaleti v playerja

    // DIRECTION TIMER
    private float directionTimer = 1.5f;
    private float directionSeconds = 1.5f;

    // HIT TIMER
    private bool isHit = false;
    private float hitTimer = 0f;
    private const float hitDuration = 0.15f; // seconds
    Color drawColor = Color.White;

    public void LoadContent(Vector2 playerPos, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        _bat = new AnimatedSprite();
        _bat = Globals.Enemies.CreateAnimatedSprite("bat-animation");

        _batSleeping = new AnimatedSprite();
        _batSleeping = Globals.Enemies.CreateAnimatedSprite("bat-sleeping-animation");

        speed = Globals.R.Next(SPEED_MIN, SPEED_MAX + 100);
        max_speed = speed * 1.2f;

        SetSpawn();
    }

    public void WakeUpBat(Vector2 playerPos)
    {
        Bounds = CollisionManager.SetBoundingRectangle(_bat);
        SetTarget(playerPos);

        batSleeping = false;
    }

    public void SetSpawn()
    {
        float x = Globals.R.NextSingle() * (Core.windowWidth - _bat.Width  * 2) + _bat.Width;
        float y = - Core.windowHeight * SpawnLayer;

        _bat.Position = new Vector2(x, y);
    }
    public void Update(Vector2 playerPos, GameWorld _)
    {
        if (batSleeping)
            return;
        
        DirectionTimer(playerPos);
        UpdateHitTimer();

        (_bat.Position, velocity) = MovementUtils.MoveTowardsTarget(_bat.Position, target, velocity, speed, max_speed);
        velocity = MovementUtils.BounceFromEdge(velocity, _bat.Position, _bat.Width);

        _bat.Effects = MovementUtils.FlipSprite(velocity, _bat.Effects);

        Bounds = CollisionManager.UpdateBoundingRectangle(Bounds, _bat);

        _bat.Update();
    }

    public void Draw()
    {
        if (!batSleeping) {
            drawColor = isHit ? Globals.Red : Color.White;
            _bat.Draw(drawColor);
        }
        else {
            _batSleeping.Position = _bat.Position;
            _batSleeping.Draw();
        }
    }

    public void TakeDamage(int damage, IAudioService audio)
    {
        HP -= damage;
        audio.PlaySound(AudioId.BatSqueak);
        
        isHit = true;
        hitTimer = hitDuration;
    }

    public void SetTarget(Vector2 playerPosition)
    {
        speed = Globals.R.Next(SPEED_MIN, SPEED_MAX + 100);
        max_speed = speed * 1.2f;
        
        Vector2 targetOffset = new(
            Globals.R.Next(-target_offset, target_offset),
            Globals.R.Next(-target_offset, target_offset)
        );

        target = playerPosition + targetOffset;
    }

    private void DirectionTimer(Vector2 playerPos)
    {
        // TIMER THAT CHANGES TARGET DIRECTION
        directionTimer += Globals.DT;

        if (directionTimer >= directionSeconds)
        {
            SetTarget(playerPos);
            directionTimer = 0f;

            directionSeconds = 0.5f + (float)Globals.R.NextDouble() * (5f - 0.5f);
        }
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