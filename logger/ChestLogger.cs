﻿using dev.gmeister.unsighted.randomeister.core;
using dev.gmeister.unsighted.randomeister.unsighted;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace dev.gmeister.unsighted.randomeister.logger;

[Harmony]
public class ChestLogger
{
    private string dir;

    public ChestLogger(string dir)
    {
        this.dir = dir;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    public void LogChest(string scene, string chest, List<string> items)
    {
        List<Ability> abilities = items.FindAll(item => AbilityTools.itemAbilities.ContainsKey(item)).SelectMany(item => AbilityTools.itemAbilities[item]).Distinct().ToList();
        abilities.Sort();

        if (!abilities.Any()) return;

        string file = "";
        for (int i = 0; i < abilities.Count; i++)
        {
            file += abilities[i].ToString().ToLower();
            if (i < abilities.Count - 1) file += "-";
        }
        file += ".tsv";

        string path = Path.Combine(dir, file);

        string line = scene + Constants.CHEST_ID_SEPARATOR + chest;
        File.AppendAllLines(path, new List<string>() { line });
    }

    [HarmonyPatch(typeof(ItemChest), nameof(ItemChest.ChestOpenCoroutine)), HarmonyPrefix]
    public static void BeforeChestOpen(ItemChest __instance)
    {
        PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();
        List<string> items = data.currentWeapons.Select(weapon => weapon.equipmentName).ToList();
        foreach (PlayerItemData item in data.playerItems)
        {
            if (item.itemName == "JumpBoots") items.Add("JumpBoots");
            if (item.itemName == "Spinner") items.Add("Spinner");
        }

        Plugin.Instance.chestLogger.LogChest(SceneManager.GetActiveScene().name, __instance.gameObject.name, items);
    }

}
