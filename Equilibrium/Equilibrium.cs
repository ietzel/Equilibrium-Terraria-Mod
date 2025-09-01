using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using System;

// A namespace to keep the mod's code organized.
namespace Equilibrium
{
    // The main class for the mod.
    public class Equilibrium : Mod
    {
    }

    // This class is attached to the player to track and apply all stat changes.
    public class EquilibriumPlayer : ModPlayer
    {
        // --- Player-specific variables ---
        public int totalKills;
        public int totalDeaths;

        // A calculated property to get the Equilibrium Modifier (EM) on the fly.
        public int EquilibriumModifier
        {
            get
            {
                // The core logic: (kills - deaths) / 12, rounded down.
                int difference = totalKills - totalDeaths;
                return (int)Math.Floor(difference / 12.0);
            }
        }

        // --- Saving and Loading Data ---
        public override void SaveData(TagCompound tag)
        {
            tag["totalKills"] = totalKills;
            tag["totalDeaths"] = totalDeaths;
        }

        public override void LoadData(TagCompound tag)
        {
            totalKills = tag.GetInt("totalKills");
            totalDeaths = tag.GetInt("totalDeaths");
        }

        // --- Event Hooks to Track Kills and Deaths ---
        public override void OnKill(NPC npc)
        {
            // We add some conditions to prevent farming weak or non-hostile creatures.
            if (!npc.townNPC && !npc.friendly && npc.lifeMax > 10 && npc.damage > 0)
            {
                totalKills++;
            }
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            totalDeaths++;
        }

        // --- Applying the Stat Modifications ---

        // This method is called every frame to apply our custom stats from the Equilibrium Modifier.
        public override void PostUpdate()
        {
            // Get the single modifier variable that will drive all calculations.
            int em = EquilibriumModifier;

            // If the modifier is zero, we don't need to do anything.
            if (em == 0) return;

            // --- Unified Stat Calculations ---

            // Damage: +1.5% per point if positive, -2% per point if negative.
            // A ternary operator chooses the multiplier based on em's sign.
            // For negative em, the result of (0.02f * em) is already negative, correctly applying a penalty.
            float damageMultiplier = em > 0 ? 0.015f : 0.02f;
            Player.GetDamage(DamageClass.Generic) += damageMultiplier * em;

            // Critical Strike Chance: +0.5% per point. Only applies if em is positive.
            // Math.Max(0, em) ensures this value is 0 if em is negative.
            Player.GetCritChance(DamageClass.Generic) += 0.5f * Math.Max(0, em);

            // Movement Speed: +1% per point. Only applies if em is positive.
            Player.moveSpeed += 0.01f * Math.Max(0, em);

            // Damage Reduction: +1% per point. Only applies if em is negative.
            // Math.Max(0, -em) will be the positive equivalent of a negative em (e.g., if em is -3, this is 3).
            Player.endurance += 0.01f * Math.Max(0, -em);

            // Max Health Penalty: -1% per point, capped at -25%. Only applies if em is negative.
            float healthReductionPercent = 0.01f * Math.Max(0, -em);
            float cappedHealthReduction = Math.Min(0.25f, healthReductionPercent);
            Player.statLifeMax2 -= (int)(Player.statLifeMax * cappedHealthReduction);
        }

        // This hook modifies buff times, perfect for the Potion Sickness penalty.
        public override void ModifyBuffTime(int type, ref int time)
        {
            int em = EquilibriumModifier;
            
            // If the buff is Potion Sickness and our modifier is negative...
            if (type == BuffID.PotionSickness && em < 0)
            {
                // Potion Sickness Duration: +3% per negative point
                float durationIncrease = 0.03f * -em; // -em makes it positive
                time += (int)(time * durationIncrease);
            }
        }
    }
    
    // An item for players to check their stats.
    public class KarmicLedger : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.value = Item.sellPrice(silver: 10);
            Item.rare = ItemRarityID.Blue;
            Item.autoReuse = false;
        }

        // This is what happens when the player uses the item.
        public override bool? UseItem(Player player)
        {
            // Ensure this only runs on the local client in multiplayer.
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<EquilibriumPlayer>();
                int em = modPlayer.EquilibriumModifier;
                int difference = modPlayer.totalKills - modPlayer.totalDeaths;

                // Determine the color of the main message based on the modifier.
                Color messageColor = em > 0 ? Color.LawnGreen : (em < 0 ? Color.IndianRed : Color.White);

                Main.NewText("--- Equilibrium Status ---", Color.Gold); // Unified title
                Main.NewText($"Total Kills: {modPlayer.totalKills}", Color.LightGreen);
                Main.NewText($"Total Deaths: {modPlayer.totalDeaths}", Color.LightCoral);
                Main.NewText($"Kill/Death Differential: {difference}", Color.Cyan);
                Main.NewText($"Equilibrium Modifier (EM): {em}", messageColor);

                // Display the calculated stat changes. The logic here remains branched
                // because the *types* of stats that change are different for positive and negative EM.
                if (em > 0)
                {
                    Main.NewText($"Damage Bonus: +{1.5f * em:F1}%", messageColor);
                    Main.NewText($"Crit Chance Bonus: +{0.5f * em:F1}%", messageColor);
                    Main.NewText($"Movement Speed Bonus: +{1f * em:F1}%", messageColor);
                }
                else if (em < 0)
                {
                    int absEm = -em;
                    Main.NewText($"Damage Penalty: -{2f * absEm:F1}%", messageColor);
                    Main.NewText($"Damage Reduction: +{1f * absEm:F1}%", messageColor);
                    Main.NewText($"Max Health Penalty: -{Math.Min(25f, 1f * absEm):F1}%", messageColor);
                    Main.NewText($"Potion Sickness Duration: +{3f * absEm:F1}%", messageColor);
                }
                else
                {
                    Main.NewText("Your stats are in balance.", messageColor);
                }
            }
            return true;
        }

        // The recipe to craft the Karmic Ledger.
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Book, 1);
            recipe.AddIngredient(ItemID.Bone, 15);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }
}
