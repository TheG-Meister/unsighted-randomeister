﻿using HarmonyLib;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.hooks;

[HarmonyPatch]
public class ItemChestMeteorPingCoroutineHook
{

    public static MethodBase TargetMethod()
    {
        Debug.Log("Finding method");
        Type type = AccessTools.FirstInner(typeof(ItemChest), t => t.Name.Contains("MeteorPingCoroutine"));
        return AccessTools.FirstMethod(type, method => method.Name.Contains("MoveNext"));
    }

    public const string METEOR_DUST = "MeteorDust";
    public const string COLLECTED_METEOR_DUST_ONCE = "CollectedMeteorDustOnce";

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CustomChestPings(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        Debug.Log("Running transpiler");

        List<CodeInstruction> result = new(instructions);
        int conditionalsStart = -1;
        int conditionalsEnd = -1;
        int lastRet = -1;
        string? currentBlock = null;

        Debug.Log(result.Count);

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].opcode == OpCodes.Ret)
            {
                if (currentBlock == METEOR_DUST) conditionalsStart = lastRet + 1;
                else if (currentBlock == COLLECTED_METEOR_DUST_ONCE)
                {
                    conditionalsEnd = i;
                    Debug.Log("Found conditionals");
                    break;
                }

                lastRet = i;
                currentBlock = null;
            }
            string? operand = result[i].operand as string;
            if (operand == METEOR_DUST || operand == COLLECTED_METEOR_DUST_ONCE) currentBlock = operand;
        }

        if (conditionalsStart > -1 && conditionalsEnd > -1 && conditionalsEnd > conditionalsStart)
        {
            
            Label label = generator.DefineLabel();
            CodeInstruction target = result[conditionalsEnd + 1];
            target.labels.Add(label);

            List<CodeInstruction> addedCodes = new()
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ItemChestMeteorPingCoroutineHook), nameof(ItemChestMeteorPingCoroutineHook.ShouldMeteorDustPing), new Type[] { typeof(ItemChest) })),
                new CodeInstruction(OpCodes.Brtrue_S, label),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ret)
            };

            for (int i = conditionalsStart - 1; i < conditionalsEnd + 2; i++) Debug.Log($"{i}:      {result[i]}");
            Debug.Log("Removing codes");
            result[conditionalsStart].opcode = OpCodes.Nop;
            result.RemoveRange(conditionalsStart + 1, conditionalsEnd - conditionalsStart);
            for (int i = conditionalsStart - 1; i < conditionalsStart + 22; i++) Debug.Log($"{i}:      {result[i]}");
            Debug.Log("Adding codes");
            result.InsertRange(conditionalsStart + 1, addedCodes);
            for (int i = conditionalsStart - 1; i < conditionalsStart + 22; i++) Debug.Log($"{i}:      {result[i]}");
        }

        return result;
    }

    public static bool ShouldMeteorDustPing(ItemChest chest)
    {
        Debug.Log("Testing for whether chest should ping");
        return true;
    }

}
