using System;
using UnityEngine;

namespace ValheimRecycle
{
    public class Utils
    {

        public static int GetModifiedAmount(int quality, Piece.Requirement requirement)
        {
            return (int)Math.Round(ValheimRecycle.instance.resourceMultiplier.Value * requirement.GetAmount(quality), 0);
        }

        public static bool HaveEmptySlotsForRecipe(Inventory inventory, Recipe recipe, int quality)
        {
            int emptySlots = inventory.GetEmptySlots();
            int requiredSlots = 0;

            foreach (Piece.Requirement req in recipe.m_resources)
            {
                if (GetModifiedAmount(quality, req) > 0) requiredSlots++;
            }
            if (emptySlots >= requiredSlots) return true;
            return false;
        }

        public static void AddResources(Inventory inventory, Piece.Requirement[] requirements, int qualityLevel)
        {

            foreach (Piece.Requirement requirement in requirements)
            {
                if (requirement.m_resItem)
                {

                    int amount = GetModifiedAmount(qualityLevel + 1, requirement);
                    if (amount > 0)
                    {
                        Debug.Log("Adding item: " + requirement.m_resItem.name);
                        Debug.Log("Amount: " + requirement.GetAmount(qualityLevel + 1));

                        inventory.AddItem(requirement.m_resItem.name, amount, requirement.m_resItem.m_itemData.m_quality, requirement.m_resItem.m_itemData.m_variant, 0L, "");
                    }
                }
            }
        }
        internal static void DoRecycle(Player player, InventoryGui __instance)
        {
            if (__instance.m_craftRecipe == null)
            {
                return;
            }
            int downgradedQuality = (__instance.m_craftUpgradeItem != null) ? (__instance.m_craftUpgradeItem.m_quality - 1) : 0;

            if (__instance.m_craftUpgradeItem != null && !player.GetInventory().ContainsItem(__instance.m_craftUpgradeItem))
            {
                return;
            }
            if (__instance.m_craftUpgradeItem == null && HaveEmptySlotsForRecipe(player.GetInventory(), __instance.m_craftRecipe, downgradedQuality + 1))
            {
                return;
            }
            int variant = __instance.m_craftUpgradeItem.m_variant;
            long playerID = player.GetPlayerID();
            string playerName = player.GetPlayerName();
            if (__instance.m_craftUpgradeItem != null)
            {
                if (downgradedQuality >= 1)
                {
                    player.UnequipItem(__instance.m_craftUpgradeItem, true);
                    if (ValheimRecycle.instance.preserveOriginalItem.Value)
                    {
                        __instance.m_craftUpgradeItem.m_quality = downgradedQuality;
                    }
                    else
                    {
                        player.GetInventory().RemoveItem(__instance.m_craftUpgradeItem);
                        player.GetInventory().AddItem(__instance.m_craftRecipe.m_item.gameObject.name, __instance.m_craftRecipe.m_amount, downgradedQuality, variant, playerID, playerName);
                    }
                }
                else
                {
                    player.UnequipItem(__instance.m_craftUpgradeItem, true);
                    player.GetInventory().RemoveItem(__instance.m_craftUpgradeItem, __instance.m_craftRecipe.m_amount);
                    
                }

            }

            AddResources(player.GetInventory(), __instance.m_craftRecipe.m_resources, downgradedQuality);

            __instance.UpdateCraftingPanel(true);

            CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
            if (currentCraftingStation)
            {
                currentCraftingStation.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity, null, 1f);
            }
            else
            {
                __instance.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity, null, 1f);
            }
            Game.instance.GetPlayerProfile().m_playerStats.m_crafts++;
            Gogan.LogEvent("Game", "Crafted", __instance.m_craftRecipe.m_item.m_itemData.m_shared.m_name, (long)downgradedQuality);
        }
  
    }
}
