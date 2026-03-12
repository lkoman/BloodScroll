using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

public class Fireball : IMob, IDrawableLayer
{
    public int DrawLayer { get; set; } = 20;
    
    public Rectangle Bounds { get; set; }
    public int HP { get; set; } = 200;
    public int DAMAGE { get; set; } = 50;
    public int PointsOnKill { get; set; } = 500;
    public bool HittingPlayer { get; set; } = false;

    private AnimatedSprite _fireball;
    private const int SPEED_MIN = 150;
    private const int SPEED_MAX = 300;
    private float speed, max_speed;
    public Vector2 target = Vector2.Zero;
    private Vector2 velocity;
    private int SpawnLayer;

    private const int target_offset = 400; // kok mob kiksne ko se zaleti v playerja

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

        _fireball = new AnimatedSprite();
        _fireball = Globals.Enemies.CreateAnimatedSprite("fireball-animation");
        _fireball.Effects = SpriteEffects.FlipHorizontally;

        speed = Globals.R.Next(SPEED_MIN, SPEED_MAX + 100);
        max_speed = speed * 1.2f;

        SetSpawn();
        SetTarget(playerPos);

        Bounds = CollisionManager.SetBoundingRectangle(_fireball);
    }

    public void SetSpawn()
    {
        float x = Globals.R.NextSingle() * (Core.windowWidth - _fireball.Width  * 2) + _fireball.Width;
        float y = - Core.windowHeight * SpawnLayer;

        _fireball.Position = new Vector2(x, y);
    }
    public void Update(Vector2 playerPos, GameWorld _)
    {
        DirectionTimer(playerPos);
        UpdateHitTimer();

        (_fireball.Position, velocity) = MovementUtils.MoveTowardsTarget(_fireball.Position, target, velocity, speed, max_speed);
        velocity = MovementUtils.BounceFromEdge(velocity, _fireball.Position, _fireball.Width);

        _fireball.Effects = MovementUtils.FlipSprite(velocity, _fireball.Effects);

        Bounds = CollisionManager.UpdateBoundingRectangle(Bounds, _fireball);

        _fireball.Update();
    }

    public void Draw()
    {
        drawColor = isHit ? Globals.Red : Color.White;
        _fireball.Draw(drawColor);
    }

    public void TakeDamage(int damage, IAudioService audio)
    {
        HP -= damage;
        audio.PlaySound(AudioId.FireHit);
        
        isHit = true;
        hitTimer = hitDuration;
    }

    public void SetTarget(Vector2 playerPosition)
    {
        speed = Globals.R.Next(SPEED_MIN, SPEED_MAX + 100);
        max_speed = speed * 1.2f;
        
        Vector2 targetOffset = new(Globals.R.Next(-target_offset, target_offset),  Globals.R.Next(-target_offset, target_offset));

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
}