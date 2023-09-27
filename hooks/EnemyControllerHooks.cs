using dev.gmeister.unsighted.randomeister.core;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.hooks;

[Harmony]
public class EnemyControllerHooks
{

    [HarmonyPatch(typeof(EnemyController), nameof(EnemyController.Start)), HarmonyPostfix]
    public static void ChangeDropController(EnemyController __instance)
    {
        if (Plugin.Instance != null && Plugin.Instance.currentData != null)
        {
            Dictionary<string, DropController> dropTables = Plugin.Instance.currentData.enemyDropTables;
            if (!dropTables.ContainsKey(__instance.GetType().Name))
            {
                List<string> materials = PseudoSingleton<Lists>.instance.itemDatabase.itemList.Where(item => item.itemType == ItemType.material).Select(item => item.itemName).ToList();

                List<string> controllerList = new List<string>() { "", materials[new System.Random().Next(materials.Count)] };
                dropTables.Add(__instance.GetType().Name, new DropController() { enemyNameID = __instance.GetType().Name, itemList = controllerList } );
            }
            __instance.dropController = dropTables[__instance.GetType().Name];
        }
    }

}
