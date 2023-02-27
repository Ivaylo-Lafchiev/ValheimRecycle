using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ItemDrop;

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
            ValheimRecycle.instance.recycleButton.interactable = true;
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch("OnTabUpgradePressed")]
        internal static bool PrefixOnTabUpgradePressed(InventoryGui __instance)
        {
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
                Text component3 = elementRoot.transform.Find("res_amount").GetComponent<Text>();
                int amount = Utils.GetModifiedAmount(quality, req);

                component3.text = amount.ToString();
                component3.color = Color.green;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateCraftingPanel")]
        internal static bool PrefixUpdateCraftingPanel(InventoryGui __instance, bool focusView = false)
        {
            if (ValheimRecycle.instance != null)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer.GetCurrentCraftingStation() && (localPlayer.GetCurrentCraftingStation().gameObject.name.Contains("cauldron") || localPlayer.GetCurrentCraftingStation().gameObject.name.Contains("artisanstation")))
                {
                    ValheimRecycle.instance.recycleObject.SetActive(false);
                    ValheimRecycle.instance.recycleButton.interactable = true;
                    return true;
                }
                if (!localPlayer.GetCurrentCraftingStation() && !localPlayer.NoCostCheat())
                {
                    __instance.m_tabCraft.interactable = false;
                    __instance.m_tabUpgrade.interactable = true;
                    __instance.m_tabUpgrade.gameObject.SetActive(false);
                    ValheimRecycle.instance.recycleObject.SetActive(false);
                    ValheimRecycle.instance.recycleButton.interactable = true;
                }
                else
                {
                    __instance.m_tabUpgrade.gameObject.SetActive(true);
                    ValheimRecycle.instance.recycleObject.SetActive(true);
                }
                List<Recipe> recipes = new List<Recipe>();
                localPlayer.GetAvailableRecipes(ref recipes);
                __instance.UpdateRecipeList(recipes);
                if (__instance.m_availableRecipes.Count <= 0)
                {
                    __instance.SetRecipe(-1, focusView);
                    return false;
                }
                if (__instance.m_selectedRecipe.Key != null)
                {
                    int selectedRecipeIndex = __instance.GetSelectedRecipeIndex();
                    __instance.SetRecipe(selectedRecipeIndex, focusView);
                    return false;
                }
                __instance.SetRecipe(0, focusView);
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateRecipeList")]
        internal static void PostfixUpdateRecipeList(InventoryGui __instance, List<Recipe> recipes)
        {
            if (ValheimRecycle.instance.InTabDeconstruct())
            {
                Player localPlayer = Player.m_localPlayer;
                __instance.m_availableRecipes.Clear();
                foreach (GameObject gameObject in __instance.m_recipeList)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
                __instance.m_recipeList.Clear();

                //Debug.Log("Recipe list:\n");

                List<KeyValuePair<Recipe, ItemDrop.ItemData>> list = new List<KeyValuePair<Recipe, ItemDrop.ItemData>>();

                List<Recipe> newRecipesList = new List<Recipe>();

                for (int l = 0; l < recipes.Count; l++)
                {
                    Recipe recipe2 = recipes[l];
                    if (recipe2.m_item.m_itemData.m_shared.m_maxQuality >= 1)
                    {
                        __instance.m_tempItemList.Clear();

                        if (recipe2.m_item.m_itemData.m_shared.m_maxStackSize == 1)
                        {
                            localPlayer.GetInventory().GetAllItems(recipe2.m_item.m_itemData.m_shared.m_name, __instance.m_tempItemList);
                        }
                        // adding all stackable items from inventory to the list
                        else
                        {
                            for (int i = 0; i < localPlayer.GetInventory().m_inventory.Count; i++)
                            {
                                if (localPlayer.GetInventory().m_inventory[i].m_shared.m_name.Equals(recipe2.m_item.m_itemData.m_shared.m_name) &&
                                   localPlayer.GetInventory().m_inventory[i].m_stack >= recipe2.m_amount)
                                {

                                    __instance.m_tempItemList.Add(localPlayer.GetInventory().m_inventory[i]);
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
                foreach (KeyValuePair<Recipe, ItemDrop.ItemData> keyValuePair in list)
                {
                    //Debug.Log(keyValuePair.Key);
                    __instance.AddRecipeToList(localPlayer, keyValuePair.Key, keyValuePair.Value, true);

                }

                float num = (float)__instance.m_recipeList.Count * __instance.m_recipeListSpace;
                num = Mathf.Max(__instance.m_recipeListBaseSize, num);
                __instance.m_recipeListRoot.SetSizeWithCurrentAnchors((RectTransform.Axis)1, num);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateRecipe")]
        internal static bool PrefixUpdateRecipe(InventoryGui __instance, Player player, float dt)
        {
            if (ValheimRecycle.instance.InTabDeconstruct())
            {

                CraftingStation currentCraftingStation = player.GetCurrentCraftingStation();
                if (currentCraftingStation)
                {
                    __instance.m_craftingStationName.text = Localization.instance.Localize(currentCraftingStation.m_name);
                    __instance.m_craftingStationIcon.gameObject.SetActive(true);
                    __instance.m_craftingStationIcon.sprite = currentCraftingStation.m_icon;
                    int level = currentCraftingStation.GetLevel();
                    __instance.m_craftingStationLevel.text = level.ToString();
                    __instance.m_craftingStationLevelRoot.gameObject.SetActive(true);
                }
                else
                {
                    __instance.m_craftingStationName.text = Localization.instance.Localize("$hud_crafting");
                    __instance.m_craftingStationIcon.gameObject.SetActive(false);
                    __instance.m_craftingStationLevelRoot.gameObject.SetActive(false);
                }
                if (__instance.m_selectedRecipe.Key)
                {
                    __instance.m_recipeIcon.enabled = true;
                    __instance.m_recipeName.enabled = true;


                    ItemDrop.ItemData value = __instance.m_selectedRecipe.Value;
                    // don't show item description if item will be destroyed in process
                    if (value.m_quality == 1)
                    {
                        __instance.m_recipeDecription.enabled = false;
                    }
                    else
                    {
                        __instance.m_recipeDecription.enabled = true;
                    }
                    // edit here
                    int num = (value != null) ? (value.m_quality >= 1 ? value.m_quality - 1 : 0) : 1;
                    bool flag = num <= __instance.m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_maxQuality;
                    int num2 = (value != null) ? value.m_variant : __instance.m_selectedVariant;
                    __instance.m_recipeIcon.sprite = __instance.m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_icons[num2];
                    // edit here
                    string text = Localization.instance.Localize(__instance.m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_name);
                    if (__instance.m_selectedRecipe.Key.m_amount > 1)
                    {
                        text = text + " x" + __instance.m_selectedRecipe.Key.m_amount;
                    }
                    __instance.m_recipeName.text = text;

                    __instance.m_recipeDecription.text = Localization.instance.Localize(ItemDrop.ItemData.GetTooltip(__instance.m_selectedRecipe.Key.m_item.m_itemData, num, true));
                    if (value != null)
                    {
                        __instance.m_itemCraftType.gameObject.SetActive(true);
                        // edit here
                        if (value.m_quality <= 1)
                        {
                            // edit here
                            __instance.m_itemCraftType.text = "Item will be recycled";
                        }
                        else
                        {
                            string text2 = Localization.instance.Localize(value.m_shared.m_name);
                            //edit here
                            __instance.m_itemCraftType.text = "Downgrade " + text2 + " quality to " + (value.m_quality - 1).ToString();
                        }
                    }
                    else
                    {
                        __instance.m_itemCraftType.gameObject.SetActive(false);
                    }
                    __instance.m_variantButton.gameObject.SetActive(__instance.m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_variants > 1 && __instance.m_selectedRecipe.Value == null);
                    // edit here
                    __instance.SetupRequirementList(num + 1, player, flag);
                    int requiredStationLevel = 0;
                    CraftingStation requiredStation = __instance.m_selectedRecipe.Key.GetRequiredStation(num);
                    if (requiredStation != null && flag)
                    {
                        __instance.m_minStationLevelIcon.gameObject.SetActive(true);
                        __instance.m_minStationLevelText.text = requiredStationLevel.ToString();
                        if (currentCraftingStation == null || currentCraftingStation.GetLevel() < requiredStationLevel)
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
                    // have requirements always true, as item is already present in inventory
                    bool flag2 = true;
                    // count number of slots required to deconstruct
                    bool flag3 = Utils.HaveEmptySlotsForRecipe(player.GetInventory(), __instance.m_selectedRecipe.Key, num + 1);
                    bool flag4 = !requiredStation || (currentCraftingStation && currentCraftingStation.CheckUsable(player, false));
                    __instance.m_craftButton.interactable = (((flag2 && flag4) || player.NoCostCheat()) && flag3 && flag);
                    Text componentInChildren = __instance.m_craftButton.GetComponentInChildren<Text>();
                    componentInChildren.text = "Recycle";
                    UITooltip component = __instance.m_craftButton.GetComponent<UITooltip>();
                    if (!flag3)
                    {
                        component.m_text = Localization.instance.Localize("$inventory_full");
                    }
                    else if (!flag4)
                    {
                        component.m_text = Localization.instance.Localize("$msg_missingstation");
                    }
                    else
                    {
                        component.m_text = "";
                    }
                }
                else
                {
                    __instance.m_recipeIcon.enabled = false;
                    __instance.m_recipeName.enabled = false;
                    __instance.m_recipeDecription.enabled = false;
                    __instance.m_qualityPanel.gameObject.SetActive(false);
                    __instance.m_minStationLevelIcon.gameObject.SetActive(false);
                    __instance.m_craftButton.GetComponent<UITooltip>().m_text = "";
                    __instance.m_variantButton.gameObject.SetActive(false);
                    __instance.m_itemCraftType.gameObject.SetActive(false);
                    for (int i = 0; i < __instance.m_recipeRequirementList.Length; i++)
                    {
                        InventoryGui.HideRequirement(__instance.m_recipeRequirementList[i].transform);
                    }
                    __instance.m_craftButton.interactable = false;
                }
                if (__instance.m_craftTimer < 0f)
                {
                    __instance.m_craftProgressPanel.gameObject.SetActive(false);
                    __instance.m_craftButton.gameObject.SetActive(true);
                    return false;
                }
                __instance.m_craftButton.gameObject.SetActive(false);
                __instance.m_craftProgressPanel.gameObject.SetActive(true);
                __instance.m_craftProgressBar.SetMaxValue(__instance.m_craftDuration);
                __instance.m_craftProgressBar.SetValue(__instance.m_craftTimer);
                __instance.m_craftTimer += dt;
                if (__instance.m_craftTimer >= __instance.m_craftDuration)
                {
                    if (ValheimRecycle.instance.InTabDeconstruct())
                    {
                        Utils.DoRecycle(player, __instance);
                    }
                    else
                    {
                        __instance.DoCrafting(player);
                    }
                    __instance.m_craftTimer = -1f;
                }
                return false;
            }
            return true;
        }
    }

}