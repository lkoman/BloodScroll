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
        // FALL OFF CHECK (did the player walk off the active platform?)
        // Asked ONCE. It is about the platform he is standing on, not about the
        // one being tested, so running it inside the loop below only asked the
        // same question once per platform on the layer.
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
        // Mobs further away than one screen cannot be reached or reach us,
        // so there is no point testing them every frame.
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
                    // Webs stick you to the floor instead of hurting you
                    case BulletEffect.Slow:
                        player.ApplySlow(Player.WEB_SLOW_FACTOR, Player.WEB_SLOW_SECONDS);
                        break;

                    // And a black one takes the controls off you outright
                    case BulletEffect.Root:
                        player.Root(BlackSpider.ROOT_SECONDS);
                        break;

                    // Hurts now, and keeps hurting - see Player.UpdatePoison
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
            // Any bullet of the player's trades itself for the one coming at
            // him: both are gone. What the SHELL adds is that it goes off where
            // they met - a mob shot flying in over a crowd is a free blast, and
            // that is worth aiming for.
            //
            // The stun does not freeze anything here. Its shot is spent the same
            // as any other, because there is nothing in the air to hold still.
            //
            foreach (var playerBullet in weaponsManager.Bullets)
            {
                if (playerBullet.active == 0 || !playerBullet.bulletBounds.Intersects(mobProjectile.bulletBounds))
                    continue;

                if (playerBullet.Explosive)
                    gameWorld.Detonate(playerBullet);

                playerBullet.active = 0;
                mobProjectile.active = 0;

                // The mob's shot is dead - it must not take a second one
                // of the player's down with it
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

                // DAMAGE doubles as the heal amount for friendly mobs
                case OnTouch.HealPlayer:
                    player.Heal(mob.DAMAGE);
                    break;

                // Both: the hit is what you feel now, the poison is what
                // costs you the next ten seconds
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

    // THE POISON IS THE ONE HIT THE BAT DOES NOT DEAL ITSELF.
    //
    // Everything else a mob costs the player is scaled where the mob is (see
    // MobBase.ApplyDifficulty), but a bite and a poison shot both end up here
    // holding nothing but a constant off GreenBat. Only the green bat poisons
    // anything and it is never a boss, so an ordinary mob's multiplier is the
    // right one - the same figure its contact damage already went through.
    private static int PoisonDose() =>
        Difficulty.Scale(GreenBat.POISON_DAMAGE, Difficulty.MobDamage);

    // PLAYER CANNOT WALK OFF BIG PLATFORM
    //
    // HIS FEET, not the frame around him. The hood is a good deal wider than
    // the tendrils under it, so asking the frame this question kept him up in
    // the air for another thirty pixels after his feet had left the ledge -
    // which is exactly what it looked like.
    private void CheckPlayerWalkedOff(IPlayer player)
    {
        if (player.FootingBounds.Right < activePlatform.Y ||
            player.FootingBounds.Left > activePlatform.X)
            player.SetPlayerInAir(true);
    }

    public void HandlePlayerPlatformCollision(IPlayer player, Rectangle plat)
    {
        // Same box as the walked-off test above, or the two would disagree
        // about where he is on the frame he steps off an edge
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
        // SHOTS FLY THROUGH SOME MOBS. The jellyfish and the butterfly are not
        // shot at all - they are set off by walking into them - so they must not
        // stand in front of the things that ARE worth shooting.
        if (!mob.StopsBullets)
            return;

        foreach (var bullet in bullets)
        {
            // A bullet that already hit something this frame is spent
            if (bullet.active == 0)
                continue;

            if (!mob.CollidesWith(bullet.bulletBounds))
                continue;

            // A SHELL never damages what it touched. It goes off there, and the
            // blast is what everything nearby - this mob included - takes.
            if (bullet.Explosive)
                gameWorld.Detonate(bullet);
            else
            {
                mob.TakeDamage(bullet.DAMAGE, audio);

                if (bullet.StunSeconds > 0f)
                    mob.Stun(bullet.StunSeconds);

                // THE SHOVE GOES THE WAY THE SHOT WAS GOING, which is why the
                // bullet's own direction is handed over rather than the line
                // from the player - a shot that bounced off a ledge pushes the
                // way it is travelling now, not the way it was fired.
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

            // A shell hitting the ground is a shell that has landed - it does
            // not bounce off a ledge and carry on, it goes off on it
            if (bullet.Explosive)
            {
                gameWorld.Detonate(bullet);
                bullet.active = 0;
                continue;
            }

            if (!bullet.bounce)
            {
                bullet.direction.Y *= -1;
                bullet.bounce = true;
            }
            else bullet.active = 0;
        }
    }
}