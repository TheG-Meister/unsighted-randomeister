using dev.gmeister.unsighted.randomeister.core;
using dev.gmeister.unsighted.randomeister.unsighted;
using static dev.gmeister.unsighted.randomeister.unsighted.PlayerAction;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.logger;

[Harmony]
public class MovementLoggerStateHooks
{

    [HarmonyPatch(typeof(MetalScrapOre), nameof(MetalScrapOre.Start)), HarmonyPostfix]
    public static void LogOreStart(MetalScrapOre __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.LogObject(__instance.gameObject, IDs.GetMetalScrapOreID(__instance));
    }

    [HarmonyPatch(typeof(MetalScrapOre), nameof(MetalScrapOre.Destroyed)), HarmonyPostfix]
    public static void LogOreDestroy(MetalScrapOre __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.SetLocation(__instance.gameObject, IDs.GetMetalScrapOreID(__instance), true, false);
    }

    [HarmonyPatch(typeof(RockBlock), nameof(RockBlock.Start)), HarmonyPrefix]
    public static void LogRockStart(RockBlock __instance)
    {
        if (!__instance.reset)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            logger.LogObject(__instance.transform.parent.gameObject, IDs.GetRockID(__instance));

            PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();
            if (data.dataStrings.Contains(__instance.GetDataString())) logger.AddStates(__instance.gameObject, IDs.GetRockStateID(__instance, false));
        }
    }

    [HarmonyPatch(typeof(BulletRaycaster), nameof(BulletRaycaster.Update)), HarmonyPrefix]
    public static void LogMissileRockBreak(BulletRaycaster __instance, bool ___collided, ref RaycastHit2D ___raycastHit)
    {
        if (!___collided && __instance.mechaMissile)
        {
            __instance.CastRaycast();

            if (___raycastHit.collider != null && ___raycastHit.transform.childCount >= 3)
            {
                RockBlock rock = ___raycastHit.transform.GetChild(2).GetComponent<RockBlock>();
                if (rock != null)
                {
                    MovementLogger logger = Plugin.instance.movementLogger;
                    if (rock.isSafeDoor) logger.AddActions(logger.GetPlayerPos(), BreakSafeWithMissile);
                    else logger.AddActions(logger.GetPlayerPos(), BreakRockWithMissile);
                }
            }
        }
    }

    [HarmonyPatch(typeof(RockBlock), nameof(RockBlock.DestroyBlock)), HarmonyPrefix]
    public static void LogRockBreak(RockBlock __instance, bool mechaMissile, bool ___destroyed)
    {
        if (!PlayerInfo.cutscene && !___destroyed && (mechaMissile || !__instance.isSafeDoor))
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            List<PlayerInfo> players = PseudoSingleton<PlayersManager>.instance.players;
            foreach (PlayerInfo player in players)
            {
                if (player.myCharacter.meeleAttacking && player.lastHoldWeapon.Contains("Meteor"))
                {
                    if (__instance.isSafeDoor) logger.AddActions(player.myCharacter, BreakSafeWithMeteorWeapon);
                    else logger.AddActions(player.myCharacter, BreakRockWithMeteorWeapon);
                }
            }

            if (!__instance.reset)
            {
                logger.SetLocation(__instance.gameObject, IDs.GetRockID(__instance), true, false);
                logger.AddStates(__instance.gameObject, IDs.GetRockStateID(__instance, false));
            }
        }
    }

    [HarmonyPatch(typeof(KeyDoor), nameof(KeyDoor.Start)), HarmonyPrefix]
    public static void LogKeyDoorStart(KeyDoor __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.LogObject(__instance.gameObject, IDs.GetKeyDoorID(__instance));

        if (__instance.KeyDoorRegistred())
        {
            logger.AddStates(__instance.gameObject, IDs.GetKeyDoorStateID(__instance, false));
        }
    }

    [HarmonyPatch(typeof(KeyDoor), nameof(KeyDoor.RegisterKeyDoor)), HarmonyPrefix]
    public static void LogKeyDoorRemoved(KeyDoor __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.AddStates(__instance.gameObject, IDs.GetKeyDoorStateID(__instance, false));
        logger.SetLocation(__instance.gameObject, IDs.GetKeyDoorID(__instance), false, false);
    }

    [HarmonyPatch(typeof(EnergyPlatform), nameof(EnergyPlatform.Start)), HarmonyPrefix]
    public static void LogEnergyPlatformStart(EnergyPlatform __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.LogObject(__instance.gameObject, IDs.GetEnergyPlatformID(__instance));

        if (__instance.saveStatus)
        {
            foreach (GameObject obj in __instance.messageRecievers)
            {
                if (obj.GetComponent<GrandPlatform>() != null || obj.GetComponent<BarrierDoor>() != null)
                {
                    logger.RemoveStates(__instance.gameObject, IDs.GetEnergyPlatformStateID(__instance, true));
                    logger.AddStates(__instance.gameObject, IDs.GetEnergyPlatformStateID(__instance, false));
                    break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(EnergyPlatform), nameof(EnergyPlatform.SendOnMessage)), HarmonyPrefix]
    public static void LogEnergyPlatformActivate(EnergyPlatform __instance)
    {
        if (!__instance.active && __instance.saveStatus)
        {
            foreach (GameObject obj in __instance.messageRecievers)
            {
                if (obj.GetComponent<BarrierDoor>() != null ||
                    obj.GetComponent<GrandPlatform>() != null)
                //obj.GetComponent<TimedPlatformsParent>() != null ||
                //obj.GetComponent<TweenPosition>() != null)
                {
                    MovementLogger logger = Plugin.instance.movementLogger;
                    logger.SetLocation(__instance.gameObject, IDs.GetEnergyPlatformID(__instance), false, false);
                    logger.RemoveStates(__instance.gameObject, IDs.GetEnergyPlatformStateID(__instance, false));
                    logger.AddStates(__instance.gameObject, IDs.GetEnergyPlatformStateID(__instance, true));
                    break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(EnergyPlatform), nameof(EnergyPlatform.SendOffMessage)), HarmonyPrefix]
    public static void LogEnergyPlatformDeactivate(EnergyPlatform __instance)
    {
        if (__instance.active && __instance.saveStatus && __instance.unsaveStatus)
        {
            foreach (GameObject obj in __instance.messageRecievers)
            {
                if (obj.GetComponent<GrandPlatform>() != null || obj.GetComponent<BarrierDoor>() != null)
                {
                    MovementLogger logger = Plugin.instance.movementLogger;
                    logger.SetLocation(__instance.gameObject, IDs.GetEnergyPlatformID(__instance), false, false);
                    logger.AddStates(__instance.gameObject, IDs.GetEnergyPlatformStateID(__instance, false));
                    break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ItemBarrier), nameof(ItemBarrier.OnEnable)), HarmonyPrefix]
    public static void LogItemBarrierStart(ItemBarrier __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.LogObject(__instance.gameObject, IDs.GetItemBarrierID(__instance));

        if (__instance.ObjectRegistred()) logger.AddStates(__instance.gameObject, IDs.GetItemBarrierStateID(__instance, false));
    }

    [HarmonyPatch(typeof(ItemBarrier), nameof(ItemBarrier.DestroyBarrier)), HarmonyPrefix]
    public static void LogItemBarrierDestroy(ItemBarrier __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.SetLocation(__instance.gameObject, IDs.GetItemBarrierID(__instance), false, false);
        logger.AddStates(__instance.gameObject, IDs.GetItemBarrierStateID(__instance, false));
    }

    [HarmonyPatch(typeof(SaveObjectLocation), nameof(SaveObjectLocation.OnEnable)), HarmonyPrefix]
    public static void LogSaveObjectLocationStart(SaveObjectLocation __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.LogObject(__instance.gameObject, IDs.GetSaveObjectLocationID(__instance));

        PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();
        if (data.dataStrings.Contains(__instance.GetDataString())) logger.AddStates(__instance.gameObject, IDs.GetSaveObjectLocationStateID(__instance, true));
    }

    [HarmonyPatch(typeof(SaveObjectLocation), nameof(SaveObjectLocation.OnCollisionEnter2D)), HarmonyPrefix]
    public static void LogObjectLocationSaved(SaveObjectLocation __instance, Collision2D hit, bool ___activated)
    {
        PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();
        if (!data.dataStrings.Contains(__instance.GetDataString()) && !___activated && hit.gameObject.layer == (int)__instance.collisionLayer && hit.gameObject.name == __instance.targetObject.name)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            logger.SetLocation(__instance.gameObject, IDs.GetSaveObjectLocationID(__instance), false, false);

            if (__instance.otherPossibleSavePosition != null && data.dataStrings.Contains(__instance.GetDataStringFromOtherPos()))
            {
                SaveObjectLocation other = __instance.otherPossibleSavePosition.GetComponent<SaveObjectLocation>();
                if (other != null) logger.RemoveStates(other.gameObject, IDs.GetSaveObjectLocationStateID(other, true));
            }

            logger.AddStates(__instance.gameObject, IDs.GetSaveObjectLocationStateID(__instance, true));
        }
    }

    [HarmonyPatch(typeof(ColoredPoleController), nameof(ColoredPoleController.UpdatePoles)), HarmonyPrefix]
    public static void LogHighwaysPoleChange(ColoredPoleController __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();

        bool bluePolesDown = data.dataStrings.Contains("BluePoleDown");
        logger.RemoveStates(logger.GetPlayerPos(), "", IDs.GetHighwaysPoleStateID(!bluePolesDown));
        logger.AddStates(logger.GetPlayerPos(), "", IDs.GetHighwaysPoleStateID(bluePolesDown));
    }

    [HarmonyPatch(typeof(EyeSpinButton), nameof(EyeSpinButton.GotHitBy)), HarmonyPrefix]
    public static void LogEyeSpinButtonHit(EyeSpinButton __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        foreach (GameObject obj in __instance.messageRecievers)
        {
            if (obj != null)
            {
                if (obj.GetComponent<ColoredPoleController>() != null)
                {
                    logger.LogObject(__instance.gameObject, IDs.GetHighwaysPoleSwitchID(__instance));
                    logger.SetLocation(obj, IDs.GetHighwaysPoleSwitchID(__instance), false, false);
                }
                else if (obj.GetComponent<MoonDevice>() != null)
                {
                    logger.LogObject(__instance.gameObject, IDs.GetMuseumLightSwitchID(__instance));
                    logger.SetLocation(obj, IDs.GetMuseumLightSwitchID(__instance), false, false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(MoonDevice), nameof(MoonDevice.EyeButtonHit)), HarmonyPostfix]
    public static void LogMuseumLightChange(MoonDevice __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();

        bool museumLightOn = data.museumLightsOn;
        logger.RemoveStates(logger.GetPlayerPos(), "", IDs.GetMuseumLightStateID(!museumLightOn));
        if (__instance.moonDevicePressed != null) logger.AddStates(logger.GetPlayerPos(), "", IDs.GetMuseumLightStateID(museumLightOn));
    }

    [HarmonyPatch(typeof(MoonPlatform), nameof(MoonPlatform.DelayedOnEnable)), HarmonyPrefix]
    public static void LogMoonPlatformStart(MoonPlatform __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();

        bool museumLightOn = data.museumLightsOn;
        logger.RemoveStates(logger.GetPlayerPos(), "", IDs.GetMuseumLightStateID(!museumLightOn));
        logger.AddStates(logger.GetPlayerPos(), "", IDs.GetMuseumLightStateID(museumLightOn));
    }

}
