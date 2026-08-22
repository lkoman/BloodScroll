using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// SHARED BASE FOR EVERY BAT
//
// All three bats are the same creature. Each one spawns asleep with the whole
// layer, is woken a wave at a time, then picks a point somewhere near the player
// and blunders towards it - rolling a fresh point every time the direction timer
// runs out, which is what makes a swarm of them look like a swarm rather than
// like a line of arrows.
//
// What separates one bat from another is a handful of numbers and whether it
// spits something at the player on the way. A subclass sets those and writes
// nothing else.
//
// THE ORDER OF THE RANDOM CALLS BELOW IS LOAD BEARING. Spawning draws from the
// same seeded stream as level generation, so a bat that rolled its numbers in a
// different order would change every layer above it - see MobManager.SpawnOrder.
//

public abstract class BatBase : MobBase, ISleepingMob
{
    // What a bat spits, for the ones that spit at all.
    //
    // Aim is how far off the player the shot may land. A bat that never missed
    // would be unanswerable at range, so the miss is deliberate and the number
    // is how forgiving that particular bat is.
    //
    // Region is how BIG the shot is and no longer what it looks like - every
    // bullet is a code drawn circle now. Colour is what that circle is painted,
    // and since the two shooting bats fly in the same swarm it is the one thing
    // telling you which of them just fired at you.
    protected record BatShot(
        string Region,
        int Damage,
        float Speed,
        int Aim,
        BulletEffect Effect = BulletEffect.Damage,
        Color? Colour = null);

    // ART - the frames it flies in, and the frames it hangs asleep in
    protected abstract string AwakeRegion { get; }
    protected abstract string SleepingRegion { get; }

    // FLIGHT. The speed is re-rolled in this band every time it turns, so no
    // two bats in a wave ever quite keep pace with each other.
    protected abstract int SpeedMin { get; }
    protected abstract int SpeedMax { get; }

    // How far off the player a new heading may be - how badly it overshoots him
    protected abstract int TargetOffset { get; }

    // Null for a bat that only ever flies at you. It has to stay null rather
    // than become an unused row, because firing a shot is two rolls of the dice
    // and a bat that quietly took them would shift the seeded stream.
    protected virtual BatShot Shot => null;

    // What the SLEEPING sprite is painted. Only the green one is not itself -
    // once awake, every bat goes through MobBase.DrawColour like any other mob.
    protected virtual Color SleepingColour => Color.White;

    // Every bat is drawn from frames that are mostly wingspan
    protected override Vector2 HitboxScale => new(0.55f, 0.60f);

    private AnimatedSprite _batSleeping;
    private bool batSleeping = true;
    private Vector2 target = Vector2.Zero;

    public override void LoadContent(Vector2 playerPos, int spawnLayer)
    {
        SpawnLayer = spawnLayer;

        Sprite = MobArt.Enemy(AwakeRegion);
        _batSleeping = MobArt.Enemy(SleepingRegion);

        RollSpeed();

        SetSpawn();
    }

    // The bounds are built HERE rather than at load, because a bat asleep on the
    // ceiling is not in the fight yet and has nothing worth colliding with
    public void WakeUp(Vector2 playerPos)
    {
        RebuildBounds();
        SetTarget(playerPos);

        batSleeping = false;
    }

    protected override void UpdateBehaviour(IPlayer player, GameWorld gameWorld)
    {
        if (batSleeping)
            return;

        if (DirectionTimerElapsed())
        {
            SetTarget(player.Position);
            Shoot(player.Position, gameWorld);
            ResetDirectionTimer();
        }

        (Sprite.Position, velocity) = MovementUtils.MoveTowardsTarget(Sprite.Position, target, velocity, speed, max_speed);
        velocity = MovementUtils.BounceFromEdge(velocity, Sprite.Position, Sprite.Width);

        Sprite.Effects = MovementUtils.FlipSprite(velocity, Sprite.Effects);

        SyncBounds();

        Sprite.Update();
    }

    public override void Draw()
    {
        if (!batSleeping)
        {
            base.Draw();
            return;
        }

        _batSleeping.Position = Sprite.Position;
        _batSleeping.Draw(SleepingColour);
    }

    // A fresh heading, and a fresh speed to take it at
    private void SetTarget(Vector2 playerPosition)
    {
        RollSpeed();

        target = playerPosition + new Vector2(
            Globals.R.Next(-TargetOffset, TargetOffset),
            Globals.R.Next(-TargetOffset, TargetOffset)
        );
    }

    private void RollSpeed()
    {
        speed = Globals.R.Next(SpeedMin, SpeedMax + 100);
        max_speed = speed * 1.2f;
    }

    // Consumes NOTHING from the random stream for a bat that has no shot
    private void Shoot(Vector2 playerPos, GameWorld gameWorld)
    {
        BatShot shot = Shot;

        if (shot == null)
            return;

        gameWorld.SpawnMonsterBullet(
            Sprite.Position + new Vector2(Sprite.Width / 2, Sprite.Height / 2),
            new Vector2(
                Globals.R.Next(-shot.Aim, shot.Aim) + playerPos.X,
                Globals.R.Next(-shot.Aim, shot.Aim) + playerPos.Y
            ),
            shot.Region,
            ScaleDamage(shot.Damage),
            shot.Speed,
            AudioId.BatSqueak,
            shot.Effect,
            shot.Colour
        );
    }
}
