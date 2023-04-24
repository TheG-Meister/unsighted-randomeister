using System.Runtime.CompilerServices;
using UnityEngine;

namespace ItemRandomizer;

internal class HarmonyHooks
{
    private static List<ItemObject>? originalItems;

    private static readonly int seed = 0;
    private static ChestList? originalChestList;
    private static ChestList? randomChestList;

    [HarmonyPatch(typeof(Helpers), nameof(Helpers.GetChestReward)), HarmonyPostfix]
    private static void Helpers_GetChestReward_Post(ref string __result)
    {
        /*Debug.Log($"Helpers.GetChestReward() postfix, called by {BepinexEntryPoint.GUID}");
        //Any weapon and shuriken, along with single hookshot, double hookshot and jump boots can be used to escape the chest room in the lab

        if (originalItems == null)
        {
            Lists lists = PseudoSingleton<Lists>.instance;

            originalItems = new List<ItemObject> ();
            originalItems.AddRange(lists.itemDatabase.itemList);
            originalItems.AddRange(lists.weaponDatabase.weaponList);
            originalItems.AddRange(lists.chipDatabase.chipList);
            originalItems.AddRange(lists.cogsDatabase.cogList);
            originalItems.AddRange(lists.armorDatabase.armorList);
        }

        string newResult = originalItems[UnityEngine.Random.Range(0, originalItems.Count)].itemName;

        Debug.Log(__result + " replaced with " + newResult);

        __result = newResult;*/
    }

    private static ChestObject CloneChest(ChestObject other)
    {
        ChestObject chest = new ChestObject();
        chest.reward = other.reward;
        chest.chestName = other.chestName;
        chest.roomName = other.roomName;
        chest.abilitiesNeeded = (Abilities[]) other.abilitiesNeeded;
        chest.dontCountToTotal = other.dontCountToTotal;

        return chest;
    }

    private static AreaChestList CloneAreaChestList(AreaChestList other)
    {
        AreaChestList areaChestList = new AreaChestList(other.areaName);
        foreach (ChestObject chest in other.chestList) areaChestList.chestList.Add(CloneChest(chest));

        return areaChestList;
    }

    private static ChestList CloneChestList(ChestList other)
    {
        ChestList chestList = new ChestList();
        chestList.areas = new List<AreaChestList>();
        chestList.chestList = new List<ChestObject>();

        foreach (AreaChestList areaChestList in other.areas)
        {
            AreaChestList newAreaChestList = CloneAreaChestList(areaChestList);
            chestList.areas.Add(newAreaChestList);
            chestList.chestList.AddRange(newAreaChestList.chestList);
        }

        return chestList;
    }

    private static void ReplaceChestItems(ChestList chestList, List<string> items)
    {
        int i = 0;
        foreach (AreaChestList areaChestList in chestList.areas) foreach (ChestObject chestObject in areaChestList.chestList)
        {
            chestObject.reward = items[i++];
        }
    }

    [HarmonyPatch(typeof(Lists), nameof(Lists.Start)), HarmonyPostfix]
    private static void Lists_Start_Post(Lists __instance)
    {
        if (originalChestList == null) originalChestList = __instance.chestList;

        List<string> items = originalChestList.areas.SelectMany(areaChestList => areaChestList.chestList).Select(chest => chest.reward).ToList();
        System.Random random = new System.Random(seed);
        items = items.OrderBy(item => random.NextDouble()).ToList();
        foreach (string item in items) { Debug.Log(item); }

        randomChestList = originalChestList;
        ReplaceChestItems(randomChestList, items);

        __instance.chestList = randomChestList;
        __instance.AnalyzeChestList();
    }
}