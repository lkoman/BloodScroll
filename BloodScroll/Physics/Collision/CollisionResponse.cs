using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace BloodScroll;

//
// HANDLES ALL COLLISION RESPONSES
// - player-mob collision (player loses HP)
// - player-platform collision (player stands on platform)
// - mob-player bullet collision (mob loses HP, bullet zgine)
// - player bullet-platform collision (bullet se odbije 1x, nato zgine)
//

public class CollisionResponse
{    
    public Vector2 activePlatform; // Right(x), Left(y) bounds of the platform the player is currently standing on
    
    // Right(x), Left(y) bounds of the platform the player jumped through from below
    // This platform cannot become the active platform until the player is no longer touching it
    public Rectangle[] enterFromBelowPlatform = new Rectangle[2];

    public void HandleAllCollisions(IPlayer player, GameWorld gameWorld, IWeaponsManager weaponsManager, IAudioService audio)
    {
        // FALL OFF CHECK. Asked ONCE - it is about the platform he is standing
        // on, not the one being tested, so it does not belong in the loop.
        CheckPlayerWalkedOff(player);

        // FOR EACH PLATFORM //
        // Platform bounds are plain world space, same as everything else here
        foreach (var p in gameWorld.GetCurrentLayerPlatformList())
        {
            HandlePlayerPlatformCollision(player, p.bounds);
            HandleBulletPlatformCollision(weaponsManager.Bullets, p.bounds, gameWorld);
            //HandleBulletPlatformCollision(layer.mobManager.projectiles, p.bounds);
        }

        // FOR EACH LAYER NEAR THE PLAYER //
        // Mobs more than one screen away cannot reach or be reached
        foreach (var layer in gameWorld.ActiveLayers())
        {
            // FOR EACH MOB IN LAYER
            foreach (var m in layer.MobManager.mobs)
            {
                HandleMobBulletCollision(weaponsManager.Bullets, m, audio, gameWorld);
                HandlePlayerMobCollision(m, player, audio);
                HandleMobBigPlatformCollision(m, gameWorld.bigPlatform);
            }
        }

        // FOR EACH MOB PROJECTILE
        foreach(var mobProjectile in gameWorld.MobProjectiles)
        {
            if (mobProjectile.active == 0)
                continue;
            
            if (player.HurtBox.Intersects(mobProjectile.bulletBounds))
            {
                switch (mobProjectile.Effect)
                {
                    // Webs slow you instead of hurting you
                    case BulletEffect.Slow:
                        player.ApplySlow(Player.WEB_SLOW_FACTOR, Player.WEB_SLOW_SECONDS);
                        break;

                    // A green spider's web takes the controls off you
                    case BulletEffect.Root:
                        player.Root(GreenSpider.ROOT_SECONDS);
                        break;

                    // Hurts now and keeps hurting - see Player.UpdatePoison
                    case BulletEffect.Poison:
                        player.TakeDamage(mobProjectile.DAMAGE, audio);
                        player.ApplyPoison(PoisonDose(), GreenBat.POISON_SECONDS);
                        break;

                    default:
                        player.TakeDamage(mobProjectile.DAMAGE, audio);
                        break;
                }

                mobProjectile.active = 0;

                continue;
            }

            //
            // SHOOTING A SHOT OUT OF THE AIR
            //
            // Both bullets are spent. A SHELL also goes off where they met.
            // The stun freezes nothing here - there is nothing to hold still.
            //
            foreach (var playerBullet in weaponsManager.Bullets)
            {
                if (playerBullet.active == 0 || !playerBullet.bulletBounds.Intersects(mobProjectile.bulletBounds))
                    continue;

                if (playerBullet.Explosive)
                    gameWorld.Detonate(playerBullet);

                playerBullet.active = 0;
                mobProjectile.active = 0;

                // The mob's shot is dead - it must not take a second one down
                break;
            }
        }
    }

    public static void HandleMobBigPlatformCollision(IMob mob, Platform bigPlatform)
    {
        if (mob.CollidesWith(bigPlatform.bounds)) {
            mob.BounceFromFloor();
        }
    }
    
    public static void HandlePlayerMobCollision(IMob mob, IPlayer player, IAudioService audio)
    {
        bool touching = mob.CollidesWith(player.HurtBox);

        if (!mob.HittingPlayer && touching)
        {
            mob.HittingPlayer = true;

            switch (mob.ON_TOUCH)
            {
                case OnTouch.HurtPlayer:
                    player.TakeDamage(mob.DAMAGE, audio);
                    break;

                case OnTouch.Explode:
                    mob.Explode();
                    break;

                case OnTouch.SlowPlayer:
                    player.ApplySlow(Player.WEB_SLOW_FACTOR, Player.WEB_SLOW_SECONDS);
                    break;

                // DAMAGE doubles as the heal amount here
                case OnTouch.HealPlayer:
                    player.Heal(mob.DAMAGE);
                    break;

                // Both - the hit now, the poison over the next twenty seconds
                case OnTouch.PoisonPlayer:
                    player.TakeDamage(mob.DAMAGE, audio);
                    player.ApplyPoison(PoisonDose(), GreenBat.POISON_SECONDS);
                    break;
            }
        }
        else if (!touching)
        {
            mob.HittingPlayer = false;
        }
    }

    // Scaled here rather than in MobBase.ApplyDifficulty, because both a bite
    // and a poison shot arrive holding nothing but a GreenBat constant.
    // Only the green bat poisons, and it is never a boss, so MobDamage is right.
    private static int PoisonDose() =>
        Difficulty.Scale(GreenBat.POISON_DAMAGE, Difficulty.MobDamage);

