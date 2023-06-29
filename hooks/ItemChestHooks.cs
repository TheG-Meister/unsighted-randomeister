using dev.gmeister.unsighted.randomeister.core;
using dev.gmeister.unsighted.randomeister.unsighted;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.hooks;

[Harmony]
internal class ItemChestHooks
{

    [HarmonyPatch(typeof(ItemChest), nameof(ItemChest.Start)), HarmonyPostfix]
    public static void ChangeMeteorPingIcon(ItemChest __instance)
    {
        string item = PseudoSingleton<Helpers>.instance.GetChestReward(__instance.gameObject.name, PseudoSingleton<MapManager>.instance.playerRoom.sceneName);
        if (Plugin.Instance.items != null) __instance.dustIcon.gameObject.GetComponent<SpriteRenderer>().sprite = Plugin.Instance.items.GetItemObject(item).itemMenuIcon;
    }

}
