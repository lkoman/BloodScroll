using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

public class Crab : IMob, IDrawableLayer
{
    public int DrawLayer { get; set; } = 20;
    
    public Rectangle Bounds { get; set; }
    public int HP {get; set;}
    private int MAX_HP = 200;
    public int DAMAGE {get; set; } = 300;
    public int PointsOnKill {get; set; } = 50;
    public bool HittingPlayer {get; set;} = false;
    public string ON_TOUCH {get; set; } = "hurt_player";

    public AnimatedSprite _crab, _crab_attack;
    private float MOVEMENT_SPEED = 200f;
    private float ATTACK_MOVEMENT_SPEED = 400f;
    public int SpawnX { get; set; } = 0;
    private Vector2 target = Vector2.Zero;
    private const int target_offset = 50; // kak offset je za target pos od playerja

    // HIT TIMER
    private bool isHit = false;
    private float hitTimer = 0f;
    private const float hitDuration = 0.15f; // seconds
    Color drawColor = Color.White;

    // ATTACK TIMER
    private float attackTimer = 0;
    private float attackSeconds = 5f;

    private const string projectileType = "projectile-fire";
    private const int PROJECTILE_DAMAGE = 25;
    private const float PROJECTILE_SPEED = 800.0f;

    public enum AttackPattern
    {
        IdleMove = 0,
        StopAndShoot = 1
    }
    public AttackPattern currentAttackPattern = AttackPattern.IdleMove;

    public void LoadContent(Vector2 _, int spawnLayer)
    {        
        _crab = new AnimatedSprite();
        _crab = Globals.Crab.CreateAnimatedSprite("crab-animation");

        _crab_attack = new AnimatedSprite();
        _crab_attack = Globals.Crab.CreateAnimatedSprite("crab-attack-animation");

        HP = MAX_HP;

        SetSpawn();

        Bounds = CollisionManager.SetBoundingRectangle(_crab);
    }
    public void Update(Vector2 playerPos, GameWorld gameWorld)
    {
        UpdateHitTimer();
        AttackTimer(playerPos, gameWorld);

        // IDLE MOVE
        if (currentAttackPattern == AttackPattern.IdleMove)
        {
            // Move crab
            (_crab.Position, _crab.Effects, target) =
                MovementUtils.MoveHorizontally(_crab.Position, _crab.Effects, target, MOVEMENT_SPEED);
        }
        
        // STOP AND SHOOT
        else {
            // Move towards target
            _crab.Position = MovementUtils.MOVE(_crab.Position, target, ATTACK_MOVEMENT_SPEED);

            if (_crab.Position.X < target.X + target_offset &&
                _crab.Position.X > target.X - target_offset)
            {
                gameWorld.SpawnMonsterBullet(
                    new Vector2(
                        _crab.Position.X + _crab.Width / 2,
                        _crab.Position.Y + _crab.Height / 2
                    ),
                    new Vector2(_crab.Position.X, 0),
                    projectileType,
                    PROJECTILE_DAMAGE, 
                    PROJECTILE_SPEED,
                    AudioId.PlayerGun
                );

                currentAttackPattern = AttackPattern.IdleMove;
            }
        }
        
        Bounds = CollisionManager.UpdateBoundingRectangle(Bounds, _crab);

        _crab.Update();
    }

    public void AttackTimer(Vector2 playerPos, GameWorld _)
    {
        // TIMER THAT CHANGES TARGET DIRECTION
        attackTimer += Globals.DT;

        if (attackTimer >= attackSeconds)
        {
            if (currentAttackPattern == AttackPattern.IdleMove)
            {
                currentAttackPattern = AttackPattern.StopAndShoot;
                target = new Vector2(playerPos.X, target.Y);
            }
            attackTimer = 0f;
        }
    }

    public void Draw()
    {
        drawColor = isHit ? Globals.Red : Color.White;

        if (currentAttackPattern == AttackPattern.IdleMove)
            _crab.Draw(drawColor);
        else {
            _crab_attack.Position = _crab.Position;
            _crab_attack.Draw(drawColor);
        }
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

        target.Y = Globals.GroundHeight - _crab.Height / 2;
        if (rand == 0)
        {
            _crab.Effects = SpriteEffects.FlipHorizontally;
            target.X = Core.windowWidth;
        }
        else target.X = 0;

        _crab.Position = target;
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

    public void BounceFromFloor() {}
    public void Explode() {}
}