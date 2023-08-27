using dev.gmeister.unsighted.randomeister.core;
using dev.gmeister.unsighted.randomeister.unsighted;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace dev.gmeister.unsighted.randomeister.hooks;

[Harmony]
public class ItemChestHooks
{

    public const float METEOR_PING_SCALE = 1.5f;

    [HarmonyPatch(typeof(ItemChest), nameof(ItemChest.ShowDustPing)), HarmonyPostfix]
    public static void ToggleDustPingDuringGameplay(ItemChest __instance, ref bool __result)
    {
        if (Plugin.Instance.currentData != null && Plugin.Instance.currentData.chestRadarMoreOften) __result = !gameTime.paused && PseudoSingleton<GlobalGameData>.instance.currentData.radarLevel != 2;
    }

    [HarmonyPatch(typeof(ItemChest), nameof(ItemChest.Start)), HarmonyPostfix]
    public static void SetIconAfterChestStart(ItemChest __instance)
    {
        if (Plugin.Instance.currentData != null && Plugin.Instance.currentData.newChestRadar) ChangeMeteorPing(__instance);
    }

    [HarmonyPatch(typeof(ItemChest), nameof(ItemChest.DustIconColor)), HarmonyPostfix]
    public static void SetIconBeforeColourChange(ItemChest __instance)
    {
        if (Plugin.Instance.currentData != null && Plugin.Instance.currentData.newChestRadar) ChangeMeteorPing(__instance, false, true, false);
    }

    [HarmonyPatch(typeof(ItemChest), nameof(ItemChest.UpdateDustIconPosition)), HarmonyPostfix]
    public static void SetIconPosition(ItemChest __instance)
    {
        if (Plugin.Instance.currentData != null && Plugin.Instance.currentData.newChestRadar)
        {
            float cameraPadding = Plugin.Instance.options.chestRadarCameraPadding.Value;
            bool snapToChest = Plugin.Instance.options.chestRadarSnapping.Value;
            bool limitToAlma = Plugin.Instance.options.chestRadarCircular.Value;
            float radius = Plugin.Instance.options.chestRadarRadius.Value;
            float minRadius = 1f;

            CameraSystem camera = PseudoSingleton<CameraSystem>.instance;
            float cameraTop = camera.myTransform.position.y + camera.cameraSizeY * 0.5f - cameraPadding;
            float cameraBottom = camera.myTransform.position.y - camera.cameraSizeY * 0.5f + cameraPadding;
            float cameraRight = camera.myTransform.position.x + camera.cameraSizeX * 0.5f - cameraPadding;
            float cameraLeft = camera.myTransform.position.x - camera.cameraSizeX * 0.5f + cameraPadding;

            PlayerInfo player = PseudoSingleton<PlayersManager>.instance.players[0];
            Vector3 chestPos = __instance.myAnimator.mySpriteRenderer.transform.position;
            Vector3 centrePos = player.transform.position + Vector3.up * player.gameObject.GetComponent<TopdownPhysics>().globalHeight;

            Vector3 position = Vector3.zero;

            if (camera.PositionInsideCamera(chestPos) && snapToChest) position = chestPos;
            else
            {
                if (!camera.PositionInsideCamera(centrePos, -cameraPadding))
                {
                    centrePos = camera.myTransform.position;
                    limitToAlma = false;
                }

                Vector3 centreToChest = chestPos - centrePos;

                float scale = centreToChest.magnitude;
                if (chestPos.x > cameraRight) scale = (cameraRight - centrePos.x) / centreToChest.normalized.x;
                else if (chestPos.x < cameraLeft) scale = (cameraLeft - centrePos.x) / centreToChest.normalized.x;
                if (chestPos.y > cameraTop) scale = Math.Min(scale, (cameraTop - centrePos.y) / centreToChest.normalized.y);
                else if (chestPos.y < cameraBottom) scale = Math.Min(scale, (cameraBottom - centrePos.y) / centreToChest.normalized.y);

                if (limitToAlma)
                {
                    if (scale > radius) scale = radius;
                    else if (scale < minRadius) scale = minRadius;
                }

                position = centrePos + centreToChest.normalized * scale;
            }

            __instance.dustIcon.transform.position = position;
            __instance.meteorPing.transform.position = position;
        }
    }

    public static void ChangeMeteorPing(ItemChest chest, bool icon = true, bool color = true, bool scale = true)
    {
        if (chest == null) return;
        string item = PseudoSingleton<Helpers>.instance.GetChestReward(chest.gameObject.name, PseudoSingleton<MapManager>.instance.playerRoom.sceneName);

        if (Plugin.Instance.items != null)
        {
            ItemObject itemObject = PseudoSingleton<Helpers>.instance.GetItemObject(item);
            if (icon) SetMeteorPingIcon(chest, itemObject.itemMenuIcon);
            if (scale) ScaleMeteorPing(chest.dustIcon.GetComponent<TweenScale>(), Plugin.Instance.options.chestRadarScale.Value);

            if (itemObject is ChipObject chipObject && chipObject != null)
            {
                if (color) SetMeteorPingColor(chest, chipObject.chipColor);
                if (icon) AddMeteorPingChipGlow(chest, chipObject);
            }
            else if (itemObject is CogObject cogObject && cogObject != null && color) SetMeteorPingColor(chest, cogObject.cogColor);
        }
    }

    private static void SetMeteorPingIcon(ItemChest chest, Sprite icon)
    {
        chest.dustIcon.GetComponent<SpriteRenderer>().sprite = icon;
    }

    private static void SetMeteorPingColor(ItemChest chest, Color color)
    {
        chest.dustIcon.GetComponent<SpriteRenderer>().color = color;

        chest.dustIconOriginalColor = color;
        chest.dustIconFinalColor = color;
        chest.dustIconFinalColor.a = 0;
    }

    private static void AddMeteorPingChipGlow(ItemChest chest, ChipObject chipObject)
    {
        //This currently doesn't work
        if (chipObject.canOnlyHaveOne)
        {
            GameObject glow = PseudoSingleton<Lists>.instance.singleChipGlow;
            if (chipObject.chipSize == 2) glow = PseudoSingleton<Lists>.instance.doubleChipGlow;
            else if (chipObject.chipSize == 3) glow = PseudoSingleton<Lists>.instance.tripleChipGlow;

            GameObject clone = UnityEngine.Object.Instantiate<GameObject>(glow, chest.dustIcon.transform);
            clone.transform.localScale = Vector3.one;
            clone.transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            clone.GetComponent<UnityEngine.UI.Image>().color = chipObject.chipColor * 1.5f;
        }
    }

    private static void ScaleMeteorPing(TweenScale tween, float scale)
    {
        tween.from *= scale;
        tween.to *= scale;
    }

}
