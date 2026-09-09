using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace ValheimRecycle
{
    [HarmonyPatch(typeof(InventoryGui))]
    public class InventoryGuiPatch
    {

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        internal static void PostfixUpdate(InventoryGui __instance) => ValheimRecycle.instance?.RebuildRecycleTab();

        [HarmonyPrefix]
        [HarmonyPatch("OnTabCraftPressed")]
        internal static bool PrefixOnTabCraftPressed(InventoryGui __instance)
        {
            ValheimRecycle.IsRecycleTabActive = false;
            ValheimRecycle.instance.recycleButton.interactable = true;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnTabUpgradePressed")]
        internal static bool PrefixOnTabUpgradePressed(InventoryGui __instance)
        {
            ValheimRecycle.IsRecycleTabActive = false;
            ValheimRecycle.instance.recycleButton.interactable = true;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("SetupRequirement")]
        internal static void PostfixSetupRequirement(Transform elementRoot, Piece.Requirement req, int quality)
        {
            // don't flash the resource amount in requirements window if deconstructing
            if (ValheimRecycle.instance.InTabDeconstruct())
            {
                TMP_Text component3 = elementRoot.transform.Find("res_amount").GetComponent<TMP_Text>();
                int amount = Utils.GetModifiedAmount(quality, req);

                component3.text = amount.ToString();
                component3.color = Color.green;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateCraftingPanel")]
        internal static void PostfixUpdateCraftingPanel(InventoryGui __instance, bool focusView)
        {
            if (ValheimRecycle.instance == null || ValheimRecycle.instance.recycleObject == null) return;

            Player localPlayer = Player.m_localPlayer;
            if (localPlayer.GetCurrentCraftingStation() && (localPlayer.GetCurrentCraftingStation().gameObject.name.Contains("cauldron") || localPlayer.GetCurrentCraftingStation().gameObject.name.Contains("artisanstation")))
            {
                ValheimRecycle.instance.recycleObject.SetActive(false);
                ValheimRecycle.instance.recycleButton.interactable = true;
                if (ValheimRecycle.IsRecycleTabActive)
                {
                    ValheimRecycle.IsRecycleTabActive = false;
                }
                return;
            }

            // Sync visibility with upgrade tab
            ValheimRecycle.instance.recycleObject.SetActive(__instance.m_tabUpgrade.gameObject.activeSelf);

            if (ValheimRecycle.IsRecycleTabActive)
            {
                __instance.m_tabUpgrade.interactable = true;
                __instance.m_tabCraft.interactable = true;
                ValheimRecycle.instance.recycleButton.interactable = false;
            }
            else
            {
                ValheimRecycle.instance.recycleButton.interactable = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateRecipeList")]
        internal static void PostfixUpdateRecipeList(InventoryGui __instance, List<Recipe> recipes)
        {

            if (ValheimRecycle.instance.InTabDeconstruct())
            {
                Player localPlayer = Player.m_localPlayer;
                Inventory localPlayerInventory = localPlayer.GetInventory();

                foreach (InventoryGui.RecipeDataPair recipeDataPair in __instance.m_availableRecipes)
                {
                    Object.Destroy(recipeDataPair.InterfaceElement);
                }
                __instance.m_availableRecipes.Clear();

                List<KeyValuePair<Recipe, ItemDrop.ItemData>> list = new List<KeyValuePair<Recipe, ItemDrop.ItemData>>();

                for (int l = 0; l < recipes.Count; l++)
                {
                    Recipe recipe2 = recipes[l];
                    if (recipe2.m_item.m_itemData.m_shared.m_maxQuality >= 1)
                    {
                        __instance.m_tempItemList.Clear();

                        if (recipe2.m_item.m_itemData.m_shared.m_maxStackSize == 1)
                        {
                            localPlayerInventory.GetAllItems(recipe2.m_item.m_itemData.m_shared.m_name, __instance.m_tempItemList);
                        }
                        else
                        {
                            for (int i = 0; i < localPlayerInventory.m_inventory.Count; i++)
                            {
                                if (localPlayerInventory.m_inventory[i].m_shared.m_name.Equals(recipe2.m_item.m_itemData.m_shared.m_name) &&
                                   localPlayerInventory.m_inventory[i].m_stack >= recipe2.m_amount)
                                {
                                    __instance.m_tempItemList.Add(localPlayerInventory.m_inventory[i]);
                                    break;
                                }
                            }
                        }
                        foreach (ItemDrop.ItemData itemData in __instance.m_tempItemList)
                        {
                            if (itemData.m_quality >= 1)
                            {
                                list.Add(new KeyValuePair<Recipe, ItemDrop.ItemData>(recipe2, itemData));
                            }
                        }
                    }
                }

                // filter out equipped items
                var equipped = localPlayerInventory.GetEquippedItems().Select(item => item.GetHashCode());
                list.RemoveAll(m => equipped.Contains(m.Value.GetHashCode()));

                // filter out hotbar items
                var hotbarItems = new List<ItemDrop.ItemData>();
                localPlayerInventory.GetBoundItems(hotbarItems);
                var hotbarItemsHashes = hotbarItems.Select(item => item.GetHashCode());
                list.RemoveAll(m => hotbarItemsHashes.Contains(m.Value.GetHashCode()));

                CraftingStation currentCraftingStation = localPlayer.GetCurrentCraftingStation();
                foreach (KeyValuePair<Recipe, ItemDrop.ItemData> keyValuePair in list)
                {
                    Recipe recipe = keyValuePair.Key;
                    ItemDrop.ItemData itemData = keyValuePair.Value;
                    
                    int targetQuality = (itemData != null) ? itemData.m_quality : 1;
                    CraftingStation reqStation = recipe.GetRequiredStation(targetQuality);
                    int reqLevel = recipe.GetRequiredStationLevel(targetQuality);
                    
                    bool hasStation = (reqStation == null) || (currentCraftingStation != null && currentCraftingStation.CheckUsable(localPlayer, false) && currentCraftingStation.GetLevel() >= reqLevel);
                    
                    int qualityIndex = (itemData != null) ? (itemData.m_quality >= 1 ? itemData.m_quality - 1 : 0) : 1;
                    bool hasEmptySlots = Utils.HaveEmptySlotsForRecipe(localPlayerInventory, recipe, qualityIndex + 1);
                    
                    bool canRecycle = (hasStation || localPlayer.NoCostCheat()) && hasEmptySlots;
                    
                    __instance.AddRecipeToList(localPlayer, recipe, itemData, canRecycle);
                }

                float num = (float)__instance.m_availableRecipes.Count * __instance.m_recipeListSpace;
                num = Mathf.Max(__instance.m_recipeListBaseSize, num);
                __instance.m_recipeListRoot.SetSizeWithCurrentAnchors((RectTransform.Axis)1, num);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateRecipe")]
        internal static void PostfixUpdateRecipe(InventoryGui __instance, Player player, float dt)
        {
            if (ValheimRecycle.instance.InTabDeconstruct() && __instance.m_selectedRecipe.Recipe)
            {
                ItemDrop.ItemData value = __instance.m_selectedRecipe.ItemData;
                int num = (value != null) ? (value.m_quality >= 1 ? value.m_quality - 1 : 0) : 1;
                bool flag = num <= __instance.m_selectedRecipe.Recipe.m_item.m_itemData.m_shared.m_maxQuality;
                bool flag3 = Utils.HaveEmptySlotsForRecipe(player.GetInventory(), __instance.m_selectedRecipe.Recipe, num + 1);
                CraftingStation currentCraftingStation = player.GetCurrentCraftingStation();
                int targetQuality = (value != null) ? value.m_quality : 1;
                CraftingStation reqStation = __instance.m_selectedRecipe.Recipe.GetRequiredStation(targetQuality);
                int reqLevel = __instance.m_selectedRecipe.Recipe.GetRequiredStationLevel(targetQuality);
                bool flag4 = (reqStation == null) || (currentCraftingStation != null && currentCraftingStation.CheckUsable(player, false) && currentCraftingStation.GetLevel() >= reqLevel);

                if (reqStation != null && flag)
                {
                    __instance.m_minStationLevelIcon.gameObject.SetActive(true);
                    __instance.m_minStationLevelText.text = reqLevel.ToString();
                    if (currentCraftingStation == null || currentCraftingStation.GetLevel() < reqLevel)
                    {
                        __instance.m_minStationLevelText.color = ((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : __instance.m_minStationLevelBasecolor);
                    }
                    else
                    {
                        __instance.m_minStationLevelText.color = __instance.m_minStationLevelBasecolor;
                    }
                }
                else
                {
                    __instance.m_minStationLevelIcon.gameObject.SetActive(false);
                }
                // don't show item description if item will be destroyed in process
                if (value != null && value.m_quality == 1)
                {
                    __instance.m_recipeDecription.enabled = false;
                }

                string text = Localization.instance.Localize(__instance.m_selectedRecipe.Recipe.m_item.m_itemData.m_shared.m_name);
                if (__instance.m_selectedRecipe.Recipe.m_amount > 1)
                {
                    text = text + " x" + __instance.m_selectedRecipe.Recipe.m_amount;
                }
                __instance.m_recipeName.text = text;

                if (value != null)
                {
                    __instance.m_itemCraftType.gameObject.SetActive(true);
                    if (value.m_quality <= 1)
                    {
                        __instance.m_itemCraftType.text = "Item will be recycled";
                    }
                    else
                    {
                        string text2 = Localization.instance.Localize(value.m_shared.m_name);
                        __instance.m_itemCraftType.text = "Downgrade " + text2 + " quality to " + (value.m_quality - 1).ToString();
                    }
                }

                __instance.SetupRequirementList(num + 1, player, flag, 1);

                __instance.m_craftButton.interactable = ((flag4 || player.NoCostCheat()) && flag3 && flag);
                TMP_Text componentInChildren = __instance.m_craftButton.GetComponentInChildren<TMP_Text>();
                componentInChildren.text = "Recycle";

                UITooltip component = __instance.m_craftButton.GetComponent<UITooltip>();
                if (!flag3)
                {
                    component.m_text = Localization.instance.Localize("$inventory_full");
                }
                else if (!flag4)
                {
                    if (currentCraftingStation != null && currentCraftingStation.CheckUsable(player, false) && currentCraftingStation.GetLevel() < reqLevel)
                    {
                        component.m_text = "Workstation level too low";
                    }
                    else
                    {
                        component.m_text = Localization.instance.Localize("$msg_missingstation");
                    }
                }
                else
                {
                    component.m_text = "";
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoCrafting")]
        internal static bool PrefixDoCrafting(InventoryGui __instance, Player player)
        {
            if (ValheimRecycle.instance.InTabDeconstruct())
            {
                Utils.DoRecycle(player, __instance);
                return false; // Prevent original crafting logic and use our own
            }
            return true;
        }
    }
}