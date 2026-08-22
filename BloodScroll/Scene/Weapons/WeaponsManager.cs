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
// in the HUD: the stun for the one thing you cannot afford to have moving, the
// shell for a group.
//

// The whole cooldown of every gun is divided by the fire rate multiplier, so
// "faster gun" makes ALL FOUR faster - including the four and six second ones.
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
        StunGun = 2,
        ShellGun = 3
    }
    public EquippedWeapon equippedWeapon = EquippedWeapon.BlueGun;

    // TESTING ONLY - set back to false before handing the game in. Starts every
    // run with the whole arsenal so the slow guns can be tried without waiting
    // for the boss to hand them over.
    private const bool UNLOCK_ALL_WEAPONS = false;

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
    //   Knockback               how hard the shot shoves what it hits, in pixels
    //                           per second. Only the rifle has any
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
        float Knockback = 0f,
        bool ShowCooldown = false);

    //
    // THE TWO FAST GUNS, AND WHY THE SLOW ONE IS THE WORSE ONE
    //
    // Damage over ten seconds of holding the trigger on a boss, which is the
    // only fight long enough for the number to mean anything:
    //
    //   PISTOL   25 x (10 / 0.20) = 50 shots = 1250
    //   RIFLE    55 x (10 / 0.48) = 20.8 shots = 1146
    //
    // 125 DPS against 114.6 - the rifle is a shade over 8% behind. That gap is
    // deliberately small enough to be worth arguing about and never small enough
    // to be a rounding error: if you only care how fast the thing in front of you
    // dies, the pistol is the answer, always.
    //
    // What the rifle sells instead is everything that happens between the shots.
    // A 55 point bullet is more than twice the pistol's, and a mob shoved back on
    // every one of them is a mob not touching you - the DPS you give up buys the
    // hits you never take. The pistol out-damages it and the rifle out-lives it.
    //
    private static readonly WeaponSpec[] Arsenal =
    [
        // THE STARTER. Small shots, quick, no surprises - this is the gun the
        // whole game is balanced around and the one you spend most of it holding.
        // The highest DPS in the arsenal, and the least forgiving: a 12 pixel
        // bullet at 25 a time has to actually land, fifty times, to do its work.
        new WeaponSpec(
            Name: "PISTOL",
            GunRegion: "gun-blue",
            GunTint: Color.White,
            BulletRegion: "bullet-blue",
            BulletTint: Globals.PistolBlue,
            Damage: 25,
            Cooldown: 0.20f,
            BulletSpeed: 1500f),

        // THE LONG ONE. Slower in every sense - between the shots and in the air -
        // and it does not add up to more damage, on purpose. It is not an upgrade,
        // it is a trade, and what you buy is threefold:
        //
        //   SIZE     a 30 pixel bullet against the pistol's 12. Forgiving against
        //            a bat that will not hold still, and it catches two of them
        //            lined up
        //   WEIGHT   55 a shot. Things that die in two hits die in two hits
        //   KNOCKBACK the shove. The crab walking you down goes back a hundred
        //            pixels every time you hit it, and a boss a third of that
        //
        // The slow bullet is part of the price, not an oversight: at 1100 you
        // have to lead a moving target, which is the cost of the fat forgiving
        // hitbox that made it easy to hit in the first place.
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

        // THE STUN. Takes nothing off whatever it hits - not a point - and
        // freezes it solid for two seconds. It is not a weak gun, it is not a
        // gun at all: the only thing it does is buy time, and every kill still
        // has to come from one of the other three.
        //
        // Six seconds between shots means it is never crowd
        // control - it is one answer, to one thing, at a time. Two frozen in
        // every six is a window to act in, not a mob taken off the board: the
        // other four seconds the thing is awake and coming, and that ratio is
        // what stops the gun from simply deleting a boss one press at a time.
        //
        // THE THIRD GUN, and it is third because of what the player meets next.
        // It is handed over on layer 8 and the spider queen is on layer 11: the
        // one boss that pins the player in place is the one boss he can pin
        // back, and he gets the tool for it with one layer to spare. Behind the
        // shell instead, the answer would arrive nine layers after the question.
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

        // THE SHELL. Fires a slow lit grenade that goes off a second and a half
        // later, wherever it has got to - or the moment it hits anything, or
        // the moment you click again (see TryDetonateShells). The fuse is the
        // deadline; the second click is the aim.
        //
        // LAST, because it is the one gun that asks something of the player
        // rather than answering something for him - a fuse to read and a second
        // click to time. By layer 17 he has the room to learn it.
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

    //
    // THE UPGRADES THAT NEVER RUN OUT
    //
    // Once the boss roster has given away everything there is to unlock, what
    // keeps arriving is one notch on one gun (see BossSchedule.Upgrade). These
    // are how big a notch is, and they are deliberately small: the climb has no
    // end, so the player collects a great many of them, and a step that felt
    // generous the first time would be absurd twenty bosses later.
    //
    // The fire rate step is the smaller of the two on purpose. Damage buys a
    // shorter fight. Cooldown buys a different game - a shell every two seconds
    // instead of every four stops being an answer to a crowd and starts being
    // the only gun worth holding.
    //
    private const float GUN_FIRE_RATE_STEP = 1.05f;
    private const float GUN_DAMAGE_STEP = 1.10f;

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

    //
    // AND ONE PAIR OF MULTIPLIERS PER GUN
    //
    // Kept alongside the one above rather than folded into it, because they are
    // not the same gift: that one speeds up ALL FOUR guns and is won once,
    // these improve a single gun and keep coming forever. Folding them together
    // would make the endless upgrades quietly buff guns they never named.
    //
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

    // DEBUG (F3). The whole arsenal at once, without waiting for four bosses.
    // The runtime version of UNLOCK_ALL_WEAPONS above, and the better of the
    // two: that one is a const that has to be edited and rebuilt and then
    // remembered about before handing the game in, this one is a key you press
    // and a run you restart.
    //
    // It does NOT equip anything - the player keeps whatever is in his hands
    // and walks up to the new guns with G or the wheel, same as always.
    public void UnlockAllWeapons()
    {
        WEAPONS_UNLOCKED = Arsenal.Length - 1;
    }

    // Stacks, so winning it twice makes you twice as fast
    public void IncreaseFireRate(float multiplier)
    {
        fireRateMultiplier *= multiplier;
    }

    // ONE GUN, ONE NOTCH. Both stack the same way the gift above does, and both
    // silently ignore a gun that does not exist rather than throwing - a gift
    // is the game being generous, and it should never be the thing that
    // crashes a run somebody has spent half an hour on.
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

    // What the gift card calls the gun it just improved. "Faster SHELL" says
    // something; "Faster Gun" on the twelfth boss says nothing at all.
    public string WeaponName(int weaponID) => InArsenal(weaponID) ? Arsenal[weaponID].Name : "GUN";

    private static bool InArsenal(int weaponID) => weaponID >= 0 && weaponID < Arsenal.Length;

    public void Update(IPlayer player, IAudioService audio, GameWorld gameWorld)
    {
        SetGunPosition(player);

        // Every gun's clock runs whether or not the player is alive or shooting
        TickCooldowns();

        // ASKED BEFORE THE SHELLS MOVE, so the blast lands where the shell was
        // drawn when the player clicked rather than a frame further along - at
        // 750 a second that is a dozen pixels, and the whole point of the
        // second click is that the player picked the spot.
        //
        // It also has to come before Spawn_bullets, or the shell fired by this
        // very click would be sitting in the list waiting to be set off by it.
        bool cutAFuse = Globals.PLAYER_ALIVE && TryDetonateShells();

        Update_bullets(gameWorld);

        if (!Globals.PLAYER_ALIVE)
            return;

        SetGunRotation();

        // A click that set a shell off is SPENT. It cannot also load the next
        // one - otherwise a player whose shell gun has been upgraded far enough
        // to recharge inside its own fuse would detonate and fire on one press
        // and never understand where the second shell came from.
        if (!cutAFuse)
            Spawn_bullets(audio);
    }

    //
    // THE SHELL GOES OFF WHEN YOU SAY SO
    //
    // One click sends it, the next sets it off. Neither of the two ways a shell
    // already ended has gone anywhere - it still goes off on its own when the
    // fuse runs out, and still the instant it touches anything - this is a
    // third, and the only one the player chooses.
    //
    // What it buys is the shot a fixed fuse cannot make: the blast lands on the
    // group that is under the shell NOW, instead of wherever the shell has got
    // to a second and a half after it left the gun. Aiming a shell stops being
    // a guess about the future and becomes a decision you make in the air.
    //
    // Only while the shell gun is the one in your hands. Swapping back to the
    // pistol to keep the pressure up means your clicks are the pistol's, and a
    // shell already in the air is left to burn its own fuse out.
    //
    private bool TryDetonateShells()
    {
        if (Spec.Kind != ShotKind.Shell || !Clicked())
            return false;

        bool any = false;

        foreach (Bullet bullet in _bullets)
        {
            // Anything already spent is left alone - a shell that hit a mob
            // this frame is waiting to be cleared, not waiting to be lit
            if (bullet.active == 0 || !bullet.Explosive || bullet.FuseSpent)
                continue;

            bullet.CutFuse();
            any = true;
        }

        return any;
    }

    // A FRESH PRESS, not the trigger still being held. Holding the button down
    // is how every gun fires, so "click again" has to mean the button coming
    // back down - or a single held press would set the shell off the frame
    // after it was fired.
    private static bool Clicked() =>
        Globals.MouseState.LeftButton == ButtonState.Pressed &&
        Globals.LastMouseState.LeftButton == ButtonState.Released;

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

    // The gun's own wait, divided by the gift that speeds up everything and by
    // whatever notches this particular gun has been given since
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
