﻿using BepInEx.Configuration;
using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace dev.gmeister.unsighted.randomeister.core;

[Harmony]
internal class Hooks
{

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

    [HarmonyPatch(typeof(Lists), nameof(Lists.Start)), HarmonyPostfix]
    private static void AfterListsStart(Lists __instance)
    {
        Plugin.Instance.SetOriginalChestList(__instance);
    }

    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.LoadGameCoroutine)), HarmonyPrefix]
    public static void BeforeGameLoaded()
    {
        Plugin.Instance.GetLogger().LogInfo("Game loaded");
        Plugin.Instance.SetCurrentSlotAndRandomise(PseudoSingleton<GlobalGameData>.instance.loadedSlot, false);
    }

    [HarmonyPatch(typeof(NewGamePopup), nameof(NewGamePopup.NewGameCoroutine)), HarmonyPrefix]
    public static void BeforeNewGame()
    {
        Plugin.Instance.GetLogger().LogInfo("New Game");
        Plugin.Instance.SetCurrentSlotAndRandomise(PseudoSingleton<GlobalGameData>.instance.loadedSlot, true);
    }

    [HarmonyPatch(typeof(RotatingSpiderBossRoom), nameof(RotatingSpiderBossRoom.Start)), HarmonyPrefix]
    public static bool BeforeQueenSpinarachBossRoomLoad(RotatingSpiderBossRoom __instance)
    {
        if (Plugin.Instance.currentData == null || !Plugin.Instance.currentData.removeFragileOnJumpBootsChest) return true;

        List<string> dataStrings = PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings;
        bool bossDefeated = dataStrings.Contains("RotatingSpiderDefeated");
        bool chestOpened = dataStrings.Contains("ChestJumpBootsChestDowntownJumpRoom");

        if (chestOpened)
        {
            __instance.exitPlatform.gameObject.SetActive(true);
            __instance.rotatingSpider.gameObject.SetActive(false);
            __instance.rotatingSpiderPlatformCollider.gameObject.SetActive(false);
            __instance.multiButtonPuzzle.gameObject.SetActive(false);
            __instance.jumpBootsChest.transform.parent = null;
            __instance.jumpBootsChest.gameObject.SetActive(true);
            return false;
        }
        else if (!bossDefeated)
        {
            __instance.rotatingSpider.gameObject.SetActive(true);
            return false;
        }
        else return true;
    }

    [HarmonyPatch(typeof(ItemChest), nameof(ItemChest.Start)), HarmonyPrefix]
    public static void BeforeItemChestStart(ItemChest __instance)
    {
        if (Plugin.Instance.currentData != null && Plugin.Instance.currentData.removeFragileOnJumpBootsChest && SceneManager.GetActiveScene().name == "DowntownJumpRoom" && __instance.gameObject.name == "JumpBootsChest") __instance.fragileItem = false;
    }

    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.EraseConfirm)), HarmonyPostfix]
    public static void AfterFileErase(SaveSlotButton __instance)
    {
        Plugin.Instance.OnFileErased(__instance);
    }

    [HarmonyPatch(typeof(SaveSlotPopup), nameof(SaveSlotPopup.CopyFile)), HarmonyPrefix]
    public static void BeforeFileCopy(SaveSlotPopup __instance, int originalSlotNumber)
    {
        Plugin.Instance.OnFileCopied(__instance, originalSlotNumber);
    }

    [HarmonyPatch(typeof(ScreenTransition), nameof(ScreenTransition.PlayerScreenTransition)), HarmonyPrefix]
    public static void OnScreenTransition(ScreenTransition __instance)
    {
        string location = SceneManager.GetActiveScene().name + " " + __instance.myDirection + " " + __instance.triggerID;

        Plugin.Instance.movementLogger.SetLocation(location, false, false);
    }

    [HarmonyPatch(typeof(ScreenTransition), nameof(ScreenTransition.Start)), HarmonyPrefix]
    public static void OnScreenTransitionObjectStart(ScreenTransition __instance)
    {
        /*if (ScreenTransition.playerTransitioningScreens &&
            ScreenTransition.currentDoorName == __instance.gameObject.name &&
            (ScreenTransition.teleportCheat ||
            ScreenTransition.lastSceneName == PseudoSingleton<MapManager>.instance.GetNextRoomName(__instance.myDirection, __instance.triggerID)))
        {
            string location = SceneManager.GetActiveScene().name + " " + __instance.myDirection + " " + __instance.triggerID;

            Plugin.Instance.movementLogger.SetLocation(location, true, false);
        }*/
    }

}