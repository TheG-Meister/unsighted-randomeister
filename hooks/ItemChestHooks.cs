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
        __result = !gameTime.paused && PseudoSingleton<GlobalGameData>.instance.currentData.radarLevel != 2;
    }

    [HarmonyPatch(typeof(ItemChest), nameof(ItemChest.Start)), HarmonyPostfix]
    public static void SetIconAfterChestStart(ItemChest __instance)
    {
        ChangeMeteorPing(__instance);
    }

    [HarmonyPatch(typeof(ItemChest), nameof(ItemChest.DustIconColor)), HarmonyPostfix]
    public static void SetIconBeforeColourChange(ItemChest __instance)
    {
        ChangeMeteorPing(__instance, false, true, false);
    }

    public static void ChangeMeteorPing(ItemChest chest, bool icon = true, bool color = true, bool scale = true)
    {
        if (chest == null) return;
        string item = PseudoSingleton<Helpers>.instance.GetChestReward(chest.gameObject.name, PseudoSingleton<MapManager>.instance.playerRoom.sceneName);

        if (Plugin.Instance.items != null)
        {
            ItemObject itemObject = Plugin.Instance.items.GetItemObject(item);
            if (icon) SetMeteorPingIcon(chest, itemObject.itemMenuIcon);
            if (scale) ScaleMeteorPing(chest.dustIcon.GetComponent<TweenScale>(), METEOR_PING_SCALE);

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
