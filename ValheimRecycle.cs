using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecycle
{
    [BepInPlugin("org.lafchi.plugins.valheim_recycle", "Valheim Recycle", "1.0.0.0")]
    [BepInProcess("valheim.exe")]
    public class ValheimRecycle : BaseUnityPlugin
    {

        internal static ValheimRecycle instance;
        internal GameObject recycleObject;
        internal Button recycleButton;
        internal float width;
        Vector3 craftingPos;
        Harmony harmony;

        internal bool InTabDeconstruct()
        {
            return !recycleButton.interactable;
        }

        internal void Awake()
        {
            Logger.LogInfo("AWAKE");
            instance = this;
            harmony = Harmony.CreateAndPatchAll(typeof(InventoryGuiPatch));

        }
        internal void OnDestroy()
        {
            Logger.LogInfo("DESTROY");
            Destroy(recycleObject);
            harmony.UnpatchSelf();
            Logger.LogInfo("Unpatched InventoryGui");
        }

        internal GameObject GetOrCreateRecycleTab()
        {
            if (instance.recycleObject != null)
            {
                return instance.recycleObject;

            }
            Logger.LogInfo("CreateRecycleButton");

            recycleObject = Instantiate(InventoryGui.instance.m_tabUpgrade.gameObject, InventoryGui.instance.m_tabUpgrade.gameObject.transform.parent);
            if (recycleObject is null)
            {
                Logger.LogError($"SortButton couldn't be instantiated.");
                return null;
            }
            recycleObject.name = "Recycle";
            recycleObject.GetComponentInChildren<Text>().text = "RECYCLE";
            width = recycleObject.GetComponent<RectTransform>().rect.width;
            craftingPos = new Vector3(recycleObject.transform.localPosition.x + width + 10f, recycleObject.transform.localPosition.y, recycleObject.transform.localPosition.z);
            recycleButton = recycleObject.GetComponent<Button>();
            recycleButton.transform.localPosition = craftingPos;
            recycleButton.interactable = true;
            recycleButton.name = "RecycleButton";
            recycleButton.onClick.RemoveAllListeners();
            recycleButton.onClick.AddListener(SelectRecycleTab);
            recycleObject.SetActive(false);
            return recycleObject;
        }

        internal void SelectRecycleTab()
        {
            Logger.LogDebug("Selected recycle");
            recycleButton.interactable = false;
            InventoryGui.m_instance.m_tabCraft.interactable = true;
            InventoryGui.m_instance.m_tabUpgrade.interactable = true;
            InventoryGui.m_instance.UpdateCraftingPanel(false);

        }

        internal void RebuildRecycleTab()
        {
            GetOrCreateRecycleTab();
        }

    }
}