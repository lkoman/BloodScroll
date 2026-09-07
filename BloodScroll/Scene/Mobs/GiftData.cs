namespace BloodScroll;

//
// What the player is given for clearing a boss layer.
// Every field is optional - only the ones present in mobWaves.json are handed out.
// A heal is always included on top of these.
//

public class GiftData
{
    //
    // THE ONE-OFF PRIZES
    //
    // Worth winning exactly once. Every one is an ABSOLUTE value - the new
    // maximum, the new capacity - so handing one out twice would measure against
    // the JSON rather than against what the player has built up.
    //
    public int? IncreasedHP { get; set; }  // new maximum HP
    public int? GunID { get; set; }        // unlocks and equips this weapon
    public int? Shield { get; set; }       // shield capacity, soaks damage before HP
    public bool? DoubleJump { get; set; }  // a second jump in mid air

    // The blade on Q. Not a GunID: it is not in the arsenal and nothing about
    // it can be won twice, so it is a flag rather than a weapon slot.
    public bool? Sword { get; set; }
    public float? FireRate { get; set; }   // multiplier on how fast EVERY gun shoots
    public int? LifeSteal { get; set; }    // HP returned per mob killed

    //
    // THE ONES THAT KEEP COMING
    //
    // Past the end of the list above the bosses hand out increments instead -
    // see BossSchedule.Upgrade. ADDED to what the player has rather than
    // replacing it, so winning one twenty times is twenty steps forward.
    //
    public int? BonusHP { get; set; }      // added to the current maximum HP
    public int? BonusShield { get; set; }  // added to the current shield capacity

    // A gun ID, not an amount. How big a notch is belongs to the guns
    // themselves and is written once in WeaponsManager - a gift only has to
    // say WHICH gun got better, which is also all the gift card can usefully
    // tell the player.
    public int? FasterGun { get; set; }    // this one gun's cooldown shortens
    public int? StrongerGun { get; set; }  // this one gun's shots hit harder

    // Copies every value that is set in "other" over this one
    public void MergeFrom(GiftData other)
    {
        if (other == null)
            return;

        IncreasedHP = other.IncreasedHP ?? IncreasedHP;
        GunID = other.GunID ?? GunID;
        Shield = other.Shield ?? Shield;
        DoubleJump = other.DoubleJump ?? DoubleJump;
        Sword = other.Sword ?? Sword;
        FireRate = other.FireRate ?? FireRate;
        LifeSteal = other.LifeSteal ?? LifeSteal;

        BonusHP = other.BonusHP ?? BonusHP;
        BonusShield = other.BonusShield ?? BonusShield;
        FasterGun = other.FasterGun ?? FasterGun;
        StrongerGun = other.StrongerGun ?? StrongerGun;
    }
}
