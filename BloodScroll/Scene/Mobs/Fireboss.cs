using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// FIRST BOSS
// Two attack patterns, switched when it drops below half HP:
//   0 - moves around slowly and shoots every time it changes direction
//   1 - moves around fast, randomly stops and unloads
//

public class Fireboss : MobBase
{
    // The region is how BIG the shot is - it is drawn as a plain yellow circle
    // and not from the atlas at all. The brightest, most saturated colour any
    // shot in the game wears, because in the second pattern there are a lot of
    // them in the air at once and every one of them takes half your health.
    private const string projectileType = "projectile-fire";
    private static readonly Color projectileColour = Globals.BossYellow;
    private const int PROJECTILE_DAMAGE = 50;
    private const float PROJECTILE_SPEED = 800.0f;

    private AnimatedSprite _fireboss_idle;
    private AnimatedSprite _fireboss_attack;
    private const int SPEED_MIN_IDLE = 50, SPEED_MIN_ATTACK = 200;
    private const int SPEED_MAX_IDLE = 150, SPEED_MAX_ATTACK = 400;
    public Vector2 target = Vector2.Zero;

    private const int projectile_target_offset = 50; // kok mob kiksne ko strela v playerja
    private const int target_offset = 400; // kok mob kiksne ko se zaleti v playerja

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

    protected override bool ShowBossHP => true;
    protected override AudioId? HitSound => AudioId.FireHit;

    protected override Vector2 HitboxScale => new(0.65f, 0.75f);

    public Fireboss()
    {
        SetHP(1250);
        DAMAGE = 100;
        PointsOnKill = 1000;
        ON_TOUCH = OnTouch.Nothing;
    }

    public override void LoadContent(Vector2 playerPos, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        _fireboss_idle = Globals.Enemies.CreateAnimatedSprite("fireboss-animation");
        _fireboss_attack = Globals.Enemies.CreateAnimatedSprite("fireboss-attack-animation");

        // Initial animated sprite for fireboss is IDLE
        Sprite = _fireboss_idle;
        Sprite.Effects = SpriteEffects.FlipHorizontally;

        speed = Globals.R.Next(SPEED_MIN_IDLE, SPEED_MAX_IDLE + 100);
        max_speed = speed * 1.2f;

        SetSpawn();
        SetTarget(playerPos);

        RebuildBounds();
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld gameWorld)
    {
        DirectionTimer(player.Position, gameWorld);
        ShootingTimer(player.Position, gameWorld);

        // If boss HP falls below 50%, it changes its attack pattern from 0 to 1
        if (HP < MaxHP / 2)
        {
            attackPattern = FirebossAttackPattern.RandomStopAndFastShoot;
        }

        // MOVE
        if (!inAttack_ap1)
        {
            (Sprite.Position, velocity) = MovementUtils.MoveTowardsTarget(Sprite.Position, target, velocity, speed, max_speed);
            velocity = MovementUtils.BounceFromEdge(velocity, Sprite.Position, Sprite.Width);
        }

        Sprite.Effects = MovementUtils.FlipSprite(velocity);

        SyncBounds();

        Sprite.Update();
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
                    Sprite.Position + new Vector2(Sprite.Width / 2, Sprite.Height / 2),
                    new Vector2(
                        Globals.R.Next(-projectile_target_offset, projectile_target_offset) + playerPos.X,
                        Globals.R.Next(-projectile_target_offset, projectile_target_offset) + playerPos.Y
                    ),
                    projectileType,
                    ScaleDamage(PROJECTILE_DAMAGE),
                    PROJECTILE_SPEED,
                    AudioId.PlayerGun,
                    tint: projectileColour
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
                        Sprite.Position + new Vector2(Sprite.Width / 2, Sprite.Height / 2),
                        playerPos,
                        projectileType,
                        PROJECTILE_DAMAGE,
                        PROJECTILE_SPEED,
                        AudioId.PlayerGun,
                        tint: projectileColour
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
                        _fireboss_attack.Position = Sprite.Position;
                        Sprite = _fireboss_attack;
                        directionTimer = 0.5f;
                    }
                    else
                    {
                        inAttack_ap1 = false;
                        _fireboss_idle.Position = Sprite.Position;
                        Sprite = _fireboss_idle;
                        SetTarget(playerPos);
                        directionTimer = 0f;
                    }
                }
                break;
        }
    }
}
