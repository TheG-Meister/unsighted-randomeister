﻿using dev.gmeister.unsighted.randomeister.core;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.hooks;

[Harmony]
public class MetalScrapOreHooks
{

    [HarmonyPatch(typeof(MetalScrapOre), nameof(MetalScrapOre.Destroyed)), HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> MetalScrapOreDestroyedTranspiler(IEnumerable<CodeInstruction> instructions)
    {

        List<CodeInstruction> result = new(instructions);
        int firstStloc_1 = -1;

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].opcode == OpCodes.Stloc_1)
            {
                firstStloc_1 = i;
                break;
            }
        }

        if (firstStloc_1 > -1)
        {
            List<CodeInstruction> addedCodes = new()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MetalScrapOreHooks), nameof(MetalScrapOreHooks.ReplaceMaterialCrystalItem), new Type[] { typeof(MetalScrapOre), typeof(string) })),
                new CodeInstruction(OpCodes.Stloc_1),
            };

            result.InsertRange(firstStloc_1 + 1, addedCodes);
        }

        return result;
    }

    public static string ReplaceMaterialCrystalItem(MetalScrapOre crystal, string item)
    {
        if (Plugin.instance == null || Plugin.instance.currentData == null) return item;
        else
        {
            Dictionary<string, string> crystalItems = Plugin.instance.currentData.crystalItems;
            string code = crystal.GetOreCode();
            string area = PseudoSingleton<MapManager>.instance.areaName;

            if (crystalItems != null)
            {
                if (Plugin.instance.currentData.crystalItemsPerArea)
                {
                    if (crystalItems.ContainsKey(area)) return crystalItems[area];
                }
                else
                {
                    if (crystalItems.ContainsKey(code)) return crystalItems[code];
                }
            }
            
            return item;
        }
    }

}
