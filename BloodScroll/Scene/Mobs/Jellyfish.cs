using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

public class JellyFish : IMob, IDrawableLayer
{
    public int DrawLayer { get; set; } = 20;
    private int SpawnLayer;
    
    public Rectangle Bounds { get; set; }
    public int HP {get; set;} = 25000;
    public int DAMAGE {get; set; } = 1;
    public int PointsOnKill {get; set; } = 50;
    public bool HittingPlayer {get; set;} = false;
    public string ON_TOUCH {get; set; } = "explode";

    public AnimatedSprite _jellyfish, _jellyfish_idle, _jellyfish_explode;
    private const int SPEED_MIN = 10, SPEED_MAX = 40;
    private float speed, max_speed;
    private Vector2 target = Vector2.Zero;
    private int targetOffset = 300;

    private Vector2 velocity;
    Color drawColor = Color.White;

    // HIT TIMER
    private bool isHit = false;
    private float hitTimer = 0f;
    private const float hitDuration = 0.15f; // seconds

    // DIRECTION TIMER
    private float directionTimer = 0f;
    private float directionSeconds = 5f;

    // EXPLODE TIMER
    private bool EXPLODING = false;
    private bool StartExplodingTimer = false;
    private float explodeTimer = 0f;
    private float explodeSeconds = 1f;

    public void LoadContent(Vector2 _, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        _jellyfish_idle = new AnimatedSprite();
        _jellyfish_idle = Globals.Jellyfish.CreateAnimatedSprite("jellyfish-animation");

        _jellyfish_explode = new AnimatedSprite();
        _jellyfish_explode = Globals.Jellyfish.CreateAnimatedSprite("jellyfish-explode-animation");

        _jellyfish = _jellyfish_idle;

        SetSpawn();
        SetTarget();

        speed = Globals.R.Next(SPEED_MIN, SPEED_MAX);
        max_speed = speed * 1.2f;

        Bounds = CollisionManager.SetBoundingRectangle(_jellyfish);
    }
    public void Update(Vector2 _, GameWorld __)
    {
        //Console.WriteLine(HP);
        DirectionTimer();
        UpdateHitTimer();

        (_jellyfish.Position, velocity) = MovementUtils.MoveTowardsTarget(_jellyfish.Position, target, velocity, speed, max_speed);
        velocity = MovementUtils.BounceFromEdge(velocity, _jellyfish.Position, _jellyfish.Width);

        Bounds = CollisionManager.UpdateBoundingRectangle(Bounds, _jellyfish);

        if (StartExplodingTimer)
            ExplodeTimer();
        
        if (EXPLODING)
            IsExploding();
        else
        {
            _jellyfish_explode.Position = _jellyfish.Position;
            _jellyfish = _jellyfish_idle;
        }

        _jellyfish.Update();
    }

    private void IsExploding()
    {
        _jellyfish_explode.Position = _jellyfish.Position;
        _jellyfish = _jellyfish_explode;

        ON_TOUCH = "hurt_player";
        HittingPlayer = false;
    
        if (_jellyfish.CurrentFrame == _jellyfish.FramesCount - 1)
            HP = 0;
    }

    public void Draw()
    {
        _jellyfish.Draw(drawColor);
    }

    // Explodes x seconds after being touched
    private void ExplodeTimer()
    {
        // TIMER THAT CHANGES TARGET DIRECTION
        explodeTimer += Globals.DT;

        if (explodeTimer >= explodeSeconds)
        {
            EXPLODING = true;
            drawColor = Color.White;
        }
    }

    private void DirectionTimer()
    {
        // TIMER THAT CHANGES TARGET DIRECTION
        directionTimer += Globals.DT;

        if (directionTimer >= directionSeconds)
        {
            SetTarget();
            directionTimer = 0f;

            directionSeconds = 0.5f + (float)Globals.R.NextDouble() * (5f - 0.5f);
        }
    }

    public void TakeDamage(int damage, IAudioService audio)
    {
        HP -= damage;
        //audio.PlaySound(AudioId.BatSqueak);
        
        isHit = true;
        hitTimer = hitDuration;
    }

    public void SetSpawn()
    {
        _jellyfish.Position = new(
            Globals.R.Next(0, Globals.VIRTUAL_WIDTH - (int)_jellyfish.Width),
            -Core.windowHeight * SpawnLayer + Globals.R.Next(0, Globals.VIRTUAL_HEIGHT - (int)_jellyfish.Height)
        );
    }

    private void SetTarget()
    {
        target = new(
            _jellyfish.Position.X + Globals.R.Next(-targetOffset, targetOffset), 
            _jellyfish.Position.Y + Globals.R.Next(-targetOffset, targetOffset)
        );
        speed = Globals.R.Next(SPEED_MIN, SPEED_MAX);
        max_speed = speed * 1.2f;
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

    public void Explode()
    {
        StartExplodingTimer = true;
        drawColor = Globals.HotPink;
    }
}