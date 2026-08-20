using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

public class Fireboss : IMob, IDrawableLayer
{
    public int DrawLayer { get; set; } = 20;
    
    public Rectangle Bounds { get; set; }
    private readonly int maxHP = 1500;
    public int HP { get; set; } = 1500;
    public int DAMAGE { get; set; } = 100;
    public int PointsOnKill { get; set; } = 1000;
    public bool HittingPlayer { get; set; } = false;
    public string ON_TOUCH {get; set; } = "nothing";

    private const string projectileType = "projectile-fire";
    private const int PROJECTILE_DAMAGE = 50;
    private const float PROJECTILE_SPEED = 800.0f;

    private AnimatedSprite _fireboss;
    private AnimatedSprite _fireboss_idle;
    private AnimatedSprite _fireboss_attack;
    private const int SPEED_MIN_IDLE = 50, SPEED_MIN_ATTACK = 200;
    private const int SPEED_MAX_IDLE = 150, SPEED_MAX_ATTACK = 400;
    private float speed, max_speed;
    public Vector2 target = Vector2.Zero;
    private Vector2 velocity;
    private int SpawnLayer;

    private const int projectile_target_offset = 50; // kok mob kiksne ko strela v playerja
    private const int target_offset = 400; // kok mob kiksne ko se zaleti v playerja

    // Has two attack patterns (0 and 1)
        // ATTACK PATTERN 0: Moves around slow and shoots when he changes direction
        // ATTACK PATTERN 1: Moves around fast and randomly stops and starts shooting fast
    private bool inAttack_ap1 = false; // in attack - attack pattern 1

    public enum FirebossAttackPattern
    {
        IdleMoveAndShoot = 0,
        RandomStopAndFastShoot = 1
    }
    public FirebossAttackPattern attackPattern = FirebossAttackPattern.IdleMoveAndShoot;

    // DIRECTION TIMER
    private float directionTimer = 1.5f;
    private const float timeBetweenDirectionChangeIdle = 1f;
    private const float timeBetweenDirectionChangeAttack = 2f;

    // SHOOTING TIMER
    private float shootingTimer = 0.4f;
    private const float timeBetweenShooting = 0.4f;

    // HIT TIMER
    private bool isHit = false;
    private float hitTimer = 0f;
    private const float hitDuration = 0.15f; // seconds
    Color drawColor = Color.White;


    public void LoadContent(Vector2 playerPos, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        _fireboss_idle = new AnimatedSprite();
        _fireboss_idle = Globals.Enemies.CreateAnimatedSprite("fireboss-animation");

        _fireboss_attack = new AnimatedSprite();
        _fireboss_attack = Globals.Enemies.CreateAnimatedSprite("fireboss-attack-animation");

        // Initial animated sprite for fireboss is IDLE
        _fireboss = _fireboss_idle;
        _fireboss.Effects = SpriteEffects.FlipHorizontally;

        speed = Globals.R.Next(SPEED_MIN_IDLE, SPEED_MAX_IDLE + 100);
        max_speed = speed * 1.2f;

        SetSpawn();
        SetTarget(playerPos);

        Bounds = CollisionManager.SetBoundingRectangle(_fireboss);
    }

    public void SetSpawn()
    {
        float x = Globals.R.NextSingle() * (Core.windowWidth - _fireboss.Width  * 2) + _fireboss.Width;
        float y = - Core.windowHeight * SpawnLayer;

        _fireboss.Position = new Vector2(x, y);
    }
    public void Update(Vector2 playerPos, GameWorld gameWorld)
    {
        DirectionTimer(playerPos, gameWorld);
        ShootingTimer(playerPos, gameWorld);
        UpdateHitTimer();

        // If boss HP falls below 50%, it changes its attack pattern from 0 to 1
        if (HP < maxHP / 2)
        {
            attackPattern = FirebossAttackPattern.RandomStopAndFastShoot;
        }

        // MOVE
        if (!inAttack_ap1)
        {
            (_fireboss.Position, velocity) = MovementUtils.MoveTowardsTarget(_fireboss.Position, target, velocity, speed, max_speed);
            velocity = MovementUtils.BounceFromEdge(velocity, _fireboss.Position, _fireboss.Width);
        }

        // Update effects
        _fireboss.Effects = MovementUtils.FlipSprite(velocity, _fireboss.Effects);

        // Update bounds
        Bounds = CollisionManager.UpdateBoundingRectangle(Bounds, _fireboss);

        // Update animated sprite
        _fireboss.Update();
    }

