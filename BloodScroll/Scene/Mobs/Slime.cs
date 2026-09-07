using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace BloodScroll;

//
// Bounces left and right across the screen. It never comes straight at you -
// but every time it hits a wall and turns round it aims the next crossing at
// whatever height the player is at NOW, so it staircases up through the layers
// after him rather than sawing across an empty room for the rest of the run.
//

public class Slime : MobBase
{
    private const int MAX_SPEED = 800, MIN_SPEED = 600;
    private Vector2 target = Vector2.Zero;
    private readonly int targetOffset = 200;

    protected override Vector2 HitboxScale => new(0.80f, 0.75f);

    public Slime()
    {
        SetHP(25);
        DAMAGE = 25;
        PointsOnKill = 50;
    }

    public override void LoadContent(Vector2 _, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        Sprite = Globals.Enemies.CreateAnimatedSprite("slime-animation");

        SetSpawn();

        RebuildBounds();
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld __)
    {
        ChangeDirection(player.Position);

        (Sprite.Position, velocity) =
            MovementUtils.MoveTowardsTarget(Sprite.Position, target, velocity, speed, speed * 1.2f);

        Sprite.Effects = MovementUtils.FlipSprite(velocity);

        SyncBounds();

        Sprite.Update();
    }

    private void ChangeDirection(Vector2 playerPos)
    {
        if (Sprite.Position.X > 0 && Sprite.Position.X < Globals.VIRTUAL_WIDTH - Sprite.Width)
            return;

        velocity.X *= -2; // hitrejÅ¡i odboj
        SetTarget(playerPos);
    }

    // Starts pinned to the left edge and aims straight across
    protected override void SetSpawn()
    {
        Sprite.Position = new(
            0,
            LayerTopY + Globals.R.Next(0, Globals.VIRTUAL_HEIGHT - (int)Sprite.Height)
        );
        target = new(Globals.VIRTUAL_WIDTH, Sprite.Position.Y + Globals.R.Next(-50, 50));

        speed = Globals.R.Next(MIN_SPEED, MAX_SPEED + 100);
    }

    private void SetTarget(Vector2 playerPos)
    {
        if (target.X == 0)
        {
            target.X = Globals.VIRTUAL_WIDTH;
            Sprite.Position = new(1, Sprite.Position.Y); // prevent the slime from getting caught in the wall if velocity is too big
        }
        else
        {
            target.X = 0;
            Sprite.Position = new(Globals.VIRTUAL_WIDTH - 1 - Sprite.Width, Sprite.Position.Y); // prevent the slime from getting caught in the wall if velocity is too big
        }

        // The one line that lets it leave its layer: the crossing is aimed at
        // the player's height rather than at its own, so a slime he ran away
        // from climbs a little closer with every bounce.
        target.Y = playerPos.Y + Globals.R.Next(-targetOffset, targetOffset);

        velocity.Y = 0;
        speed = Globals.R.Next(MIN_SPEED, MAX_SPEED + 100);
    }
}
