using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

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
        // FOR EACH PLATFORM //
        foreach (var p in gameWorld.GetCurrentLayerPlatformList())
        {
            Rectangle adjustedPlat = p.bounds;
            adjustedPlat.Y -= (int)(Globals.CurrentLayerIndex * Core.windowHeight);

            HandlePlayerPlatformCollision(player, adjustedPlat);
            HandleBulletPlatformCollision(weaponsManager.Bullets, adjustedPlat);
            //HandleBulletPlatformCollision(layer.mobManager.projectiles, adjustedPlat);
        }

        // FOR EACH LAYER //
        foreach (var layer in gameWorld.Layers)
        {
            // FOR EACH MOB IN LAYER
            foreach (var m in layer.MobManager.mobs)
            {
                HandleMobBulletCollision(weaponsManager.Bullets, m, audio);
                HandlePlayerMobCollision(m, player, audio);
                HandleMobBigPlatformCollision(m, gameWorld.bigPlatform);
            }
        }

        // FOR EACH MOB PROJECTILE
        foreach(var mobProjectile in gameWorld.MobProjectiles)
        {
            if (mobProjectile.active == 0)
                continue;
            
            if (CollisionManager.CircleIntersectsRectangle(mobProjectile.bulletBounds, player.Bounds))
            {
                player.TakeDamage(mobProjectile.DAMAGE, audio);
                mobProjectile.active = 0;

                continue;
            }

            foreach (var playerBullet in weaponsManager.Bullets)
            {
                if (playerBullet.active == 0 || !playerBullet.bulletBounds.Intersects(mobProjectile.bulletBounds))
                    continue;

                playerBullet.active = 0;
                mobProjectile.active = 0;
            }
        }
    }

    public static void HandleMobBigPlatformCollision(IMob mob, Platform bigPlatform)
    {
        if (mob.Bounds.Intersects(bigPlatform.bounds)) {
            mob.BounceFromFloor();
        }
    }
    
    public static void HandlePlayerMobCollision(IMob mob, IPlayer player, IAudioService audio)
    {
        if (!mob.HittingPlayer && mob.Bounds.Intersects(player.Bounds))
        {
            mob.HittingPlayer = true;

            if (mob.ON_TOUCH == "hurt_player")
                player.TakeDamage(mob.DAMAGE, audio);
            
            else if (mob.ON_TOUCH == "explode")
                mob.Explode();
        }
        else if (!mob.Bounds.Intersects(player.Bounds))
        {
            mob.HittingPlayer = false;
        }
    }

    public void HandlePlayerPlatformCollision(IPlayer player, Rectangle plat)
    {
        // FALL OFF CHECK (did player walk off the active platform?)
            // PLAYER CANNOT WALK OFF BIG PLATFORM
        if (player.Bounds.Right < activePlatform.Y ||
            player.Bounds.Left > activePlatform.X)
            player.SetPlayerInAir(true);
        
        if (!player.Bounds.Intersects(plat))
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

    private static void HandleMobBulletCollision(List<Bullet> bullets, IMob mob, IAudioService audio)
    {
        foreach (var bullet in bullets)
        {
            if (!CollisionManager.CircleIntersectsRectangle(bullet.bulletBounds, mob.Bounds))
                continue;

            mob.TakeDamage(bullet.DAMAGE, audio);
            bullet.active = 0;
            break;
        }
    }

    public static void HandleBulletPlatformCollision(List<Bullet> bullets, Rectangle platBounds)
    {
        foreach (var bullet in bullets)
        {
            if (!CollisionManager.CircleIntersectsRectangle(bullet.bulletBounds, platBounds))
                continue;
            
            if (!bullet.bounce)
            {
                bullet.direction.Y *= -1;
                bullet.bounce = true;
            }
            else bullet.active = 0;
        }
    }
}