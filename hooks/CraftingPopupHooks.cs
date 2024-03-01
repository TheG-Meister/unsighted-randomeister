using dev.gmeister.unsighted.randomeister.core;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.hooks;

[Harmony]
public class CraftingPopupHooks
{

    [HarmonyPatch(typeof(CraftingPopup), nameof(CraftingPopup.ConfirmCraft)), HarmonyPrefix]
    public static bool BeforeConfirmCraft(CraftingPopup __instance)
    {
        if (Plugin.instance == null || Plugin.instance.currentData == null || Plugin.instance.currentData.canCraftWithoutBlueprint || __instance.usedBlueprint) return true;
        else
        {
            PseudoSingleton<PopupManager>.instance.MessagePopup(__instance.gameObject, "You cannot craft an item without a valid blueprint.", true);
            return false;
        }
    }

}
