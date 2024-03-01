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
        if (Plugin.instance != null && Plugin.instance.currentData != null)
        {
            Dictionary<string, DropController> dropTables = Plugin.instance.currentData.enemyDropTables;
            if (dropTables != null && dropTables.ContainsKey(__instance.GetType().Name))
            {
                __instance.dropController = dropTables[__instance.GetType().Name];
            }
        }
    }

}
