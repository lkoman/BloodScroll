using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace BloodScroll;

//
// BOSS - SHADOW TWIN
//
// The player, drawn in near black, on the far side of the screen.
//
// Phase 1: it mirrors your horizontal position, so every step you take moves
//          it too and you end up fighting your own movement. Standing still
//          keeps it still; running keeps it running.
// Phase 2: it stops copying and just comes for you.
//
// Needs no art of its own - it borrows the player atlas.
//

public class ShadowTwin : MobBase
{
    private enum Phase { Mirroring, Hunting }

    private Phase phase = Phase.Mirroring;

    private const string projectileType = "projectile-purple";
    private const int PROJECTILE_DAMAGE = 40;
    private const float PROJECTILE_SPEED = 900.0f;

    private const float SHOOT_SECONDS_MIRROR = 1.6f;
    private const float SHOOT_SECONDS_HUNT = 0.7f;
    private float shootTimer = 0f;

    private const float MIRROR_FOLLOW_SPEED = 900f;
    private const float HUNT_SPEED = 340f;

    // Switches to hunting below this fraction of its HP
    private const float HUNT_THRESHOLD = 0.4f;

    //
    // HOW DARK A SHADOW CAN ACTUALLY BE
    //
    // It used to be drawn in AlmostBlack, which is very nearly the colour of
    // the backgrounds it flies in front of - the boss was genuinely invisible
    // in the darker layers and the player was fighting a health bar hovering in
    // mid air. A silhouette still has to be a silhouette you can SEE, so this
    // is a cold violet grey: unmistakably a shadow of the player, and clearly
    // separate from anything behind it.
    //
    // Its shots are drawn in it too - the darkest bullet in the game, which is
    // exactly what a shadow should be firing, and only readable at all because
    // every circle is drawn inside a pale rim (see Bullet).
    private static readonly Color SHADOW = new(92, 84, 122);

    // Once the player is dead there is nobody left to mimic. It used to keep
    // hunting and settle onto the body while the death screen was up, which
    // read as a bug rather than as menace - so it simply is not there.
    private bool gone = false;

    protected override bool ShowBossHP => true;
    protected override AudioId? HitSound => AudioId.PlayerHit;

    protected override Vector2 HitboxScale => new(0.55f, 0.95f);

    public ShadowTwin()
    {
        SetHP(1200);
        DAMAGE = 60;
        PointsOnKill = 1500;
    }

    public override void LoadContent(Vector2 playerPos, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        Sprite = MobArt.PlayerLookalike();

        SetSpawn();

        speed = HUNT_SPEED;
        max_speed = HUNT_SPEED * 1.2f;

        RebuildBounds();
    }

    protected override void SetSpawn()
    {
        Sprite.Position = new Vector2(
            Globals.VIRTUAL_WIDTH / 2f,
            LayerTopY + Globals.VIRTUAL_HEIGHT / 2f
        );
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld gameWorld)
    {
        // The player is dead - there is nothing left to be the shadow OF
        if (!Globals.PLAYER_ALIVE)
        {
            gone = true;
            return;
        }

        if (HP < MaxHP * HUNT_THRESHOLD)
            phase = Phase.Hunting;

        if (phase == Phase.Mirroring)
            Mirror(player);
        else
            Hunt(player);

        ShootTimer(player, gameWorld);

        Sprite.Effects = MovementUtils.FlipSprite(velocity, Sprite.Effects);

        SyncBounds();

        Sprite.Update();
    }

    private void Mirror(IPlayer player)
    {
        // Reflected across the middle of the screen, matched in height.
        // Move left and it moves right.
        Vector2 mirrored = new(
            Core.windowWidth - player.Position.X - Sprite.Width,
            player.Position.Y
        );

        Vector2 previous = Sprite.Position;
        Sprite.Position = MovementUtils.MOVE(Sprite.Position, mirrored, MIRROR_FOLLOW_SPEED);
        velocity = Sprite.Position - previous;
    }

    private void Hunt(IPlayer player)
    {
        (Sprite.Position, velocity) = MovementUtils.MoveTowardsTarget(Sprite.Position, player.Position, velocity, speed, max_speed);
        velocity = MovementUtils.BounceFromEdge(velocity, Sprite.Position, Sprite.Width);
    }

    private void ShootTimer(IPlayer player, GameWorld gameWorld)
    {
        shootTimer += Globals.DT;

        float interval = phase == Phase.Mirroring ? SHOOT_SECONDS_MIRROR : SHOOT_SECONDS_HUNT;
        if (shootTimer < interval)
            return;

        shootTimer = 0f;

        gameWorld.SpawnMonsterBullet(
            Sprite.Position + new Vector2(Sprite.Width / 2, Sprite.Height / 2),
            player.Position,
            projectileType,
            PROJECTILE_DAMAGE,
            PROJECTILE_SPEED,
            AudioId.PlayerGun,
            tint: SHADOW
        );
    }

    public override void Draw()
    {
        if (gone)
            return;

        // Your own silhouette
        Sprite.Draw(Tinted(SHADOW));

        GamePlayUI.DrawBossHP(HP, Sprite.Position.X + Sprite.Width / 2, Sprite.Position.Y - 20);
    }
}
