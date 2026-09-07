using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// THE JELLYFISH
//
// Drifts around and cannot be shot down. Touching it lights a one second fuse,
// then it GOES OFF - the same blast a flower bomb leaves, in the hot pink it
// was flashing while the fuse burned.
//
// The drift leans towards the player, so one left behind slowly comes up
// through the layers after him. Slowest thing in the game.
//
// It asks the world for a blast the same way a shell and a bomb do, so IT HURTS
// MOBS TOO - a bomb the player can walk into and lead somewhere.
//

public class JellyFish : MobBase
{
    private AnimatedSprite _jellyfish_idle;
    private const int SPEED_MIN = 10, SPEED_MAX = 40;
    private Vector2 target = Vector2.Zero;
    private readonly int targetOffset = 300;

    // How far each new drift target is pulled towards the player. Kept under
    // targetOffset above so the wobble still dominates any one step.
    private const float DRIFT_TOWARDS = 200f;

    // THE BLAST. Smaller and weaker than a flower bomb, but still kills every
    // small mob it reaches. PLAYER_DAMAGE is the number that matters - one
    // second is not long to get clear.
    private const int BLAST_DAMAGE = 200;
    private const int PLAYER_DAMAGE = 100;
    private const float BLAST_RADIUS = 200f;

    private Color bodyColor = Color.White;

    // EXPLODE TIMER
    private bool StartExplodingTimer = false;
    private float explodeTimer = 0f;
    private readonly float explodeSeconds = 1f;

    // How fast it blinks at the start of the fuse and at the end of it. Same
    // telegraph the flower bomb uses, because it is now the same event.
    private const float SLOW_BLINK = 4f;
    private const float FAST_BLINK = 24f;

    // Bullets do not kill it - the fuse does
    protected override AudioId? HitSound => null;

    // AND THEY DO NOT STOP IN IT EITHER. It used to swallow every shot aimed
    // through it, which turned a mob you are not meant to shoot into cover for
    // everything behind it. Shots now go straight through, and because nothing
    // ever hits it, it never flashes red - the only colour it changes to is the
    // pink of a lit fuse, which is the one thing worth reading on it.
    public override bool StopsBullets => false;

    // It dies when its own fuse runs out, not because the player killed it
    public override bool GivesLifeSteal => false;

    protected override Vector2 HitboxScale => new(0.70f, 0.75f);

    public JellyFish()
    {
        SetHP(100);

        // Nothing reads this any more - touching it lights the fuse, it never
        // deals contact damage, and what the blast costs the player is
        // PLAYER_DAMAGE above
        DAMAGE = 0;

        PointsOnKill = 50;
        ON_TOUCH = OnTouch.Explode;
        Invulnerable = true;
    }

    public override void LoadContent(Vector2 playerPos, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        // The explode animation is not loaded at all any more - the blast is
        // drawn by the world, not by the mob that caused it
        _jellyfish_idle = Globals.Jellyfish.CreateAnimatedSprite("jellyfish-animation");

        Sprite = _jellyfish_idle;

        SetSpawn();
        SetTarget(playerPos);

        speed = Globals.R.Next(SPEED_MIN, SPEED_MAX);
        max_speed = speed * 1.2f;

        SetDirectionTimer(0f, 5f);

        RebuildBounds();
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld gameWorld)
    {
        if (DirectionTimerElapsed())
        {
            SetTarget(player.Position);
            ResetDirectionTimer();
        }

        (Sprite.Position, velocity) = MovementUtils.MoveTowardsTarget(Sprite.Position, target, velocity, speed, max_speed);
        velocity = MovementUtils.BounceFromEdge(velocity, Sprite.Position, Sprite.Width);

        SyncBounds();

        if (StartExplodingTimer)
            ExplodeTimer(gameWorld);

        Sprite.Update();
    }

    // Nothing can damage it, so the only thing that overrides its own colour
    // is being frozen
    public override void TakeDamage(int damage, IAudioService audio) {}

    public override void Draw()
    {
        Sprite.Draw(Tinted(bodyColor));
    }

    // Explodes x seconds after being touched
    private void ExplodeTimer(GameWorld gameWorld)
    {
        explodeTimer += Globals.DT;

        if (explodeTimer < explodeSeconds)
        {
            bodyColor = FuseColour();
            return;
        }

        Detonate(gameWorld);
    }

    // Flashes between its own colour and the pink it is about to go off in,
    // faster the closer it gets. The fuse is only a second long, so this is all
    // the warning there is - and it has to be readable while the player is
    // running away from it rather than looking at it.
    private Color FuseColour()
    {
        float left = 1f - explodeTimer / explodeSeconds;
        float rate = MathHelper.Lerp(FAST_BLINK, SLOW_BLINK, left);

        float pulse = 0.5f + 0.5f * MathF.Sin(explodeTimer * rate);

        return Color.Lerp(Color.White, Globals.HotPink, pulse);
    }

    //
    // IT GOES OFF, AND IT IS GONE
    //
    // The blast outlives the mob by a third of a second, so it is killed on the
    // SAME frame - otherwise it sits in the middle of its own explosion still
    // being something you can walk into.
    //
    private void Detonate(GameWorld gameWorld)
    {
        gameWorld.SpawnExplosion(
            Sprite.Position + new Vector2(Sprite.Width, Sprite.Height) * 0.5f,
            BLAST_DAMAGE,
            BLAST_RADIUS,
            Globals.HotPink,
            hurtsPlayer: true,

            // Only the half of the blast that lands on the PLAYER moves with
            // the difficulty. What it does to other mobs is the jellyfish
            // clearing a room for him, and making baby mode worse at that
            // would be the setting working against itself.
            playerDamage: ScaleDamage(PLAYER_DAMAGE));

        HP = 0;
    }

    protected override void SetSpawn()
    {
        Sprite.Position = new(
            Globals.R.Next(0, Globals.VIRTUAL_WIDTH - (int)Sprite.Width),
            LayerTopY + Globals.R.Next(0, Globals.VIRTUAL_HEIGHT - (int)Sprite.Height)
        );
    }

    private void SetTarget(Vector2 playerPos)
    {
        // A step in his direction, then the old random wobble on top of it.
        // The wobble is wider than the step, so it still reads as drifting
        // rather than as hunting - it only wins on average, over minutes.
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

    // Lights the fuse - called from the collision response when the player
    // touches it. Touching it again while it burns changes nothing; the clock
    // was already running.
    public override void Explode()
    {
        StartExplodingTimer = true;
    }
}
