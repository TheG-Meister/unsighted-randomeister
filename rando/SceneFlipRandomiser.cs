using dev.gmeister.unsighted.randomeister.core;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace dev.gmeister.unsighted.randomeister.rando;

[Harmony]
public class SceneFlipRandomiser
{

    public SceneFlipRandomiser()
    { }

    public Dictionary<string, bool> RandomiseSceneFlips(Random random)
    {
        Dictionary<string, bool> output = new();

        // Exclude certain scenes which have unequal numbers of exits sometimes, eg. during prologue or before beating corrupted tomb guardian
        List<string> excludedScenes = new() { "DowntownIndustrialZoneEntrance", "DowntownFinalPuzzle", "DowntownLargeRoom", "ChurchDarkMonsterRoom", "IndustriesVillage" };

        MapManager manager = PseudoSingleton<MapManager>.instance;
        MapPopupController controller = manager.mapPopup.GetComponent<MapPopupController>() ??
            throw new ApplicationException("MapManager.mapPopup did not have a MapPopupController component");
        foreach (GameObject obj in controller.abovegroundRooms.Concat(controller.surfaceRooms).Concat(controller.undergroundRooms))
        {
            MapPopupRoom room = obj.GetComponent<MapPopupRoom>() ??
                throw new ApplicationException("MapPopupController room object did not have a MapPopupRoom component");
            int leftExits = room.leftRooms == null ? 0 : room.leftRooms.Length;
            int rightExits = room.rightRooms == null ? 0 : room.rightRooms.Length;
            output.Add(obj.name, leftExits == rightExits && random.NextDouble() >= 0.5 && !excludedScenes.Contains(obj.name));
        }

        return output;
    }

    [HarmonyPatch(typeof(DungeonRoomController), nameof(DungeonRoomController.OnEnable)), HarmonyPostfix]
    public static void AfterDungeonRoomControllerEnable(DungeonRoomController __instance)
    {
        string scene = __instance.currentRoomDescription.sceneName;
        if (Plugin.instance != null && Plugin.instance.currentData != null && Plugin.instance.currentData.sceneFlips[scene])
        {
            GameObject[] objects = SceneManager.GetSceneByName(scene).GetRootGameObjects();
            foreach (GameObject obj in objects)
            {
                if (obj.GetComponentInChildren<DungeonRoomController>(true) == null && obj.GetComponentInChildren<LevelController>() == null)
                {
                    obj.transform.localScale = new Vector3(-1, 1, 1);
                    obj.transform.position += 2 * Vector3.right * (__instance.roomGeometryParent.transform.position.x - obj.transform.position.x);
                }
            }
        }
    }

}
