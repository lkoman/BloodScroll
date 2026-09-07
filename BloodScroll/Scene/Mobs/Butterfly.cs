using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// THE FRIENDLY ONE - the jellyfish turned inside out
//
// Drifts around harmlessly. WALK INTO IT to light a fuse, then it bursts and
// HEALS whoever is still standing in it. Cannot be shot - touch only, same as
// the jellyfish.
//
// Its drift leans towards the player, so one he climbed past follows him up
// instead of being stranded a layer below.
//
// It throws gold dust (see GoldDust), not a fireball - the one thing in the
// game that helps must not look like the two that blow you up.
//
// IT ONLY EVER HEALS THE PLAYER. It never asks the world for a blast the way
// the jellyfish does, so it can never heal the swarm chasing him.
//

public class Butterfly : MobBase
{
    private AnimatedSprite _idle;

    private const int SPEED_MIN = 40, SPEED_MAX = 90;
    private Vector2 target = Vector2.Zero;
    private readonly int targetOffset = 250;

    // How far each new drift target is pulled towards the player. Under
    // targetOffset, so the wobble still dominates any one step.
    private const float DRIFT_TOWARDS = 200f;

    private const int HEAL_AMOUNT = 60;
    private const float FUSE_SECONDS = 1.5f;

    // HOW CLOSE HE HAS TO BE WHEN IT GOES OFF - he lit the fuse by walking into
    // it, so this asks whether he STAYED.
    //
    // ONE instant check, not a window. The dust scatters further than this on
    // purpose - only the THICK of it heals.
    private const float HEAL_RADIUS = 160f;

    private bool fuseLit = false;
    private float fuseTimer = 0f;

    // Its own art, so it is drawn white with no tint
    private static readonly Color BODY = Color.White;

    private Color bodyColor = BODY;

    // Nothing ever hits it
    protected override AudioId? HitSound => null;

    // A pickup, not an enemy - never blocks the layer from being cleared
    public override bool CountsAsEnemy => false;

    // Shots pass through, so it never eats the bullets meant for what is behind
    public override bool StopsBullets => false;

    // It burns itself out - not a kill, so no life steal
    public override bool GivesLifeSteal => false;

    protected override Vector2 HitboxScale => new(0.65f, 0.65f);

    public Butterfly()
    {
        SetHP(100);

        // Nothing reads this. The heal is handed over in Burst.
        DAMAGE = 0;

        PointsOnKill = 0;       // the heal IS the reward
        ON_TOUCH = OnTouch.Explode; // touching it lights the fuse - see Explode
        Invulnerable = true;
    }

    public override void LoadContent(Vector2 playerPos, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        // No burst animation - the dust is thrown by the world
        _idle = MobArt.Butterflies(MobArt.Butterfly);

        Sprite = _idle;

        SetSpawn();
        SetTarget(playerPos);

        SetDirectionTimer(0f, 3f);

        RebuildBounds();
    }

    protected override void SetSpawn()
    {
        Sprite.Position = new Vector2(
            Globals.R.Next(0, Globals.VIRTUAL_WIDTH - (int)Sprite.Width),
            LayerTopY + Globals.R.Next(0, Globals.VIRTUAL_HEIGHT - (int)Sprite.Height)
        );
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld gameWorld)
    {
        if (DirectionTimerElapsed())
        {
            SetTarget(player.Position);
            ResetDirectionTimer(0.5f, 3f);
        }

        (Sprite.Position, velocity) = MovementUtils.MoveTowardsTarget(Sprite.Position, target, velocity, speed, max_speed);
        velocity = MovementUtils.BounceFromEdge(velocity, Sprite.Position, Sprite.Width);
        SyncBounds();

        if (fuseLit)
            FuseTimer(player, gameWorld);

        Sprite.Update();
    }

    private void FuseTimer(IPlayer player, GameWorld gameWorld)
    {
        fuseTimer += Globals.DT;

        if (fuseTimer < FUSE_SECONDS)
        {
            // Flashes towards HealGreen, faster the closer the burst is
            float t = fuseTimer / FUSE_SECONDS;
            float pulse = 0.5f + 0.5f * MathF.Sin(fuseTimer * MathHelper.TwoPi * (2f + 6f * t));

            bodyColor = Color.Lerp(BODY, Globals.HealGreen, pulse);
            return;
        }

        Burst(player, gameWorld);
    }

    //
    // IT BURSTS, AND IT IS GONE
    //
    // The heal is given HERE and nowhere else, so there is no healing hitbox
    // left lying around for a mob to wander into. The dust goes to the world
    // because the butterfly is dead on this frame and cannot draw itself.
    //
    private void Burst(IPlayer player, GameWorld gameWorld)
    {
        Vector2 centre = Sprite.Position + new Vector2(Sprite.Width, Sprite.Height) * 0.5f;

        // Only if he stayed. Measured the same way a blast is.
        Circle reach = new((int)centre.X, (int)centre.Y, (int)HEAL_RADIUS);

        if (player.HurtBox.Intersects(reach))
            player.Heal(HEAL_AMOUNT);

        gameWorld.SpawnGoldDust(centre);

        HP = 0;
    }

    // Walking into it lights the fuse. Called from the collision response, the
    // same OnTouch.Explode path the jellyfish uses.
    public override void Explode()
    {
        if (fuseLit)
            return;

        fuseLit = true; // the flashing in FuseTimer is the telegraph from here
    }

    // Bullets fly through it and nothing else can touch it
    public override void TakeDamage(int damage, IAudioService audio) {}

    public override void Draw()
    {
        Sprite.Draw(Tinted(bodyColor));
    }

    private void SetTarget(Vector2 playerPos)
    {
        // A step in his direction, then the old random wobble on top of it
        Vector2 towards = playerPos - Sprite.Position;

        if (towards != Vector2.Zero)
            towards.Normalize();

        target = Sprite.Position
               + towards * DRIFT_TOWARDS
               + new Vector2(
                     Globals.R.Next(-targetOffset, targetOffset),
                     Globals.R.Next(-targetOffset, targetOffset)
                 );

        speed = Globals.R.Next(SPEED_MIN, SPEED_MAX);
        max_speed = speed * 1.2f;
    }
}
