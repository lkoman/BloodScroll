using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BloodScroll;

//
// One gun's recharge, as the HUD needs it.
//   Ready   0 the instant it was fired, climbing to 1 as it comes back, so the
//           bar FILLS UP and a full bar means the gun can be used
//   Colour  the same colour the gun and its shots are drawn in, so the bar in
//           the corner is obviously the gun in your hand
//
public readonly record struct WeaponCooldown(string Name, float Ready, Color Colour);

public interface IWeaponsManager
{
    public List<Bullet> Bullets { get; }

    // Only the guns slow enough to be worth waiting for, and only once unlocked
    public IReadOnlyList<WeaponCooldown> Cooldowns { get; }

    public void UnlockNewWeapon(int weaponID);
    // step of +1 walks up the arsenal, -1 walks back down it
    public void SwitchWeapon(int step = 1);

    // Every gun at once, won once - see the note at the top of WeaponsManager
    public void IncreaseFireRate(float multiplier);

    // One gun, one notch, handed out forever once the unlocks run out
    public void UpgradeFireRate(int weaponID);
    public void UpgradeDamage(int weaponID);

    // What to call that gun on the gift card
    public string WeaponName(int weaponID);
}
