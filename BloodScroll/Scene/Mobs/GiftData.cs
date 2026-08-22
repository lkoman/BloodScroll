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
    // Things the player either has or does not, and which are worth winning
    // exactly once. Every one of them is an ABSOLUTE value - the new maximum,
    // the new capacity - which is why they cannot be handed out twice: the
    // second one would be measured against the number in the JSON rather than
    // against what the player has actually built up.
    //
    public int? IncreasedHP { get; set; }  // new maximum HP
    public int? GunID { get; set; }        // unlocks and equips this weapon
    public int? Shield { get; set; }       // shield capacity, soaks damage before HP
    public bool? DoubleJump { get; set; }  // a second jump in mid air
    public float? FireRate { get; set; }   // multiplier on how fast EVERY gun shoots
    public int? LifeSteal { get; set; }    // HP returned per mob killed

    //
    // THE ONES THAT KEEP COMING
    //
    // The climb has no end and the list above does, so past it the bosses hand
    // out increments instead - see BossSchedule.Upgrade. These are ADDED to
    // what the player already has rather than replacing it, which is the whole
    // difference: winning one twenty times over is twenty steps forward.
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
        FireRate = other.FireRate ?? FireRate;
        LifeSteal = other.LifeSteal ?? LifeSteal;

        BonusHP = other.BonusHP ?? BonusHP;
        BonusShield = other.BonusShield ?? BonusShield;
        FasterGun = other.FasterGun ?? FasterGun;
        StrongerGun = other.StrongerGun ?? StrongerGun;
    }
}
