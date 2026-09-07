using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace BloodScroll;

//
// SHARED BASE FOR THE CEILING PATROL
//
// Walks back and forth along the top of its own layer and spits webs down. It
// never comes down, so the only way it touches you is what it drops.
//
// NEITHER WEB DOES ANY DAMAGE. A subclass sets what its web does instead, how
// fast it walks, how often it shoots, and its colour. Nothing else.
//

public abstract class CeilingSpider : MobBase
{
    // Flush against the ceiling. Neither spider hangs any lower, and the
    // movement helper nudges vertically, so the height is re-pinned every frame.
    private const int CEILING_OFFSET = 0;

    // A web is a thing that happens to you, not a thing that hurts you
    private const int WEB_DAMAGE = 0;

    protected abstract float PatrolSpeed { get; }
    protected abstract float ShootSecondsMin { get; }
    protected abstract float ShootSecondsMax { get; }
    protected abstract float WebSpeed { get; }

    // The whole difference between the two spiders in one line
    protected abstract BulletEffect WebEffect { get; }

    private Vector2 target = Vector2.Zero;
    private float shootTimer = 0f;
    private float shootSeconds;

    // Which way it is WALKING, which is not the same as which way it is drawn.
    // This is the helper's own state and has to be handed straight back to it
    // every frame - it only rewrites this when the spider reaches an edge, so
    // flipping the value here would flip the spider on every single frame.
    private SpriteEffects heading = SpriteEffects.None;

    protected override Vector2 HitboxScale => new(0.60f, 0.60f);

    public override void LoadContent(Vector2 _, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        Sprite = MobArt.Spiders(MobArt.Spider);

        SetSpawn();
        RollShootTimer();

        RebuildBounds();
    }

    // Pinned to the ceiling of its own layer
    protected override void SetSpawn()
    {
        Sprite.Position = new Vector2(
            Globals.R.NextSingle() * (Core.windowWidth - Sprite.Width * 2) + Sprite.Width,
            LayerTopY + CEILING_OFFSET
        );

        // Start heading for one side or the other, and start drawn facing the
        // way it is about to walk rather than waiting for the first edge
        bool goingLeft = Globals.R.Next(2) == 0;

        target = new Vector2(goingLeft ? 0 : Core.windowWidth, Sprite.Position.Y);
        heading = goingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld gameWorld)
    {
        (Sprite.Position, heading, target) =
            MovementUtils.MoveHorizontally(Sprite.Position, heading, target, PatrolSpeed);

        // The helper flips for art drawn facing right, and this art is drawn
        // facing left, so what it walks as and what it draws as are opposites.
        // Redraw the spider facing right and this is what you delete.
        Sprite.Effects = heading == SpriteEffects.None
            ? SpriteEffects.FlipHorizontally
            : SpriteEffects.None;

        // Stay glued to the ceiling however the movement helper nudges it
        Sprite.Position = new Vector2(Sprite.Position.X, LayerTopY + CEILING_OFFSET);

        ShootTimer(player, gameWorld);

        SyncBounds();

        Sprite.Update();
    }

    private void ShootTimer(IPlayer player, GameWorld gameWorld)
    {
        shootTimer += Globals.DT;
        if (shootTimer < shootSeconds)
            return;

        shootTimer = 0f;

        gameWorld.SpawnMonsterBullet(
            Sprite.Position + new Vector2(Sprite.Width / 2, Sprite.Height),
            player.Position,
            MobArt.WebProjectile,
            WEB_DAMAGE,
            WebSpeed,
            AudioId.BatSqueak,
            WebEffect
        );

        RollShootTimer();
    }

    private void RollShootTimer()
    {
        shootSeconds = ShootSecondsMin
                       + (float)Globals.R.NextDouble() * (ShootSecondsMax - ShootSecondsMin);
    }

    // Hangs from the ceiling, the floor is not its problem
    public override void BounceFromFloor() {}
}
