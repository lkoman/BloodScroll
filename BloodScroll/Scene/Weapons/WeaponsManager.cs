using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace BloodScroll;

//
// - TRACKS EQUPPED GUN
// - Draws equipped gun
// - Spawns, updates, draws _bullets
//
//
// FOUR GUNS, ONE TABLE
//
// Everything that makes one gun different from another is a line in Arsenal
// below - the art, the colour, the damage, how often it may be fired and what
// its shot does when it lands. Nothing else in this file knows which gun is
// equipped; it reads the row and does what the row says.
//
// The two fast guns are the ones you fight with. The two slow ones are answers
// to a specific problem, which is why they are on cooldowns long enough to see
// in the HUD: the shell for a group, the stun for the one thing you cannot
// afford to have moving.
//

// The whole cooldown of every gun is divided by the fire rate multiplier, so
// "faster gun" makes ALL FOUR faster - including the five and ten second ones.
// A gift that only sped up the gun you happened to be holding would be worth a
// different amount to two players who won it on the same layer.

public class WeaponsManager : IWeaponsManager, IDrawableLayer
{
    public int DrawLayer { get; set; } = 40;

    // GUNS
    private float gun_angle;
    private List<Sprite> _guns = [];
    public enum EquippedWeapon {
        BlueGun = 0,
        GoldGun = 1,
        ShellGun = 2,
        StunGun = 3
    }
    public EquippedWeapon equippedWeapon = EquippedWeapon.BlueGun;

    // TESTING ONLY - set back to false before handing the game in. Starts every
    // run with the whole arsenal so the slow guns can be tried without waiting
    // for the boss to hand them over.
    private const bool UNLOCK_ALL_WEAPONS = true;

    public int WEAPONS_UNLOCKED = StartingWeaponsUnlocked;

    // What a shot does when it arrives
    private enum ShotKind { Plain, Shell, Stun }

    //
    // ONE ROW PER GUN
    //
    //   GunRegion/GunTint       what you hold. The two slow guns borrow the big
    //                           gold body and are told apart by colour -
    //                           PLACEHOLDER, same trick MobArt uses
    //   BulletRegion/BulletTint what leaves it, in the same colour as the gun.
    //                           The REGION is only how big the shot is - every
    //                           bullet is a code drawn circle now, so the tint
    //                           is the whole of what it looks like
    //   Damage                  what it takes off. For the shell this is the
    //                           BLAST's damage - the shell itself never hits
    //   Cooldown                seconds between shots, before the fire rate gift
    //   BulletScale             how big the shot is drawn AND how big it hits,
    //                           because the hitbox is measured off the sprite
    //   ShowCooldown            whether it gets a bar in the HUD. Only the guns
    //                           slow enough that waiting for one is a decision
    private record WeaponSpec(
        string Name,
        string GunRegion,
        Color GunTint,
        string BulletRegion,
        Color BulletTint,
        int Damage,
        float Cooldown,
        float BulletSpeed,
        ShotKind Kind = ShotKind.Plain,
        float BulletScale = 1f,
        float BlastRadius = 0f,
        float FuseSeconds = 0f,
        float StunSeconds = 0f,
        bool ShowCooldown = false);

    private static readonly WeaponSpec[] Arsenal =
    [
        // THE STARTER. Small shots, quick, no surprises - this is the gun the
        // whole game is balanced around and the one you spend most of it holding.
        new WeaponSpec(
            Name: "PISTOL",
            GunRegion: "gun-blue",
            GunTint: Color.White,
            BulletRegion: "bullet-blue",
            BulletTint: Globals.PistolBlue,
            Damage: 25,
            Cooldown: 0.20f,
            BulletSpeed: 1500f),

        // THE LONG ONE. Same damage as the pistol and a bit slower, so it is not
        // an upgrade - it is a trade. What you buy is the SIZE of the shot: a
        // fat bullet is far more forgiving against a bat that will not hold
        // still, and far better at catching two of them lined up.
        new WeaponSpec(
            Name: "RIFLE",
            GunRegion: "gun-gold",
            GunTint: Color.White,
            BulletRegion: "bullet-gold",
            BulletTint: Globals.RifleGold,
            Damage: 25,
            Cooldown: 0.38f,
            BulletSpeed: 1350f,
            BulletScale: 1.4f),

        // THE SHELL. Fires a slow lit grenade that goes off a second and a half
        // later, wherever it has got to - or the moment it hits anything. Aiming
        // it is aiming at where a group WILL be, not where it is.
        new WeaponSpec(
            Name: "SHELL",
            GunRegion: "gun-gold",
            GunTint: Globals.BlastOrange,   // PLACEHOLDER - the gold body, tinted
            BulletRegion: "projectile-fire",
            BulletTint: Globals.BlastOrange,
            Damage: 120,
            Cooldown: 5f,
            BulletSpeed: 750f,
            Kind: ShotKind.Shell,
            BlastRadius: 220f,
            FuseSeconds: 1.5f,
            ShowCooldown: true),

        // THE STUN. Barely scratches whatever it hits and freezes it solid for
        // three and a half seconds. Ten seconds between shots means it is never
        // crowd control - it is one answer, to one thing, once.
        new WeaponSpec(
            Name: "STUN",
            GunRegion: "gun-gold",
            GunTint: Globals.StunPurple,    // PLACEHOLDER - the gold body, tinted
            BulletRegion: "projectile-purple",
            BulletTint: Globals.StunPurple,
            Damage: 25,
            Cooldown: 10f,
            BulletSpeed: 1200f,
            Kind: ShotKind.Stun,
            StunSeconds: 3.5f,
            ShowCooldown: true),
    ];

