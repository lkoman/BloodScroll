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
    public void SwitchWeapon();
    public void IncreaseFireRate(float multiplier);
}
