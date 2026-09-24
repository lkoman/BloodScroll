using System;
using System.Text;
using MonoGameLibrary;

namespace BloodScroll;

//
// Hands out what the player won for clearing a boss layer, and writes the text
// that goes on the gift card.
//
// A new gift type only touches this file, GiftData, and the player/weapons side
// that stores it.
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

            // Named with its key, the same way the gun gift names G and the
            // wheel. Nothing else in the game is on Q, so a sword the player
            // never finds the key for is a gift he never received.
            if (gifts.Sword == true)
            {
                text.Append("- Sword\n");
                text.Append("  Q to swing it, aimed with the mouse\n");

                weaponsManager.UnlockSword();
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

            // THE ENDLESS ONES. Measured against what the player ALREADY has, so
            // the same gift is still worth something the twentieth time.

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

            //
            // THE WHOLE LOOP, PAID AT THE END OF IT
            //
            // Multiplies what is already banked rather than adding a lump, so
            // it is worth more the further in the loop was cleared - which is
            // the point. Nothing is handed to the player here; the reward is
            // the number on the board.
            //
            if (gifts.ScoreMultiplier.HasValue)
            {
                float multiplier = gifts.ScoreMultiplier.Value;

                text.Append(multiplier == 2f
                    ? "- SCORE DOUBLED\n"
                    : $"- SCORE x{multiplier:0.##}\n");

                // In long, then clamped. The multiplier compounds every loop,
                // and a deep enough run would otherwise wrap the score around
                // into a negative number at the moment it was going best.
                long boosted = (long)(Globals.POINTS * multiplier);

                Globals.POINTS = (int)Math.Min(boosted, int.MaxValue);
            }
        }

        player.Heal();
        player.RefillShield();

        return text.ToString();
    }
}