    // Highest gun index a fresh run begins with: the pistol alone, or the lot
    private static int StartingWeaponsUnlocked => UNLOCK_ALL_WEAPONS ? Arsenal.Length - 1 : 0;

    // BULLETS
    private List<Bullet> _bullets = [];
    public List<Bullet> Bullets => _bullets; // Za interface

    //
    // ONE TIMER PER GUN, AND THEY ALL RUN AT ONCE
    //
    // Seconds still to wait before that gun may be fired again. Every one of
    // them counts down every frame whichever gun is in your hands, so the shell
    // recharges while you are fighting with the pistol - swapping to the slow
    // gun the moment its bar fills is the point, and it could not work if the
    // clock only ran while you were holding it.
    private readonly float[] cooldowns = new float[Arsenal.Length];

    // Raised by the "faster gun" gift. Every cooldown is divided by it.
    private float fireRateMultiplier = 1f;

    private WeaponSpec Spec => Arsenal[(int)equippedWeapon];

    public void LoadContent(IPlayer player)
    {
        foreach (WeaponSpec spec in Arsenal)
        {
            Sprite gun = Globals.Weapons.CreateSprite(spec.GunRegion);
            gun.Color = spec.GunTint;

            _guns.Add(gun);
        }

        SetGunPosition(player);
    }

    public void Restart(IPlayer player)
    {
        equippedWeapon = EquippedWeapon.BlueGun;
        WEAPONS_UNLOCKED = StartingWeaponsUnlocked; // a new run starts with only the blue gun again
        fireRateMultiplier = 1f;
        _bullets.Clear();

        Array.Clear(cooldowns);

        SetGunPosition(player);
    }

    // A gun already unlocked is never taken away, so a gift handed out twice
    // (the boss schedule cycles forever) cannot walk the player backwards
    public void UnlockNewWeapon(int weaponID)
    {
        if (weaponID < 0 || weaponID >= Arsenal.Length)
            return;

        WEAPONS_UNLOCKED = Math.Max(WEAPONS_UNLOCKED, weaponID);
        equippedWeapon = (EquippedWeapon)weaponID;
    }

    // Stacks, so winning it twice makes you twice as fast
    public void IncreaseFireRate(float multiplier)
    {
        fireRateMultiplier *= multiplier;
    }

    public void Update(IPlayer player, IAudioService audio, GameWorld gameWorld)
    {
        SetGunPosition(player);

        // Every gun's clock runs whether or not the player is alive or shooting
        TickCooldowns();

        Update_bullets(gameWorld);

        if (!Globals.PLAYER_ALIVE)
            return;

        SetGunRotation();
        Spawn_bullets(audio);
    }

    public List<IDrawableLayer> GetDrawables()
    {
        var list = new List<IDrawableLayer>();

        // All player bullets
        foreach (Bullet bullet in _bullets) {
            list.Add(bullet);
        }

        // This file draws the gun
        list.Add(this);

        return list;
    }

    public void Draw()
    {
        DrawGun();
    }

