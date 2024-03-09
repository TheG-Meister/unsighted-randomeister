using dev.gmeister.unsighted.randomeister.core;
using dev.gmeister.unsighted.randomeister.unsighted;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace dev.gmeister.unsighted.randomeister.logger;

[Harmony]
public class MovementLoggerLocationHooks
{

    private static readonly List<string> loggedNonShopNPCs = new() { "BlacksmithNPC", "OlgaNPC", "ElisaNPC", "ClaraNPC", "GrimReaperNPC" };

    [HarmonyPatch(typeof(NewGamePopup), nameof(NewGamePopup.NewGameCoroutine)), HarmonyPrefix]
    public static void ResetLocationOnNewGame()
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.Reset();
    }

    [HarmonyPatch(typeof(LabCutscene1), nameof(LabCutscene1.AfterCutscene)), HarmonyPrefix]
    public static void LogNewGame(LabCutscene1 __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;

        string location = IDs.GetNewGameID();
        logger.SetLocation(__instance.gameObject, location, false, false);
    }

    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.LoadGameCoroutine)), HarmonyPrefix]
    public static void ResetLocationOnLoadGame()
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.Reset();
    }

    [HarmonyPatch(typeof(LevelController), nameof(LevelController.FinishRestartingPlayers)), HarmonyPostfix]
    public static void LogExitCheckpoint(ref IEnumerator __result)
    {
        MovementLogger logger = Plugin.instance.movementLogger;

        PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();

        if (data.lastTerminalData == null || string.IsNullOrEmpty(data.lastTerminalData.areaName))
        {
            TemporaryCheckpointLocation checkpoint = data.lastCheckpoint;
            string location = IDs.GetCheckpointID(checkpoint);
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                logger.SetLocation(location, checkpoint.position, false, false);
            });
        }
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.PlayersEnteredMyTrigger)), HarmonyPostfix]
    public static void LogEnterTerminalTrigger(Terminal __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.SetLocation(__instance.gameObject, IDs.GetTerminalID(), false, false);
    }

    [HarmonyPatch(typeof(InteractionTrigger), nameof(InteractionTrigger.PlayersEnteredMyTrigger)), HarmonyPrefix]
    public static void LogEnterInteractionTrigger(InteractionTrigger __instance)
    {
        if (!ScreenTransition.playerTransitioningScreens)
        {
            MovementLogger logger = Plugin.instance.movementLogger;

            ItemChest chest = __instance.messageReciever.GetComponent<ItemChest>();
            if (chest != null) logger.SetLocation(chest.gameObject, IDs.GetChestID(chest), false, false);

            StoreNPC shop = __instance.messageReciever.GetComponent<StoreNPC>();
            NPCCharacter npc = __instance.messageReciever.GetComponent<NPCCharacter>();
            if (shop != null && shop.npcName != "JoanaNPC") logger.SetLocation(shop.gameObject, IDs.GetNPCID(shop), false, false);
            else if (npc != null && loggedNonShopNPCs.Contains(npc.npcName)) logger.SetLocation(npc.gameObject, IDs.GetNPCID(npc), false, false);

            KeyCard keyCard = __instance.messageReciever.GetComponent<KeyCard>();
            if (keyCard != null) logger.SetLocation(keyCard.gameObject, IDs.GetKeyCardID(), false, false);

            CraftingTable table = __instance.messageReciever.GetComponent<CraftingTable>();
            if (table != null) logger.SetLocation(table.gameObject, IDs.GetCraftingTableID(table), false, false);

            EagleRideTrigger eagle = __instance.messageReciever.GetComponent<EagleRideTrigger>();
            if (eagle != null) logger.SetLocation(eagle.gameObject, IDs.GetEagleRideTriggerID(eagle), false, false);
        }
    }

    [HarmonyPatch(typeof(AbilityCollectable), nameof(AbilityCollectable.ItemCollected)), HarmonyPrefix]
    public static void LogCollectAbility(AbilityCollectable __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetAbilityCollectableID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, false);
    }

    [HarmonyPatch(typeof(ScreenTransition), nameof(ScreenTransition.PlayerScreenTransition)), HarmonyPrefix]
    public static void LogEnterScreenTransition(ScreenTransition __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetScreenTransitionID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(ScreenTransition), nameof(ScreenTransition.EndPlayerScreenTransition)), HarmonyPostfix]
    public static void LogExitScreenTransition(ScreenTransition __instance, ref IEnumerator __result)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        if (ScreenTransition.playerTransitioningScreens &&
            ScreenTransition.currentDoorName == __instance.gameObject.name &&
            (ScreenTransition.teleportCheat ||
            ScreenTransition.lastSceneName == PseudoSingleton<MapManager>.instance.GetNextRoomName(__instance.myDirection, __instance.triggerID)))
        {
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                string location = IDs.GetScreenTransitionID(__instance);
                logger.SetLocation(__instance.gameObject, location, false, false);
            });
        }
    }

    /*
    [HarmonyPatch(typeof(LevelController), nameof(LevelController.FinishRestartingPlayers)), HarmonyPostfix]
    public static void LogTerminalSpawn(Terminal __instance, ref IEnumerator __result)
    {
        string scene = SceneManager.GetActiveScene().name;
        __result = AddLocationChangeToEnumerator(__result, MovementLogger.GetTerminalName(scene), scene, __instance.transform.position, false);
    }
    */

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.TeleportToLastTerminal)), HarmonyPostfix]
    public static void SetChangingSceneOnTeleport()
    {
        Plugin.instance.movementLogger.SetChangingScene(true);
    }

    [HarmonyPatch(typeof(HoleTeleporter), nameof(HoleTeleporter.FallIntoHole)), HarmonyPrefix]
    public static void LogEnterHole(HoleTeleporter __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetHoleID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(HoleTeleporter), nameof(HoleTeleporter.Start)), HarmonyPostfix]
    public static void LogExitHole(HoleTeleporter __instance, ref IEnumerator __result)
    {
        if (__instance.GetComponent<ElevatedGround>() == null && HoleTeleporter.fallingDownOnHole && HoleTeleporter.lastHoleID == __instance.holeIndex)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetHoleID(__instance);
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                logger.SetLocation(location, new Vector3(__instance.transform.position.x, __instance.transform.position.y, 16f), false, false);
            });
        }
    }

    [HarmonyPatch(typeof(Elevator), nameof(Elevator.PlayerScreenTransition)), HarmonyPrefix]
    public static void LogEnterElevator(Elevator __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetElevatorID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(Elevator), nameof(Elevator.Start)), HarmonyPostfix]
    public static void LogExitElevator(Elevator __instance, ref IEnumerator __result)
    {
        if (__instance.elevatorID == Elevator.lastElevatorID && Elevator.ridingElevator)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetElevatorID(__instance);
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                logger.SetLocation(__instance.gameObject, location, false, false);
            });
        }
    }

    [HarmonyPatch(typeof(CrystalAppear), nameof(CrystalAppear.CollectionCoroutine)), HarmonyPrefix]
    public static void LogEnterCrystal(CrystalAppear __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetCrystalAppearID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(CrystalTeleportExit), nameof(CrystalTeleportExit.Start)), HarmonyPostfix]
    public static void LogExitCrystal(CrystalTeleportExit __instance)
    {
        if (CrystalTeleportExit.usingCrystalTeleport)
        {
            string location = IDs.GetCrystalTeleportExitID(__instance);
            MovementLogger logger = Plugin.instance.movementLogger;
            logger.SetLocation(__instance.gameObject, location, false, false);
        }
    }

    [HarmonyPatch(typeof(SceneChangeLadder), nameof(SceneChangeLadder.LadderCoroutine)), HarmonyPrefix]
    public static void LogEnterLadder(SceneChangeLadder __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetLadderID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(SceneChangeLadder), nameof(SceneChangeLadder.Start)), HarmonyPrefix]
    public static void LogExitLadder(SceneChangeLadder __instance)
    {
        if (SceneChangeLadder.currentLadder == __instance.name)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetLadderID(__instance);
            logger.SetLocation(__instance.gameObject, location, false, false);
        }
    }

    [HarmonyPatch(typeof(CraterTowerElevator), nameof(CraterTowerElevator.ElevatorCoroutine)), HarmonyPrefix]
    public static void LogEnterTowerElevator(CraterTowerElevator __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetTowerElevatorID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(CraterTowerElevator), nameof(CraterTowerElevator.Start)), HarmonyPostfix]
    public static void LogExitTowerElevator(CraterTowerElevator __instance, ref IEnumerator __result)
    {
        if (CraterTowerElevator.currentElevator == __instance.name)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetTowerElevatorID(__instance);
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                logger.SetLocation(__instance.gameObject, location, false, false);
            });
        }
    }

    /*
    [HarmonyPatch(typeof(EagleRideTrigger), nameof(EagleRideTrigger.CutsceneCoroutine)), HarmonyPrefix]
    public static void LogEnterEagleFlight(EagleRideTrigger __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;
        string location = IDs.GetEagleRideTriggerID(__instance);
        logger.SetLocation(scene, location, __instance.transform.position, false, true);
    }
    */

    [HarmonyPatch(typeof(Eagle), nameof(Eagle.Start)), HarmonyPostfix]
    public static void LogExitEagleFlight(Eagle __instance, ref IEnumerator __result)
    {
        if (__instance.firstEagle)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetEagleBossEntranceID(__instance);
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                logger.SetLocation(__instance.gameObject, location, false, false);
            });
        }
    }

    [HarmonyPatch(typeof(EaglesController), nameof(EaglesController.Death)), HarmonyPrefix]
    public static void LogEnterEagleBossDeath(EaglesController __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetEagleBossExitID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(AfterEagleBossCutscene), nameof(AfterEagleBossCutscene.Start)), HarmonyPrefix]
    public static void LogSpawnInCrashSite(AfterEagleBossCutscene __instance)
    {
        GlobalGameData data = PseudoSingleton<GlobalGameData>.instance;
        if (!data.currentData.playerDataSlots[data.loadedSlot].dataStrings.Contains("AfterEagleBossCutscene"))
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetCrashSiteEntranceID(__instance);
            logger.SetLocation(__instance.gameObject, location, false, false);
        }
    }

    [HarmonyPatch(typeof(EagleBossCrystal), nameof(EagleBossCrystal.CollectionCoroutine)), HarmonyPrefix]
    public static void LogEnterEagleCrystal(EagleBossCrystal __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetEagleCrystalID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(EagleBossCrystal), nameof(EagleBossCrystal.AfterCrystalCoroutine)), HarmonyPostfix]
    public static void LogExitEagleCrystal(EagleBossCrystal __instance, ref IEnumerator __result)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetEagleCrystalID(__instance);
        __result = logger.AppendCodeToEnumerator(__result, () =>
        {
            logger.SetLocation(__instance.gameObject, location, false, false);
        });
    }

    public static int GetFlashbackRoomLayout(FlashbackRoomController controller)
    {
        if (controller.layout1.activeSelf) return 1;
        else if (controller.layout2.activeSelf) return 2;
        else if (controller.layout3.activeSelf) return 3;
        else if (controller.layout4.activeSelf) return 4;
        else if (controller.layout5.activeSelf) return 5;
        return -1;
    }

    [HarmonyPatch(typeof(FlashbackRoomController), nameof(FlashbackRoomController.Start)), HarmonyPostfix]
    public static void LogEnterFlashbackRoom(FlashbackRoomController __instance, ref IEnumerator __result)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetFlashbackRoomEntranceID(GetFlashbackRoomLayout(__instance));
        __result = logger.AppendCodeToEnumerator(__result, () =>
        {
            logger.SetLocation(__instance.gameObject, location, false, false);
        });
    }

    [HarmonyPatch(typeof(FlashbackRoomController), nameof(FlashbackRoomController.ExitCutscene)), HarmonyPrefix]
    public static void LogExitFlashbackRoom(FlashbackRoomController __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;

        string exitLoc = IDs.GetFlashbackRoomExitID(GetFlashbackRoomLayout(__instance));
        logger.SetLocation(__instance.gameObject, exitLoc, false, true);

        string itemLoc = IDs.GetMeteorCrystalItemLocID(FlashbackRoomController.myBossName);
        logger.SetLocation(__instance.gameObject, itemLoc, false, true);
    }

    [HarmonyPatch(typeof(DarkMonsterCraterCutscene), nameof(DarkMonsterCraterCutscene.SkipCutsceneInput)), HarmonyPrefix]
    public static void LogWinDarkMonsterFight(DarkMonsterCraterCutscene __instance)
    {
        MovementLoggerLocationHooks.LogExitDarkMonsterFight(__instance);

        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetVanaFlamebladeID();
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(DarkMonsterCraterCutscene), nameof(DarkMonsterCraterCutscene.DeathCutsceneCoroutine)), HarmonyPrefix]
    public static void LogExitDarkMonsterFight(DarkMonsterCraterCutscene __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetDarkMonsterFightExitID();
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(BlacksmithCutscene), nameof(BlacksmithCutscene.Start)), HarmonyPrefix]
    public static void LogEnterBlacksmithCutscene(BlacksmithCutscene __instance)
    {
        if (!PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings.Contains("BlacksmithCutscene"))
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetBlacksmithCutsceneID();
            logger.SetLocation(__instance.gameObject, location, false, false);
        }
    }

}
