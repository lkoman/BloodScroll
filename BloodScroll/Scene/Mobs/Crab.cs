using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// Walks along the ground of its layer. Every few seconds it stops
// above the player and fires a shot straight up.
//

public class Crab : MobBase
{
    private const int MAX_HP = 200;

    private AnimatedSprite _crab_attack;
    private readonly float MOVEMENT_SPEED = 200f;
    private readonly float ATTACK_MOVEMENT_SPEED = 400f;
    private Vector2 target = Vector2.Zero;
    private const int target_offset = 50; // kak offset je za target pos od playerja

    // ATTACK TIMER
    private float attackTimer = 0;
    private readonly float attackSeconds = 5f;

    // The region is how BIG the shot is - it is drawn as a plain red circle and
    // not from the atlas at all. Red because the crab is the only thing in the
    // game that fires STRAIGHT UP the shaft you are climbing: a red circle
    // rising past you means look down, and nothing else in the game is red.
    private const string projectileType = "projectile-fire";
    private static readonly Color projectileColour = Globals.CrabRed;
    private const int PROJECTILE_DAMAGE = 25;
    private const float PROJECTILE_SPEED = 800.0f;

    public enum AttackPattern
    {
        IdleMove = 0,
        StopAndShoot = 1
    }
    public AttackPattern currentAttackPattern = AttackPattern.IdleMove;

    protected override Vector2 HitboxScale => new(0.70f, 0.85f);

    // IT WALKS, IT DOES NOT FLY. Every shove it takes - the rifle's, the
    // sword's - is flattened to left or right, so a hit from above pushes it
    // along the ground instead of lifting it off it. See MobBase.HorizontalOnly.
    protected override bool HorizontalOnly => true;

    public Crab()
    {
        SetHP(MAX_HP);
        DAMAGE = 300;
        PointsOnKill = 50;
    }

    public override void LoadContent(Vector2 _, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        Sprite = Globals.Crab.CreateAnimatedSprite("crab-animation");
        _crab_attack = Globals.Crab.CreateAnimatedSprite("crab-attack-animation");

        SetSpawn();

        RebuildBounds();
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld gameWorld)
    {
        AttackTimer(player.Position);

        // IDLE MOVE
        if (currentAttackPattern == AttackPattern.IdleMove)
        {
            (Sprite.Position, Sprite.Effects, target) =
                MovementUtils.MoveHorizontally(Sprite.Position, Sprite.Effects, target, MOVEMENT_SPEED);
        }

        // STOP AND SHOOT
        else
        {
            // Move towards target
            Sprite.Position = MovementUtils.MOVE(Sprite.Position, target, ATTACK_MOVEMENT_SPEED);

            if (Sprite.Position.X < target.X + target_offset &&
                Sprite.Position.X > target.X - target_offset)
            {
                gameWorld.SpawnMonsterBullet(
                    new Vector2(
                        Sprite.Position.X + Sprite.Width / 2,
                        Sprite.Position.Y + Sprite.Height / 2
                    ),
                    new Vector2(Sprite.Position.X, LayerTopY),
                    projectileType,
                    ScaleDamage(PROJECTILE_DAMAGE),
                    PROJECTILE_SPEED,
                    AudioId.PlayerGun,
                    tint: projectileColour
                );

                currentAttackPattern = AttackPattern.IdleMove;
            }
        }

        SyncBounds();

        Sprite.Update();
    }

    private void AttackTimer(Vector2 playerPos)
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

    public override void Draw()
    {
        if (currentAttackPattern == AttackPattern.IdleMove)
        {
            base.Draw();
            return;
        }

        _crab_attack.Position = Sprite.Position;
        _crab_attack.Effects = Sprite.Effects;
        _crab_attack.Draw(isHit ? Globals.Red : Color.White);
    }

    // Walks the ground slab, which only the ground layer has - MobManager keeps
    // crabs off every layer above it, so LayerTopY here is always the bottom one
    protected override void SetSpawn()
    {
        int rand = Globals.R.Next(2);  // 0 or 1

        target.Y = LayerTopY + Globals.GroundHeight - Sprite.Height / 2;
        if (rand == 0)
        {
            Sprite.Effects = SpriteEffects.FlipHorizontally;
            target.X = Core.windowWidth;
        }
        else target.X = 0;

        Sprite.Position = target;
    }

    // Already walking on the floor
    public override void BounceFromFloor() {}
}