    public void Draw()
    {
        drawColor = isHit ? Globals.Red : Color.White;
        _fireboss.Draw(drawColor);

        GamePlayUI.DrawBossHP(HP, _fireboss.Position.X + _fireboss.Width / 2, _fireboss.Position.Y - 20);
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
        switch (attackPattern)
        {
            case FirebossAttackPattern.IdleMoveAndShoot:
                speed = Globals.R.Next(SPEED_MIN_IDLE, SPEED_MAX_IDLE + 100);
                max_speed = speed * 1.2f;
                Vector2 targetOffset = new(
                    Globals.R.Next(-target_offset, target_offset),
                    Globals.R.Next(-target_offset, target_offset)
                );
                target = playerPosition + targetOffset;
                break;

            case FirebossAttackPattern.RandomStopAndFastShoot:
                speed = Globals.R.Next(SPEED_MIN_ATTACK, SPEED_MAX_ATTACK + 100);
                max_speed = speed * 1.2f;
                target = new(
                    Globals.R.Next(0 - (int)Globals.CameraOffset.X, Globals.VIRTUAL_WIDTH - (int)Globals.CameraOffset.X),
                    Globals.R.Next(0 - (int)Globals.CameraOffset.Y, Globals.VIRTUAL_HEIGHT - (int)Globals.CameraOffset.Y)
                );
                break;
        }
    }

    private void ShootingTimer(Vector2 playerPos, GameWorld gameWorld)
    {
        shootingTimer += Globals.DT;

        switch (attackPattern)
        {
            case FirebossAttackPattern.RandomStopAndFastShoot when inAttack_ap1 && shootingTimer >= timeBetweenShooting:
                gameWorld.SpawnMonsterBullet(
                    _fireboss.Position + new Vector2(_fireboss.Width/2, _fireboss.Height/2),
                    new Vector2(
                        Globals.R.Next(-projectile_target_offset, projectile_target_offset) + playerPos.X,
                        Globals.R.Next(-projectile_target_offset, projectile_target_offset) + playerPos.Y
                    ),
                    projectileType,
                    PROJECTILE_DAMAGE,
                    PROJECTILE_SPEED,
                    AudioId.PlayerGun
                );
                shootingTimer = 0f;
                break;
        }
    }

    private void DirectionTimer(Vector2 playerPos, GameWorld gameWorld)
    {
        directionTimer += Globals.DT;

        switch (attackPattern)
        {
            case FirebossAttackPattern.IdleMoveAndShoot:
                if (directionTimer >= timeBetweenDirectionChangeIdle)
                {
                    gameWorld.SpawnMonsterBullet(
                        _fireboss.Position + new Vector2(_fireboss.Width/2, _fireboss.Height/2),
                        playerPos,
                        projectileType,
                        PROJECTILE_DAMAGE,
                        PROJECTILE_SPEED,
                        AudioId.PlayerGun
                    );
                    SetTarget(playerPos);
                    directionTimer = 0f;
                }
                break;

            case FirebossAttackPattern.RandomStopAndFastShoot:
                if (directionTimer >= timeBetweenDirectionChangeAttack)
                {
                    if (!inAttack_ap1)
                    {
                        inAttack_ap1 = true;
                        _fireboss_attack.Position = _fireboss.Position;
                        _fireboss = _fireboss_attack;
                        directionTimer = 0.5f;
                    }
                    else
                    {
                        inAttack_ap1 = false;
                        _fireboss_idle.Position = _fireboss.Position;
                        _fireboss = _fireboss_idle;
                        SetTarget(playerPos);
                        directionTimer = 0f;
                    }
                }
                break;
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