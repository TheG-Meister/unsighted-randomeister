﻿using static dev.gmeister.unsighted.randomeister.core.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dev.gmeister.unsighted.randomeister.core;
using UnityEngine.SceneManagement;
using static UnityEngine.UI.Selectable;

namespace dev.gmeister.unsighted.randomeister.unsighted;

public class IDs
{

    public static string GetID(params object[] ids) => string.Join(ID_SEPARATOR.ToString(), ids);
    public static string AddSceneToID(string scene, string id) => string.Join(ID_SEPARATOR.ToString(), scene, id);
    public static string AddSceneToId(string id) => string.Join(ID_SEPARATOR.ToString(), SceneManager.GetActiveScene().name, id);
    public static string GetNewGameID() => "New Game";
    public static string GetScreenTransitionID(ScreenTransition transition) => string.Join(ID_SEPARATOR.ToString(), transition.GetType(), Strings.SnakeToPascalCase(transition.myDirection.ToString()), transition.triggerID);
    public static string GetTerminalID() => string.Join(ID_SEPARATOR.ToString(), typeof(Terminal));
    public static string GetHoleID(HoleTeleporter hole) => string.Join(ID_SEPARATOR.ToString(), hole.GetType(), hole.holeIndex);
    public static string GetElevatorID(Elevator elevator) => string.Join(ID_SEPARATOR.ToString(), elevator.GetType(), elevator.elevatorID);
    public static string GetLadderID(SceneChangeLadder ladder) => string.Join(ID_SEPARATOR.ToString(), ladder.GetType(), ladder.name);
    public static string GetTowerElevatorID(CraterTowerElevator elevator) => string.Join(ID_SEPARATOR.ToString(), elevator.GetType(), elevator.name);
    public static string GetMetalScrapOreStateID(MetalScrapOre ore, bool present) => string.Join(ID_SEPARATOR.ToString(), ore.name, ore.transform.GetSiblingIndex(), present ? "Present" : "Absent");
    public static string GetMetalScrapOreID(MetalScrapOre ore) => string.Join(ID_SEPARATOR.ToString(), ore.name, ore.transform.GetSiblingIndex());
    public static string GetCheckpointID(TemporaryCheckpointLocation checkpoint) => IDs.GetID("Checkpoint", checkpoint.position.x, checkpoint.position.y);
    public static string GetEagleRideTriggerID(EagleRideTrigger trigger) => IDs.GetID(trigger.GetType());
    public static string GetEagleBossEntranceID(Eagle eagle) => IDs.GetID(eagle.GetType());
    public static string GetEagleBossExitID(EaglesController controller) => IDs.GetID(controller.GetType(), "Exit");
    public static string GetCrashSiteEntranceID(AfterEagleBossCutscene cutscene) => IDs.GetID(cutscene.GetType());
    public static string GetEagleCrystalID(EagleBossCrystal crystal) => IDs.GetID(crystal.GetType());
    public static string GetFlashbackRoomEntranceID(int layout) => IDs.GetID("Flashback", layout, "Entrance");
    public static string GetFlashbackRoomExitID(int layout) => IDs.GetID("Flashback", layout, "Exit");
    public static string GetMeteorCrystalItemLocID(string boss) => IDs.GetID(boss, "MeteorShard");
    public static string GetChestID(ItemChest chest) => IDs.GetID(chest.name);
    public static string GetNPCID(NPCCharacter npc) => IDs.GetID(npc.npcName);
    public static string GetKeyCardID() => IDs.GetID(typeof(KeyCard));
    public static string GetAbilityCollectableID(AbilityCollectable ability) => IDs.GetID(ability.name);
    public static string GetDarkMonsterFightExitID() => IDs.GetID(typeof(DarkMonsterCraterCutscene));
    public static string GetVanaFlamebladeID() => IDs.GetID("Vana", "Flameblade");
    public static string GetBlacksmithCutsceneID() => IDs.GetID(typeof(BlacksmithCutscene));
    public static string GetCraftingTableID(CraftingTable table) => IDs.GetID(table.name);
    public static string GetCrystalAppearID(CrystalAppear crystal) => IDs.GetID(typeof(CrystalAppear), crystal.myBossName);
    public static string GetCrystalTeleportExitID(CrystalTeleportExit exit) => IDs.GetID(typeof(CrystalTeleportExit));
    public static string GetRockID(RockBlock rock) => IDs.GetID(rock.isSafeDoor ? "SafeDoor" : "RockBlock", rock.transform.parent.name);
    public static string GetRockStateID(RockBlock rock, bool present) => IDs.GetID(IDs.GetRockID(rock), present ? "Present" : "Broken");
    public static string GetKeyDoorID(KeyDoor keyDoor)
    {
        keyDoor.GetKeyDoorName();
        return IDs.GetID(keyDoor.GetType().Name, keyDoor.name, Strings.SnakeToPascalCase(keyDoor.myDoorDirection.ToString()));
    }

    public static string GetKeyDoorStateID(KeyDoor keyDoor, bool present) => IDs.GetID(IDs.GetKeyDoorID(keyDoor), present ? "Shut" : "Open");
    public static string GetEnergyPlatformID(EnergyPlatform platform) => IDs.GetID(platform.name);
    public static string GetEnergyPlatformStateID(EnergyPlatform platform, bool active) => IDs.GetID(IDs.GetEnergyPlatformID(platform), active ? "Active" : "Inactive");
    public static string GetItemBarrierID(ItemBarrier barrier) => IDs.GetID(barrier.name);
    public static string GetItemBarrierStateID(ItemBarrier barrier, bool present) => IDs.GetID(IDs.GetItemBarrierID(barrier), present ? "Present" : "Gone");
    public static string GetSaveObjectLocationID(SaveObjectLocation location) => IDs.GetID(location.name);
    public static string GetSaveObjectLocationStateID(SaveObjectLocation location, bool saved) => IDs.GetID(IDs.GetSaveObjectLocationID(location), saved ? "Saved" : "NotSaved");
    public static string GetPrologueStateID(bool prologue) => IDs.GetID(prologue ? "Prologue" : "PrologueOver");
    public static string GetMuseumLightStateID(bool on) => IDs.GetID("MuseumLight", on ? "On" : "Off");
    public static string GetHighwaysPoleStateID(bool blueDown) => IDs.GetID("Highways", blueDown ? "BluePolesDown" : "RedPolesDown");
    public static string GetHighwaysPoleSwitchID(EyeSpinButton button) => IDs.GetID("HighwaysPoleSwitch", button.name);
    public static string GetMuseumLightSwitchID(EyeSpinButton button) => IDs.GetID("MuseumLightSwitch", button.name);
    public static string GetInputID(InputType inputType) => IDs.GetID(nameof(InputType), inputType.ToString());
    public static string GetDifficultyID(Difficulty difficulty) => IDs.GetID("Difficulty", difficulty.ToString());
    public static string GetNewGameStyleID(GameMode mode) => IDs.GetID("NewGameStyle", mode.ToString());

}
