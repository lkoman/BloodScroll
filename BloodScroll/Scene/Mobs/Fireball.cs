using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// Fast homing hazard, used to pad out boss arenas.
//

public class Fireball : MobBase
{
    private const int SPEED_MIN = 150;
    private const int SPEED_MAX = 300;
    public Vector2 target = Vector2.Zero;

    private const int target_offset = 400; // kok mob kiksne ko se zaleti v playerja

    protected override AudioId? HitSound => AudioId.FireHit;

    protected override Vector2 HitboxScale => new(0.70f, 0.70f);

    public Fireball()
    {
        SetHP(200);
        DAMAGE = 50;
        PointsOnKill = 500;
    }

    public override void LoadContent(Vector2 playerPos, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        Sprite = Globals.Enemies.CreateAnimatedSprite("fireball-animation");
        Sprite.Effects = SpriteEffects.FlipHorizontally;

        speed = Globals.R.Next(SPEED_MIN, SPEED_MAX + 100);
        max_speed = speed * 1.2f;

        SetSpawn();
        SetTarget(playerPos);

        RebuildBounds();
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld _)
    {
        if (DirectionTimerElapsed())
        {
            SetTarget(player.Position);
            ResetDirectionTimer();
        }

        (Sprite.Position, velocity) = MovementUtils.MoveTowardsTarget(Sprite.Position, target, velocity, speed, max_speed);
        velocity = MovementUtils.BounceFromEdge(velocity, Sprite.Position, Sprite.Width);

        Sprite.Effects = MovementUtils.FlipSprite(velocity);

        SyncBounds();

        Sprite.Update();
    }

    public void SetTarget(Vector2 playerPosition)
    {
        speed = Globals.R.Next(SPEED_MIN, SPEED_MAX + 100);
        max_speed = speed * 1.2f;

        Vector2 targetOffset = new(Globals.R.Next(-target_offset, target_offset), Globals.R.Next(-target_offset, target_offset));

        target = playerPosition + targetOffset;
    }
}
