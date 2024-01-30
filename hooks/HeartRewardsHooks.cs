using dev.gmeister.unsighted.randomeister.core;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dev.gmeister.unsighted.randomeister.hooks;

[Harmony]
public class HeartRewardsHooks
{

    public static string ReplaceHeartReward(string original, string npc)
    {
        return original;
    }

    public static readonly Dictionary<Type, string> npcRewards = new()
    {
        { typeof(ArmadilloNPC), "MerchantChip" },
        { typeof(AvatarNPC), "ParryMasterChip" },
        { typeof(Blacksmith2NPC), "PortableBlacksmith" },
        { typeof(BlacksmithNPC), "ScavengerChip" },
        { typeof(ChipNPC), "ChipBoard" },
        { typeof(ElisaNPC), "AutoSyringeChip" },
        { typeof(FishNPC), "GoldenRod" },
        { typeof(GeneralShopNPC), "BigHeartChip" },
        { typeof(GrandmaNPC), "CogChip" },
        { typeof(HarpieNPC), "GlitchChip" },
        { typeof(OlgaNPC), "PortableCrafting" },
        { typeof(ResearcherNPC), "AnimaChip" },
        { typeof(ResearcherNPCGarden), "AnimaChip" },
        { typeof(TobiasNPC), "DogChip" },
        { typeof(VanaNPC), "DoctorsGun" },
        { typeof(WeaponShopNPC), "ElementalBlade" },
    };

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (Type type in npcRewards.Keys) yield return AccessTools.FirstMethod(AccessTools.FirstInner(type, t => t.Name.Contains("MeteorDustGivenCoroutine")), method => method.Name.Contains("MoveNext"));
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TranspileHeartRewards(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        List<CodeInstruction> result = new(instructions);

        int rewardIndex = -1;

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].opcode == OpCodes.Ldstr && result[i].operand.ToString() == npcRewards[original.DeclaringType.DeclaringType])
            {
                rewardIndex = i;
                break;
            }
        }

        if (rewardIndex >= 0)
        {
            List<CodeInstruction> addedIntructions = new()
            {
                new CodeInstruction(OpCodes.Ldstr, original.DeclaringType.DeclaringType.Name),
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(HeartRewardsHooks), nameof(HeartRewardsHooks.ReplaceHeartReward))),
            };

            result.InsertRange(rewardIndex + 1, addedIntructions);
        }

        return result;
    }

}
