using HarmonyLib;

namespace ValheimRecycle.GameClasses
{
    [HarmonyPatch(typeof(Humanoid))]
    class HumanoidPatch
    {
        [HarmonyPatch(nameof(Humanoid.EquipItem))]
        [HarmonyPostfix]
        static void EquipItem(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects = true)
        {
            if (__instance.IsPlayer()) InventoryGui.instance?.UpdateCraftingPanel();
        }
            
        [HarmonyPatch(nameof(Humanoid.UnequipItem))]
        [HarmonyPostfix]
        static void UnequipItem(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects = true)
        {
            if (__instance.IsPlayer()) InventoryGui.instance?.UpdateCraftingPanel();
        }
    }
}