using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using System;

namespace Equilibrium {
    public class Equilibrium : Mod {
    }
    public class EquilibriumPlayer : ModPlayer {
        public int totalKills;
        public int totalDeaths;
        public int EquilibriumModifier {
            get {
                int difference = totalKills - totalDeaths;
                return (int)Math.Floor(difference / 12.0);
            }
        }
        public override void SaveData(TagCompound tag) {
            tag["totalKills"] = totalKills;
            tag["totalDeaths"] = totalDeaths;
        }
        public override void LoadData(TagCompound tag) {
            totalKills = tag.GetInt("totalKills");
            totalDeaths = tag.GetInt("totalDeaths");
        }
        public override void OnKill(NPC npc) {
            if (!npc.townNPC && !npc.friendly && npc.lifeMax > 10 && npc.damage > 0) {
                totalKills++;
            }
        }
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) {
            totalDeaths++;
        }
        public override void ResetEffects() {
            int em = EquilibriumModifier;
            if (em > 0) {
                Player.maxMinions += (int)Math.Floor(em / 10.0);
                float lifeBonusPercent = Math.Min(5.00f, 0.005f * em);
                Player.statLifeMax2 += (int)(Player.statLifeMax * lifeBonusPercent);
                float manaBonusPercent = Math.Min(0.50f, 0.01f * em);
                Player.statManaMax2 += (int)(Player.statManaMax * manaBonusPercent);
                Player.lifeRegen += (int)em;
                Player.statDefense += (int)em;
                Player.knockbackResist += Math.Min(0.75f, 0.01f * em);
            }
        }
        public override void PostUpdate() {
            int em = EquilibriumModifier;
            if (em == 0) return;
            float damageMultiplier = em > 0 ? 0.015f : 0.02f;
            Player.GetDamage(DamageClass.Generic) += damageMultiplier * em;
            Player.GetCritChance(DamageClass.Generic) += 0.5f * Math.Max(0, em);
            Player.moveSpeed += 0.01f * Math.Max(0, em);
            Player.endurance += 0.01f * Math.Max(0, -em);
            float healthReductionPercent = 0.01f * Math.Max(0, -em);
            float cappedHealthReduction = Math.Min(0.25f, healthReductionPercent);
            Player.statLifeMax2 -= (int)(Player.statLifeMax * cappedHealthReduction);
        }
        public override void ModifyBuffTime(int type, ref int time) {
            int em = EquilibriumModifier;
            if (type == BuffID.PotionSickness && em < 0) {
                float durationIncrease = 0.03f * -em;
                time += (int)(time * durationIncrease);
            }
        }
        public override void ModifyShoppingSettings(ShoppingSettings shopSettings) {
            int em = EquilibriumModifier;
            if (em > 0) {
                float discount = Math.Min(0.99f, 0.01f * em);
                shopSettings.PriceAdjustment -= discount;
            }
        }
    }
    public class KarmicLedger : ModItem {
        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 32;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.value = Item.sellPrice(silver: 10);
            Item.rare = ItemRarityID.Blue;
            Item.autoReuse = false;
        }
        public override bool? UseItem(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                var modPlayer = player.GetModPlayer<EquilibriumPlayer>();
                int em = modPlayer.EquilibriumModifier;
                int difference = modPlayer.totalKills - modPlayer.totalDeaths;
                Color messageColor = em > 0 ? Color.LawnGreen : (em < 0 ? Color.IndianRed : Color.White);
                Main.NewText("--- Equilibrium Status ---", Color.Gold);
                Main.NewText($"Total Kills: {modPlayer.totalKills}", Color.LightGreen);
                Main.NewText($"Total Deaths: {modPlayer.totalDeaths}", Color.LightCoral);
                Main.NewText($"Kill/Death Differential: {difference}", Color.Cyan);
                Main.NewText($"Equilibrium Modifier (EM): {em}", messageColor);
                if (em > 0) {
                    Main.NewText($"Damage Bonus: +{1.5f * em:F1}%", messageColor);
                    Main.NewText($"Crit Chance Bonus: +{0.5f * em:F1}%", messageColor);
                    Main.NewText($"Movement Speed Bonus: +{1f * em:F1}%", messageColor);
                    Main.NewText($"Max Health Bonus: +{Math.Min(25f, 0.5f * em):F1}%", messageColor);
                    Main.NewText($"Max Mana Bonus: +{Math.Min(50f, 1f * em):F1}%", messageColor);
                    Main.NewText($"Life Regen Bonus: +{(int)Math.Floor(em / 5.0)}", messageColor);
                    Main.NewText($"Defense Bonus: +{(int)Math.Floor(em / 4.0)}", messageColor);
                    Main.NewText($"Knockback Resist: +{Math.Min(50f, 1f * em):F0}%", messageColor);
                    Main.NewText($"Bonus Max Minions: +{(int)Math.Floor(em / 10.0)}", messageColor);
                    Main.NewText($"Shop Discount: {Math.Min(99f, 1f * em):F0}%", messageColor);
                }
                else if (em < 0) {
                    int absEm = -em;
                    Main.NewText($"Damage Penalty: -{2f * absEm:F1}%", messageColor);
                    Main.NewText($"Damage Reduction: +{1f * absEm:F1}%", messageColor);
                    Main.NewText($"Max Health Penalty: -{Math.Min(25f, 1f * absEm):F1}%", messageColor);
                    Main.NewText($"Potion Sickness Duration: +{3f * absEm:F1}%", messageColor);
                }
                else {
                    Main.NewText("Your stats are in balance.", messageColor);
                }
            }
            return true;
        }
        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Book, 1);
            recipe.AddIngredient(ItemID.Bone, 15);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }
}