    // HIS FEET, not the frame around him - the hood is much wider than the
    // tendrils, so the frame would keep him up 30px past the edge
    private void CheckPlayerWalkedOff(IPlayer player)
    {
        if (player.FootingBounds.Right < activePlatform.Y ||
            player.FootingBounds.Left > activePlatform.X)
            player.SetPlayerInAir(true);
    }

    public void HandlePlayerPlatformCollision(IPlayer player, Rectangle plat)
    {
        // Same box as the walked-off test above, or the two disagree on the
        // frame he steps off an edge
        if (!player.FootingBounds.Intersects(plat))
        {
            if (plat == enterFromBelowPlatform[0])
                enterFromBelowPlatform[0] = Rectangle.Empty;
            else if (plat == enterFromBelowPlatform[1])
                enterFromBelowPlatform[1] = Rectangle.Empty;
            
            return;
        }

        // IS PLAYER STANDING ON A PLATFORM
        if (player.IsPlayerStandingOnPlatform(plat) && 
            plat != enterFromBelowPlatform[0] &&
            plat != enterFromBelowPlatform[1])
        {
            player.PlacePlayerOnPlatform(plat.Y);

            // Set the active platform - player is standing here
            activePlatform.X = plat.Right;
            activePlatform.Y = plat.Left;
        }

        if (enterFromBelowPlatform[0] == Rectangle.Empty)
            enterFromBelowPlatform[0] = plat;
        else enterFromBelowPlatform[1] = plat;
    }

    private static void HandleMobBulletCollision(List<Bullet> bullets, IMob mob, IAudioService audio, GameWorld gameWorld)
    {
        // SHOTS FLY THROUGH SOME MOBS. The jellyfish and butterfly are set off
        // by touch, so they must not block shots at what is behind them.
        if (!mob.StopsBullets)
            return;

        foreach (var bullet in bullets)
        {
            // A bullet that already hit something this frame is spent
            if (bullet.active == 0)
                continue;

            if (!mob.CollidesWith(bullet.bulletBounds))
                continue;

            // A SHELL never damages what it touched - it goes off there and the
            // blast is what everything nearby takes, this mob included
            if (bullet.Explosive)
                gameWorld.Detonate(bullet);
            else
            {
                mob.TakeDamage(bullet.DAMAGE, audio);

                if (bullet.StunSeconds > 0f)
                    mob.Stun(bullet.StunSeconds);

                // THE SHOVE GOES THE WAY THE SHOT WAS GOING - the bullet's own
                // direction, so a bounced shot pushes the way it travels NOW
                if (bullet.Knockback > 0f)
                    mob.Knockback(bullet.direction, bullet.Knockback);
            }

            bullet.active = 0;
            break;
        }
    }

    public static void HandleBulletPlatformCollision(List<Bullet> bullets, Rectangle platBounds, GameWorld gameWorld)
    {
        foreach (var bullet in bullets)
        {
            if (bullet.active == 0)
                continue;

            if (!CollisionManager.CircleIntersectsRectangle(bullet.bulletBounds, platBounds))
                continue;

            // A shell does not bounce - it goes off on the ledge
            if (bullet.Explosive)
            {
                gameWorld.Detonate(bullet);
                bullet.active = 0;
                continue;
            }

            if (bullet.bounce)
            {
                bullet.active = 0;
                continue;
            }

            // OFF THE FACE IT ACTUALLY HIT. Reflecting off the real normal
            // turns the shot around whichever face it met; a corner sends it
            // back diagonally.
            (Vector2 normal, float depth) = SurfaceHit(bullet.bulletBounds, platBounds);

            bullet.direction = Vector2.Reflect(bullet.direction, normal);
            bullet.bounce = true;

            // And back out of the ledge, or the next frame finds it still inside
            // and spends the bounce it just used
            bullet.PushOut(normal * (depth + PUSH_OUT_MARGIN));
        }
    }

    // A pixel of daylight, so the shot is clear even after the bounds round
    // back down to whole pixels
    private const float PUSH_OUT_MARGIN = 1f;

    //
    // WHICH WAY IS OUT
    //
    // The nearest point of the rect to the middle of the shot is where it
    // touched, so the line back to the middle is the surface normal. How far
    // short of the radius it falls is the depth.
    //
    // A shot whose middle is INSIDE the rect has no such line, so it leaves by
    // the nearest wall.
    //
    private static (Vector2 normal, float depth) SurfaceHit(Circle shot, Rectangle plat)
    {
        int closestX = Math.Clamp(shot.X, plat.Left, plat.Right);
        int closestY = Math.Clamp(shot.Y, plat.Top, plat.Bottom);

        Vector2 out_ = new(shot.X - closestX, shot.Y - closestY);
        float distance = out_.Length();

        if (distance > 0f)
            return (out_ / distance, shot.Radius - distance);

        // Centre inside: the shortest way to a wall wins
        float left   = shot.X - plat.Left;
        float right  = plat.Right - shot.X;
        float top    = shot.Y - plat.Top;
        float bottom = plat.Bottom - shot.Y;

        float nearest = MathF.Min(MathF.Min(left, right), MathF.Min(top, bottom));

        if (nearest == left)  return (-Vector2.UnitX, left   + shot.Radius);
        if (nearest == right) return ( Vector2.UnitX, right  + shot.Radius);
        if (nearest == top)   return (-Vector2.UnitY, top    + shot.Radius);

        return (Vector2.UnitY, bottom + shot.Radius);
    }
}