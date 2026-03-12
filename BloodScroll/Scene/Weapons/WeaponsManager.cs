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

public class WeaponsManager : IWeaponsManager, IDrawableLayer
{
    public int DrawLayer { get; set; } = 40;

    // GUNS
    private float gun_angle;
    private List<Sprite> _guns = [];
    public enum EquippedWeapon {
        BlueGun = 0,
        GreenGun = 1
    }
    public EquippedWeapon equippedWeapon = EquippedWeapon.BlueGun;

    public int WEAPONS_UNLOCKED = 0;
    
    // BULLETS
    private readonly List<string> bulletTypes = [];
    private readonly List<int> bulletDamage = [];
    private List<Bullet> _bullets = [];
    public List<Bullet> Bullets => _bullets; // Za interface
    private const float BULLET_SPEED = 1500.0f;

    // BULLET SPAWN TIMER
    private float bulletSpawnTimer = 0f;
    private readonly float timerSeconds = 0.2f;

    public void LoadContent(IPlayer player)
    {
        // WEAPON 0 - DEFAULT BLUE
        _guns.Add(Globals.Weapons.CreateSprite("gun-blue"));
        bulletTypes.Add("bullet-blue");
        bulletDamage.Add(25);

        // WEAPON 1 - BIGGER GREEN
        _guns.Add(Globals.Weapons.CreateSprite("gun-green"));
        bulletTypes.Add("bullet-green");
        bulletDamage.Add(50);

        SetGunPosition(player);
    }

    public void Restart(IPlayer player)
    {
        equippedWeapon = EquippedWeapon.BlueGun;
        _bullets.Clear();

        bulletSpawnTimer = 0f;

        SetGunPosition(player);
    }

    public void UnlockNewWeapon(int weaponID)
    {
        equippedWeapon = (EquippedWeapon)weaponID;
        WEAPONS_UNLOCKED = weaponID;
    }

    public void Update(IPlayer player, IAudioService audio)
    {
        SetGunPosition(player);
        Update_bullets();

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

    /////////////
    // BULLETS //
    ////////////
    private void Spawn_bullets(IAudioService audio)
    {
        // BUTTON PRESSED
        if (Globals.MouseState.LeftButton == ButtonState.Pressed)
        {
            if (Globals.HoldingLeftButton == false)
            {
                audio.PlaySound(AudioId.PlayerGun);

                // On (first) mouseclick: spawn a bullet
                _bullets.Add(new Bullet());
                _bullets.Last().LoadContent(
                    _guns[(int)equippedWeapon].Position, // SPAWN
                    Globals.MousePosition, // TARGET
                    bulletTypes[(int)equippedWeapon], 
                    bulletDamage[(int)equippedWeapon], 
                    BULLET_SPEED);

                Globals.HoldingLeftButton = true;
            }
            else {
                // On (not first) mouseclick: update bullet timer
                bulletSpawnTimer += Globals.DT;
                if (bulletSpawnTimer >= timerSeconds)
                {
                    audio.PlaySound(AudioId.PlayerGun);
                    
                    _bullets.Add(new Bullet());
                    _bullets.Last().LoadContent(
                        _guns[(int)equippedWeapon].Position, // SPAWN
                        Globals.MousePosition, // TARGET
                        bulletTypes[(int)equippedWeapon], 
                        bulletDamage[(int)equippedWeapon], 
                        BULLET_SPEED);

                    bulletSpawnTimer = 0f;
                }
            }
        }

        // BUTTON RELEASED
        else if (Globals.MouseState.LeftButton == ButtonState.Released)
        {
            // Reset bullet timer
            bulletSpawnTimer = 0f;
            Globals.HoldingLeftButton = false;
        }
    }

    private void Update_bullets()
    {
        // Update all _bullets (or remove them)
        for (int i = _bullets.Count - 1; i >= 0; i--) {
            if (_bullets[i].active == 1)
                _bullets[i].Update();
            else
                _bullets.RemoveAt(i);
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

    public void SwitchWeapon()
    {
        if (equippedWeapon == EquippedWeapon.BlueGun && WEAPONS_UNLOCKED >= (int)EquippedWeapon.GreenGun)
            equippedWeapon = EquippedWeapon.GreenGun;
        else equippedWeapon = EquippedWeapon.BlueGun;
    }
}