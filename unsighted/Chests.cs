using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.unsighted;

public class Chests
{

    public static ChestObject CloneChest(ChestObject other)
    {
        ChestObject chest = new()
        {
            reward = other.reward,
            chestName = other.chestName,
            roomName = other.roomName,
            abilitiesNeeded = other.abilitiesNeeded,
            dontCountToTotal = other.dontCountToTotal
        };

        return chest;
    }

    public static AreaChestList CloneAreaChestList(AreaChestList other)
    {
        AreaChestList areaChestList = new(other.areaName);
        foreach (ChestObject chest in other.chestList) areaChestList.chestList.Add(CloneChest(chest));

        return areaChestList;
    }

    public static ChestList CloneChestList(ChestList other)
    {
        ChestList chestList = ScriptableObject.CreateInstance<ChestList>();
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

}