    //
    // WHAT THE HUD DRAWS
    //
    // Only the guns the player actually has, and only the ones slow enough to
    // be worth a bar. Ready is 1 when it can be fired and climbs back to 1 as
    // it recharges, so the bar FILLS UP - a full bar means go.
    //
    public IReadOnlyList<WeaponCooldown> Cooldowns
    {
        get
        {
            var bars = new List<WeaponCooldown>();

            for (int i = 0; i <= WEAPONS_UNLOCKED && i < Arsenal.Length; i++)
            {
                if (!Arsenal[i].ShowCooldown)
                    continue;

                bars.Add(new WeaponCooldown(
                    Arsenal[i].Name,
                    1f - cooldowns[i] / FullCooldown(i),
                    Arsenal[i].GunTint));
            }

            return bars;
        }
    }

    private float FullCooldown(int weapon) => Arsenal[weapon].Cooldown / fireRateMultiplier;

    private void TickCooldowns()
    {
        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (cooldowns[i] > 0f)
                cooldowns[i] -= Globals.DT;
        }
    }

    /////////////
    // BULLETS //
    ////////////
    private void Spawn_bullets(IAudioService audio)
    {
        bool held = Globals.MouseState.LeftButton == ButtonState.Pressed;

        // Holding the trigger down fires as fast as the equipped gun allows -
        // which for the two slow guns is once every five or ten seconds
        if (!held || cooldowns[(int)equippedWeapon] > 0f)
            return;

        Fire(audio);
    }

    private void Fire(IAudioService audio)
    {
        WeaponSpec spec = Spec;

        audio.PlaySound(AudioId.PlayerGun);

        Bullet bullet = new();
        bullet.LoadContent(
            GetGunTipPosition(),    // SPAWN
            Globals.MousePosition,  // TARGET
            spec.BulletRegion,
            spec.Damage,
            spec.BulletSpeed,
            BulletEffect.Damage,
            spec.BulletScale);

        bullet.Tint = spec.BulletTint;
        bullet.Explosive = spec.Kind == ShotKind.Shell;
        bullet.BlastRadius = spec.BlastRadius;
        bullet.Fuse = spec.FuseSeconds;
        bullet.StunSeconds = spec.StunSeconds;

        _bullets.Add(bullet);

        cooldowns[(int)equippedWeapon] = FullCooldown((int)equippedWeapon);
    }

    private Vector2 GetGunTipPosition()
    {
        var gun = _guns[(int)equippedWeapon];

        Vector2 origin = new Vector2(0, gun.Height / 2); // same as Draw
        Vector2 tipLocal = new Vector2(gun.Width, 0) - origin;

        // rotate offset
        Vector2 gunTipPos = gun.Position +
            new Vector2(
                tipLocal.X * MathF.Cos(gun_angle) - tipLocal.Y * MathF.Sin(gun_angle),
                tipLocal.X * MathF.Sin(gun_angle) + tipLocal.Y * MathF.Cos(gun_angle)
            );

        return gunTipPos;
    }

    private void Update_bullets(GameWorld gameWorld)
    {
        // Update all _bullets (or remove them)
        for (int i = _bullets.Count - 1; i >= 0; i--) {
            if (_bullets[i].active == 0)
            {
                _bullets.RemoveAt(i);
                continue;
            }

            _bullets[i].Update();

            // A SHELL THAT HIT NOTHING still goes off. This is the only place
            // that notices, because the collision code only ever looks at shots
            // that touched something.
            if (_bullets[i].FuseSpent)
            {
                gameWorld.Detonate(_bullets[i]);
                _bullets[i].active = 0;
            }
        }
    }

    /////////
    // GUN //
    ////////
    private void SetGunPosition(IPlayer player)
    {
        _guns[(int)equippedWeapon].Position = new Vector2(player.Position.X + player.Width / 2, player.Position.Y + player.Height / 2 + 10);
    }

    private void SetGunRotation()
    {
        // kot okoli (gun.pos.x, gun.pos.y) vektorja (gun.pos -> mouse.pos)
        gun_angle = MathF.Atan2(Globals.MousePosition.Y - _guns[(int)equippedWeapon].Position.Y, Globals.MousePosition.X - _guns[(int)equippedWeapon].Position.X);
    }

    private void DrawGun()
    {
        _guns[(int)equippedWeapon].Draw(gun_angle, new Vector2(0, _guns[(int)equippedWeapon].Height / 2));
    }

    // Cycles through everything unlocked and wraps round to the pistol, so one
    // key covers four guns however many of them the player has won
    public void SwitchWeapon()
    {
        equippedWeapon = (EquippedWeapon)(((int)equippedWeapon + 1) % (WEAPONS_UNLOCKED + 1));
    }
}
