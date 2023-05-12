using BepInEx.Configuration;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace dev.gmeister.unsighted.randomeister;

internal class HarmonyHooks
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
    private static void Lists_Start_Post(Lists __instance)
    {
        Plugin.Instance.SetOriginalChestList(__instance);
    }

    [HarmonyPatch(typeof(SplashScreenScene), nameof(SplashScreenScene.Start)), HarmonyPrefix]
    private static bool SplashScreenScene_Start_Pre(SplashScreenScene __instance)
    {
        Plugin.Instance.GetLogger().LogInfo("Attempting to skip intro...");
        Time.timeScale = 1f;
        __instance.CheckBestResolution();
        SceneManager.LoadScene("TitleScreen");
        return false;
    }

    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.LoadGameCoroutine)), HarmonyPrefix]
    public static void LoadedGame()
    {
        Plugin.Instance.GetLogger().LogInfo("Game loaded");
        Plugin.Instance.SetCurrentSlotAndRandomise(PseudoSingleton<GlobalGameData>.instance.loadedSlot, false);
        Plugin.Instance.LogDataStrings(PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings, PseudoSingleton<GlobalGameData>.instance.loadedSlot);
    }

    [HarmonyPatch(typeof(NewGamePopup), nameof(NewGamePopup.NewGameCoroutine)), HarmonyPrefix]
    public static void NewGame()
    {
        Plugin.Instance.GetLogger().LogInfo("New Game");
        Plugin.Instance.SetCurrentSlotAndRandomise(PseudoSingleton<GlobalGameData>.instance.loadedSlot, true);
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
        if (ScreenTransition.playerTransitioningScreens &&
            ScreenTransition.currentDoorName == __instance.gameObject.name &&
            (ScreenTransition.teleportCheat ||
            ScreenTransition.lastSceneName == PseudoSingleton<MapManager>.instance.GetNextRoomName(__instance.myDirection, __instance.triggerID)))
        {
            string location = SceneManager.GetActiveScene().name + " " + __instance.myDirection + " " + __instance.triggerID;

            Plugin.Instance.movementLogger.SetLocation(location, true, false);
        }
    }

}