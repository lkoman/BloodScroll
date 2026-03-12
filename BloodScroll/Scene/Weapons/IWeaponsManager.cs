using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BloodScroll;

public interface IWeaponsManager
{
    public List<Bullet> Bullets { get; }
    public void UnlockNewWeapon(int weaponID);
}