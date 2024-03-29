﻿using dev.gmeister.unsighted.randomeister.core;
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

    public static string GetChestID(ChestObject chest)
    {
        return chest.roomName + Constants.CHEST_ID_SEPARATOR + chest.chestName;
    }

    public static void GetChestLocation(string id, out string scene, out string chest)
    {
        string[] split = id.Split(Constants.CHEST_ID_SEPARATOR);

        scene = split[0];
        chest = split[1];
    }

    public static Dictionary<string, ChestObject> GetChestObjectDictionary(ChestList chestList)
    {
        Dictionary<string, ChestObject> result = new();

        foreach (AreaChestList areaChestList in chestList.areas)
            foreach (ChestObject chest in areaChestList.chestList)
            {
                result.Add(Chests.GetChestID(chest), chest);
            }

        return result;
    }

    public static Dictionary<string, string> GetChestItemDictionary(ChestList chestList)
    {
        Dictionary<string, string> result = new();

        foreach (AreaChestList areaChestList in chestList.areas)
            foreach (ChestObject chest in areaChestList.chestList)
            {
                result.Add(Chests.GetChestID(chest), chest.reward);
            }

        return result;
    }

}
