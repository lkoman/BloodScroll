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
                text.Append("  G or mousewheel for change of weapon\n");

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

            //
            // THE ENDLESS ONES
            //
            // Every one of these is measured against what the player already
            // has rather than against a number written somewhere else, which is
            // what lets the same gift be won twenty times and be worth
            // something on the twentieth.
            //

            if (gifts.BonusHP.HasValue)
            {
                text.Append("- More HP\n");
                player.IncreaseMaxHP(player.MaxHP + gifts.BonusHP.Value);
            }

            if (gifts.BonusShield.HasValue)
            {
                text.Append("- Stronger Shield\n");
                player.GrantShield(player.ShieldMax + gifts.BonusShield.Value);
            }

            // Named on the card, because by this point in a run the player has
            // four guns and "faster gun" would leave him guessing which
            if (gifts.FasterGun.HasValue)
            {
                text.Append($"- Faster {weaponsManager.WeaponName(gifts.FasterGun.Value)}\n");
                weaponsManager.UpgradeFireRate(gifts.FasterGun.Value);
            }

            if (gifts.StrongerGun.HasValue)
            {
                text.Append($"- Stronger {weaponsManager.WeaponName(gifts.StrongerGun.Value)}\n");
                weaponsManager.UpgradeDamage(gifts.StrongerGun.Value);
            }
        }

        player.Heal();
        player.RefillShield();

        return text.ToString();
    }
}
