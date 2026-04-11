using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace dev.gmeister.unsighted.randomeister.rando;

public class SceneFlipRandomiser
{

    public SceneFlipRandomiser()
    { }

    public Dictionary<string, bool> RandomiseSceneFlips(Random random, Dictionary<string, bool> original)
    {
        Dictionary<string, bool> output = new();

        // Exclude certain scenes with equal numbers of left and right exits which have unequal numbers of exits in the prologue
        List<string> excludedScenes = new() { "DowntownIndustrialZoneEntrance", "DowntownFinalPuzzle", "DowntownLargeRoom", "ChurchDarkMonsterRoom" };

        MapManager manager = PseudoSingleton<MapManager>.instance;
        MapPopupController controller = manager.mapPopup.GetComponent<MapPopupController>() ??
            throw new ApplicationException("MapManager.mapPopup did not have a MapPopupController component");
        foreach (GameObject obj in controller.abovegroundRooms.Concat(controller.surfaceRooms).Concat(controller.undergroundRooms))
        {
            MapPopupRoom room = obj.GetComponent<MapPopupRoom>() ??
                throw new ApplicationException("MapPopupController room object did not have a MapPopupRoom component");
            int leftExits = room.leftRooms == null ? 0 : room.leftRooms.Length;
            int rightExits = room.rightRooms == null ? 0 : room.rightRooms.Length;
            output.Add(obj.name, original[obj.name] ^ (leftExits == rightExits && random.NextDouble() >= 0.5 && !excludedScenes.Contains(obj.name)));
        }

        return output;
    }

}
