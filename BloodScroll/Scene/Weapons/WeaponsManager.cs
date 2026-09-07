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
// FOUR GUNS, ONE TABLE
//
// Vse kar loči eno pištolo od druge je ena vrstica v Arsenal spodaj.
// Nothing else in this file knows which gun is equipped.
//
// Two fast guns to fight with, two slow ones on long cooldowns (shown in HUD).
//
// The fire rate gift divides EVERY gun's cooldown, not just the equipped one.
//

public class WeaponsManager : IWeaponsManager, IDrawableLayer
{
    public int DrawLayer { get; set; } = 40;

    // GUNS
    private float gun_angle;
    private List<Sprite> _guns = [];
    public enum EquippedWeapon {
        BlueGun = 0,
        GoldGun = 1,
        StunGun = 2,
        ShellGun = 3
    }
    public EquippedWeapon equippedWeapon = EquippedWeapon.BlueGun;

    // TESTING ONLY - true starts every run with the whole arsenal
    private const bool UNLOCK_ALL_WEAPONS = false;

    public int WEAPONS_UNLOCKED = StartingWeaponsUnlocked;

    // What a shot does when it arrives
    private enum ShotKind { Plain, Shell, Stun }

    //
    // ONE ROW PER GUN
    //
    //   GunRegion/GunTint       what you hold (slow guns = gold body, tinted)
    //   BulletRegion/BulletTint the shot. Region is only its SIZE - every bullet
    //                           is a code drawn circle, so tint is the look
    //   Damage                  for the shell this is the BLAST's damage
    //   Cooldown                seconds between shots, before the fire rate gift
    //   BulletScale             how big it is drawn AND how big it hits
    //   Knockback               px/s shove. Only the rifle has any
    //   ShowCooldown            whether it gets a bar in the HUD
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
        float Knockback = 0f,
        bool ShowCooldown = false);

    //
    // DPS: PISTOL 125, RIFLE 114.6. Rifle je ~8% zadaj.
    //
    private static readonly WeaponSpec[] Arsenal =
    [
        // THE STARTER. 12px bullet, highest DPS in the arsenal.
        new WeaponSpec(
            Name: "PISTOL",
            GunRegion: "gun-blue",
            GunTint: Color.White,
            BulletRegion: "bullet-blue",
            BulletTint: Globals.PistolBlue,
            Damage: 25,
            Cooldown: 0.20f,
            BulletSpeed: 1500f),

        // THE LONG ONE. 30px bullet, 55 damage, knockback (boss takes 1/3).
        // Slow bullet - you have to lead a moving target.
        new WeaponSpec(
            Name: "RIFLE",
            GunRegion: "gun-gold",
            GunTint: Color.White,
            BulletRegion: "bullet-gold",
            BulletTint: Globals.RifleGold,
            Damage: 55,
            Cooldown: 0.48f,
            BulletSpeed: 1100f,
            BulletScale: 1.5f,
            Knockback: 800f),

        // THE STUN. 0 damage, freezes for 2s, 6s cooldown.
        // Unlocked on layer 8 (spider queen is on layer 11).
        new WeaponSpec(
            Name: "STUN",
            GunRegion: "gun-gold",
            GunTint: Globals.StunPurple,    // PLACEHOLDER - the gold body, tinted
            BulletRegion: "projectile-purple",
            BulletTint: Globals.StunPurple,
            Damage: 0,
            Cooldown: 6f,
            BulletSpeed: 1200f,
            Kind: ShotKind.Stun,
            StunSeconds: 2f,
            ShowCooldown: true),

        // THE SHELL. Slow grenade, goes off after 1.5s, or on hitting anything,
        // or on a second click (see TryDetonateShells). Unlocked on layer 17.
        new WeaponSpec(
            Name: "SHELL",
            GunRegion: "gun-gold",
            GunTint: Globals.BlastOrange,   // PLACEHOLDER - the gold body, tinted
            BulletRegion: "projectile-fire",
            BulletTint: Globals.BlastOrange,
            Damage: 120,
            Cooldown: 4f,
            BulletSpeed: 750f,
            Kind: ShotKind.Shell,
            BlastRadius: 220f,
            FuseSeconds: 1.5f,
            ShowCooldown: true),
    ];

    // Highest gun index a fresh run begins with: the pistol alone, or the lot
    private static int StartingWeaponsUnlocked => UNLOCK_ALL_WEAPONS ? Arsenal.Length - 1 : 0;

    // How many guns there are, for the boss schedule to walk through when it
    // starts handing out upgrades one gun at a time
    public static int GunCount => Arsenal.Length;

    // ENDLESS UPGRADES. Once everything is unlocked the bosses hand out one
    // notch on one gun instead (see BossSchedule.Upgrade). Small steps - the
    // climb has no end, so the player collects many of them.
    private const float GUN_FIRE_RATE_STEP = 1.05f;
    private const float GUN_DAMAGE_STEP = 1.10f;

    // BULLETS
    private List<Bullet> _bullets = [];
    public List<Bullet> Bullets => _bullets; // Za interface

    // THE SWORD. Not in the table above and not in EquippedWeapon - G and the
    // wheel walk past it. Its own key, its own clock. See Sword.
    // It lives here because this is what gets handed the world and audio.
    private readonly Sword sword = new();
    public Sword Sword => sword;

    // ONE TIMER PER GUN, ALL RUNNING AT ONCE. Seconds left before that gun may
    // fire again. Every one counts down whichever gun is held, so the shell
    // recharges while you fight with the pistol.
    private readonly float[] cooldowns = new float[Arsenal.Length];

    // The "faster gun" gift. Every cooldown is divided by it.
    private float fireRateMultiplier = 1f;

    // AND ONE PAIR PER GUN. Kept separate from the multiplier above: that one
    // is won once and speeds up all four, these are the endless upgrades and
    // only touch the gun they name.
    private readonly float[] gunFireRate = FreshMultipliers();
    private readonly float[] gunDamage = FreshMultipliers();

    // One per gun, every one of them "as the gun was written"
    private static float[] FreshMultipliers() => Enumerable.Repeat(1f, Arsenal.Length).ToArray();

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

        // Every gun back to what it was written as. The upgrades are earned
        // from the bosses of THIS run, the same way the guns themselves are.
        Array.Fill(gunFireRate, 1f);
        Array.Fill(gunDamage, 1f);

        Array.Clear(cooldowns);

        sword.Restart();

        SetGunPosition(player);
    }

    // A gun already unlocked is never taken away - the boss schedule cycles
    // forever and can hand the same gift out twice
    public void UnlockNewWeapon(int weaponID)
    {
        if (weaponID < 0 || weaponID >= Arsenal.Length)
            return;

        WEAPONS_UNLOCKED = Math.Max(WEAPONS_UNLOCKED, weaponID);
        equippedWeapon = (EquippedWeapon)weaponID;
    }

    // First boss hands this over. Idempotent - the schedule cycles.
    public void UnlockSword() => sword.Unlock();

    // DEBUG (F3). The whole arsenal, sword included. Does NOT equip anything -
    // the player walks up to the new guns with G or the wheel.
    public void UnlockAllWeapons()
    {
        WEAPONS_UNLOCKED = Arsenal.Length - 1;

        sword.Unlock();
    }

    // Stacks, so winning it twice makes you twice as fast
    public void IncreaseFireRate(float multiplier)
    {
        fireRateMultiplier *= multiplier;
    }

    // ONE GUN, ONE NOTCH. Both stack. An unknown gun is ignored, not thrown on.
    public void UpgradeFireRate(int weaponID)
    {
        if (!InArsenal(weaponID))
            return;

        gunFireRate[weaponID] *= GUN_FIRE_RATE_STEP;
    }

    public void UpgradeDamage(int weaponID)
    {
        if (!InArsenal(weaponID))
            return;

        gunDamage[weaponID] *= GUN_DAMAGE_STEP;
    }

    // What the gift card calls the gun it just improved
    public string WeaponName(int weaponID) => InArsenal(weaponID) ? Arsenal[weaponID].Name : "GUN";

    private static bool InArsenal(int weaponID) => weaponID >= 0 && weaponID < Arsenal.Length;

    public void Update(IPlayer player, IAudioService audio, GameWorld gameWorld)
    {
        SetGunPosition(player);

        // Every gun's clock runs whether or not the player is alive or shooting
        TickCooldowns();

        // Its own key, its own clock - swung with the gun still loaded
        sword.Update(player, audio, gameWorld);

        // BEFORE the shells move, so the blast lands where the shell was drawn
        // when the player clicked. Also before Spawn_bullets, or the shell
        // fired by this click would be set off by the same click.
        bool cutAFuse = Globals.PLAYER_ALIVE && TryDetonateShells();

        Update_bullets(gameWorld);

        if (!Globals.PLAYER_ALIVE)
            return;

        SetGunRotation();

        // A click that set a shell off is SPENT - it cannot also fire the next
        if (!cutAFuse)
            Spawn_bullets(audio);
    }

    //
    // THE SHELL GOES OFF WHEN YOU SAY SO
    //
    // One click sends it, the next sets it off. A shell still also goes off on
    // its own fuse and on touching anything - this is a third way.
    // Only works while the shell gun is equipped.
    //
    private bool TryDetonateShells()
    {
        if (Spec.Kind != ShotKind.Shell || !Clicked())
            return false;

        bool any = false;

        foreach (Bullet bullet in _bullets)
        {
            // Anything already spent is left alone
            if (bullet.active == 0 || !bullet.Explosive || bullet.FuseSpent)
                continue;

            bullet.CutFuse();
            any = true;
        }

        return any;
    }

    // A FRESH PRESS, not the trigger still held - guns fire on hold, so
    // "click again" has to mean the button coming back down
    private static bool Clicked() =>
        Globals.MouseState.LeftButton == ButtonState.Pressed &&
        Globals.LastMouseState.LeftButton == ButtonState.Released;

    public List<IDrawableLayer> GetDrawables()
    {
        // All player bullets
        var list = new List<IDrawableLayer>(_bullets);

        // This file draws the gun
        list.Add(this);

        // And the sword draws itself, over the top of both of them
        list.Add(sword);

        return list;
    }

    public void Draw()
    {
        DrawGun();
    }

    //
    // WHAT THE HUD DRAWS
    //
    // Only unlocked guns with ShowCooldown. Ready climbs back to 1 as the gun
    // recharges, so the bar FILLS UP - full bar means ready.
    //
    public IReadOnlyList<WeaponCooldown> Cooldowns
    {
        get
        {
            var bars = new List<WeaponCooldown>();

            // SWORD FIRST so its bar never moves - it is unlocked before the
            // stun (layer 8) and the shell (17), which are added under it
            if (sword.Unlocked)
                bars.Add(sword.CooldownBar);

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

    // The gun's own wait, divided by both fire rate multipliers
    private float FullCooldown(int weapon) =>
        Arsenal[weapon].Cooldown / (fireRateMultiplier * gunFireRate[weapon]);

    // Same idea for what a shot takes off. Rounded once, here, so the HUD and
    // the mob it hits can never disagree about it.
    //
    // THE FLOOR OF ONE IS FOR GUNS THAT DEAL DAMAGE, so that a bad rounding can
    // never leave a shot doing nothing. A gun written as 0 means it, and no
    // number of damage gifts turns it into a weapon - the stun stays a stun.
    private int ShotDamage(int weapon)
    {
        int baseDamage = Arsenal[weapon].Damage;

        if (baseDamage == 0)
            return 0;

        return Math.Max(1, (int)MathF.Round(baseDamage * gunDamage[weapon]));
    }

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
        // BOTH HANDS ARE ON THE SWORD. Nothing is fired while the blade is out,
        // whichever gun is equipped and however long its own clock says it has
        // been ready - a swing costs you the fifth of a second it takes.
        //
        // The trigger is not remembered, it is simply ignored: holding it down
        // through a swing starts firing again the frame the blade is put away,
        // and a click spent entirely inside one is a click that never happened.
        if (sword.Swinging)
            return;

        bool held = Globals.MouseState.LeftButton == ButtonState.Pressed;

        // Holding the trigger down fires as fast as the equipped gun allows -
        // which for the two slow guns is once every four or six seconds
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
            ShotDamage((int)equippedWeapon),
            spec.BulletSpeed,
            BulletEffect.Damage,
            spec.BulletScale);

        bullet.Tint = spec.BulletTint;
        bullet.Explosive = spec.Kind == ShotKind.Shell;
        bullet.BlastRadius = spec.BlastRadius;
        bullet.Fuse = spec.FuseSeconds;
        bullet.StunSeconds = spec.StunSeconds;
        bullet.Knockback = spec.Knockback;

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
    // key covers four guns however many of them the player has won.
    // A step of -1 walks the arsenal the other way, which is what the wheel
    // rolled the other way has to do - the double modulo is there because a
    // negative step would otherwise land on a negative index.
    public void SwitchWeapon(int step = 1)
    {
        int unlocked = WEAPONS_UNLOCKED + 1;
        equippedWeapon = (EquippedWeapon)((((int)equippedWeapon + step) % unlocked + unlocked) % unlocked);
    }
}
