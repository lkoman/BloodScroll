using System.Text;

namespace BloodScroll;

//
// Hands out what the player won for clearing a boss layer, and writes
// the text that goes on the gift card.
//
// This used to live inside Layer, which meant a class about level geometry
// also knew about guns and max HP. New gift types only touch this file,
// GiftData and the player/weapons side that actually stores them.
//

public static class RewardService
{
    public static string GiveRewards(GiftData gifts, IPlayer player, IWeaponsManager weaponsManager)
    {
        StringBuilder text = new();

        // A heal comes with every boss kill
        text.Append("- Heal\n");

        if (gifts != null)
        {
            if (gifts.GunID.HasValue)
            {
                text.Append("- New Gun\n");

                // The gift card is the only place the game ever talks to the
                // player, and a second gun he does not know how to reach is
                // not a gift. Said every time, because by the fourth gun the
                // one line he read on layer 5 is long gone.
                text.Append("  Switch the guns by clicking G\n");

                weaponsManager.UnlockNewWeapon(gifts.GunID.Value);
            }

            if (gifts.IncreasedHP.HasValue)
            {
                text.Append("- Increased HP\n");
                player.IncreaseMaxHP(gifts.IncreasedHP.Value);
            }

            if (gifts.Shield.HasValue)
            {
                text.Append("- Shield\n");
                player.GrantShield(gifts.Shield.Value);
            }

            if (gifts.DoubleJump == true)
            {
                text.Append("- Double Jump\n");
                player.GrantDoubleJump();
            }

            if (gifts.FireRate.HasValue)
            {
                text.Append("- Faster Gun\n");
                weaponsManager.IncreaseFireRate(gifts.FireRate.Value);
            }

            if (gifts.LifeSteal.HasValue)
            {
                text.Append("- Life Steal\n");
                player.GrantLifeSteal(gifts.LifeSteal.Value);
            }
        }

        player.Heal();
        player.RefillShield();

        return text.ToString();
    }
}
