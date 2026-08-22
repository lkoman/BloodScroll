namespace BloodScroll;

//
// What the player is given for clearing a boss layer.
// Every field is optional - only the ones present in mobWaves.json are handed out.
// A heal is always included on top of these.
//

public class GiftData
{
    public int? IncreasedHP { get; set; }  // new maximum HP
    public int? GunID { get; set; }        // unlocks and equips this weapon
    public int? Shield { get; set; }       // shield capacity, soaks damage before HP
    public bool? DoubleJump { get; set; }  // a second jump in mid air
    public float? FireRate { get; set; }   // multiplier on how fast you can shoot
    public int? LifeSteal { get; set; }    // HP returned per mob killed

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
    }
}
