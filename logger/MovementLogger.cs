using dev.gmeister.unsighted.randomeister.core;
using dev.gmeister.unsighted.randomeister.unsighted;
using HarmonyLib;
using System;
using System.Drawing;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.SceneManagement;
using static System.Collections.Specialized.BitVector32;
using static dev.gmeister.unsighted.randomeister.unsighted.PlayerAction;
using System.Text;
using System.Linq;
using System.Collections;
using System.Reflection.Emit;

namespace dev.gmeister.unsighted.randomeister.logger;

[Harmony]
public class MovementLogger : Logger
{

    private string? currentLocation;
    private readonly HashSet<PlayerAction> actions;
    private readonly List<string> tags;
    private bool sceneChange;
    public bool announce;
    public bool log;

    private HashSet<PlayerAction> silentActions = new() { Walk, Run, StaminaRecharge, Attack, DashAttack, SpinAttack, Parry, SpinnerAttack, JumpOffSpinner, Grind, JumpUp };

    public MovementLogger(string path, bool log, bool announce) : base(path)
    {
        currentLocation = null;
        this.actions = new();
        this.tags = new();
        this.announce = announce;
        this.log = log;
        this.sceneChange = false;
    }

    public void SetLocation(string location, Vector3 position, bool sceneChange)
    {
        if (this.log)
        {
            ColorNames colour = ColorNames.Yellow;
            if (currentLocation != null && currentLocation != location)
            {
                colour = ColorNames.Green;
                string actions = string.Join(",", this.actions.ToArray());
                string tags = string.Join(",", this.tags.ToArray());

                List<string> fields = new() { currentLocation, location, actions, tags };

                stream.WriteLine(string.Join("\t", fields));
                stream.Flush();
            }
            if (this.announce)
            {
                PseudoSingleton<InGameTextController>.instance.ShowText(location, this.GetPositionInCamera(position), color: colour, duration: 2f);
            }
            currentLocation = location;
        }
        else
        {
            this.currentLocation = null;
        }
        this.actions.Clear();
        this.tags.Clear();
    }

    public void ClearLocation()
    {
        this.currentLocation = null;
        this.actions.Clear();
        this.tags.Clear();
    }

    public Vector3 GetPositionInCamera(Vector3 pos)
    {
        CameraSystem cameraSystem = PseudoSingleton<CameraSystem>.instance;
        if (!cameraSystem.PositionInsideCamera(pos, -2f))
        {
            Vector3 cameraPos = cameraSystem.myTransform.position;
            cameraPos.z = 0;
            return cameraPos;
        }
        else return pos;
    }

    public void AddActions(Vector3 position, params PlayerAction[] actions)
    {
        if (this.log && !this.sceneChange)
        {
            List<string> announcements = new();
            foreach (PlayerAction action in actions)
            {
                if (!this.actions.Contains(action))
                {
                    this.actions.Add(action);
                    //if (!this.silentActions.Contains(action)) 
                    announcements.Add(action.ToString());
                }
            }
            if (this.announce) PseudoSingleton<InGameTextController>.instance.ShowText(announcements.Join(delimiter: "\n"), this.GetPositionInCamera(position));
        }
    }

    public void AddActions(BasicCharacterController controller, params PlayerAction[] actions)
    {
        this.AddActions(controller.gameObject.transform.position + Vector3.up * (controller.myPhysics.globalHeight + controller.myPhysics.Zsize * 1.55f), actions);
    }

    public void AddTags(string[] tags)
    {
        this.tags.AddRange(tags);
    }

    public static string SnakeToPascalCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        else
        {
            StringBuilder builder = new();

            bool lastUnderscore = true;
            foreach (char c in text)
            {
                if (c == '_') lastUnderscore = true;
                else if (Char.IsLetter(c) && lastUnderscore)
                {
                    builder.Append(Char.ToUpperInvariant(c));
                    lastUnderscore = false;
                }
                else builder.Append(c);
            }

            return builder.ToString();
        }
    }

    public static string GetTransitionName(string scene, ScreenTransition transition)
    {
        return String.Join(Constants.SCENE_TRANSITION_ID_SEPARATOR.ToString(), SceneManager.GetActiveScene().name, MovementLogger.SnakeToPascalCase(transition.myDirection.ToString()), transition.triggerID);
    }

    [HarmonyPatch(typeof(ScreenTransition), nameof(ScreenTransition.PlayerScreenTransition)), HarmonyPrefix]
    public static void OnScreenTransition(ScreenTransition __instance)
    {
        string location = MovementLogger.GetTransitionName(SceneManager.GetActiveScene().name, __instance);

        Vector3 pos = PseudoSingleton<CameraSystem>.instance.myTransform.position;
        pos.z = 0;
        Plugin.Instance.movementLogger.SetLocation(location, pos, true);
    }

    [HarmonyPatch(typeof(ScreenTransition), nameof(ScreenTransition.EndPlayerScreenTransition)), HarmonyPostfix]
    public static void AfterEndPlayerScreenTransition(ScreenTransition __instance, ref IEnumerator __result)
    {
        if (ScreenTransition.playerTransitioningScreens &&
            ScreenTransition.currentDoorName == __instance.gameObject.name &&
            (ScreenTransition.teleportCheat ||
            ScreenTransition.lastSceneName == PseudoSingleton<MapManager>.instance.GetNextRoomName(__instance.myDirection, __instance.triggerID)))
        {
            string location = MovementLogger.GetTransitionName(SceneManager.GetActiveScene().name, __instance);

            __result = MovementLogger.AddLocationChangeToEnumerator(__result, location);
        }
    }

    public static IEnumerator AddLocationChangeToEnumerator(IEnumerator original, string location)
    {
        while (original.MoveNext()) yield return original.Current;
        Vector3 pos = PseudoSingleton<CameraSystem>.instance.myTransform.position;
        pos.z = 0;
        Plugin.Instance.movementLogger.SetLocation(location, pos, false);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StaminaChargeCoroutine)), HarmonyPrefix]
    public static void LogStaminaRecharge(BasicCharacterController __instance)
    {
        Plugin.Instance.movementLogger.AddActions(__instance, StaminaRecharge);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.SwordCoroutine)), HarmonyPrefix]
    public static void LogMeleeAttacks(BasicCharacterController __instance, bool ___forceJumpAttack)
    {
        if ((!__instance.jumpAttacking || __instance.myPhysics.grounded) && (!__instance.staminaDrained || !__instance.myPhysics.grounded || __instance.jumpAttacking))
        {
            List<PlayerAction> actions = new() { Attack };
            if (!__instance.jumpAttacking && (!__instance.myPhysics.grounded || (___forceJumpAttack && !__instance.myInfo.canJump) || __instance.ridingSpinner)) actions.Add(JumpAttack);
            if (__instance.running) actions.Add(DashAttack);
            Plugin.Instance.movementLogger.AddActions(__instance, actions.ToArray());
        }
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.MeleeAttackCharge)), HarmonyPrefix]
    public static void LogSpinAttack(BasicCharacterController __instance)
    {
        HashSet<PlayerAction> actions = new() { SpinAttack };
        if (__instance.hookshotFiring) actions.Add(Telehook);
        Plugin.Instance.movementLogger.AddActions(__instance, actions.ToArray());
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.GuardCoroutine)), HarmonyPrefix]
    public static void LogParry(BasicCharacterController __instance)
    {
        Plugin.Instance.movementLogger.AddActions(__instance, Parry);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.ShurikenCoroutine)), HarmonyPrefix]
    public static void LogShuriken(BasicCharacterController __instance)
    {
        if (!__instance.staminaDrained && __instance.CanThrowShuriken()) Plugin.Instance.movementLogger.AddActions(__instance, ShurikenThrow);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.GunFireCoroutine)), HarmonyPrefix]
    public static void LogGuns(BasicCharacterController __instance)
    {
        if (!__instance.myAnimations.hookshotEquipped && !__instance.dashing)
        {
            switch (__instance.myInfo.GetHoldWeapon())
            {
                case "Blaster":
                case "DoctorsGun":
                case "AutomaticBlaster":
                case "Shotgun":
                    Plugin.Instance.movementLogger.AddActions(__instance, ShootBullet);
                    break;
                case "Flamethrower":
                case "Icethrower":
                    Plugin.Instance.movementLogger.AddActions(__instance, Spray);
                    break;
                case "GranadeLauncher":
                case "IceGranade":
                case "GranadeShotgun":
                    Plugin.Instance.movementLogger.AddActions(__instance, Grenade);
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(BulletRaycaster), nameof(BulletRaycaster.IceShurikenPlatformSpawner)), HarmonyPostfix]
    public static void LogCryojetPlatformSpawn(BulletRaycaster __instance, ref IEnumerator __result)
    {
        __result = MovementLogger.GetCryojetPlatformLoggingEnumerator(__instance, __result);
    }

    public static IEnumerator GetCryojetPlatformLoggingEnumerator(BulletRaycaster raycaster, IEnumerator original)
    {
        while (original.MoveNext())
        {
            if (!raycaster.AnyPointOverlapPlatform(0.75f))
            {
                ElevatedGround elevatedGround = PseudoSingleton<Helpers>.instance.HighestGround(raycaster.transform.position, false, true);
                if (elevatedGround.deepWater) Plugin.Instance.movementLogger.AddActions(raycaster.transform.position, CreateIceOrRockPlatform);
            }
            yield return original.Current;
        }
    }

    [HarmonyPatch(typeof(ShurikenController), nameof(ShurikenController.IceShurikenPlatformSpawner)), HarmonyPostfix]
    public static void LogIceShurikenPlatformSpawn(ShurikenController __instance, ref IEnumerator __result)
    {
        __result = MovementLogger.GetIceShurikenPlatformLoggingEnumerator(__instance, __result);
    }

    public static IEnumerator GetIceShurikenPlatformLoggingEnumerator(ShurikenController controller, IEnumerator original)
    {
        while (original.MoveNext())
        {
            if (controller.myPhysics.globalHeight <= 1.5f && !controller.AnyPointOverlapPlatform(0.25f))
            {
                ElevatedGround elevatedGround = PseudoSingleton<Helpers>.instance.HighestGround(controller.transform.position, false, true);
                if (elevatedGround.deepWater) Plugin.Instance.movementLogger.AddActions(controller.transform.position, CreateIceOrRockPlatform);
            }
            yield return original.Current;
        }
    }

    [HarmonyPatch(typeof(GranadeController), nameof(GranadeController.FallOnWater)), HarmonyPrefix]
    public static void LogIceGrenadePlatformSpawn(GranadeController __instance)
    {
        if (__instance.iceGranade) Plugin.Instance.movementLogger.AddActions(__instance.transform.position, CreateIceOrRockPlatform);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.HookshotCoroutine)), HarmonyPrefix]
    public static void LogHookshot(BasicCharacterController __instance)
    {
        HashSet<PlayerAction> actions = new() { Hookshot };
        if (__instance.hookshotClimbing) actions.Add(HookshotWhileHanging);
        if (__instance.meleeCharging) actions.Add(Telehook);
        Plugin.Instance.movementLogger.AddActions(__instance, actions.ToArray());
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StartRidingSpinner)), HarmonyPrefix]
    public static void LogSpinner(BasicCharacterController __instance)
    {
        Plugin.Instance.movementLogger.AddActions(__instance, Spinner);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.SpinnerAttack)), HarmonyPrefix]
    public static void LogSpinnerAttack(BasicCharacterController __instance, float ___lastTimeWaterSkip)
    {
        if (Time.time - ___lastTimeWaterSkip >= 0.3f && (__instance.myPhysics.height == 0f || __instance.spinnerGrinding || (__instance.myPhysics.currentElevatedGround != null && __instance.myPhysics.currentElevatedGround.deepWater)))
        {
            HashSet<PlayerAction> actions = new() { SpinnerAttack };
            if (__instance.myPhysics.currentElevatedGround != null && __instance.myPhysics.currentElevatedGround.deepWater && !__instance.spinnerGrinding && (__instance.myPhysics.globalHeight < 0.75f && __instance.myPhysics.heightDelta < 0f && PseudoSingleton<CameraSystem>.instance.PositionInsideCamera(__instance.myAnimations.myAnimator.transform.position, 0f)))
                actions.Add(Skip);
            Plugin.Instance.movementLogger.AddActions(__instance, actions.ToArray());
        }
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.CheckIfBeganRidingRail)), HarmonyPrefix]
    public static void LogGrind(BasicCharacterController __instance)
    {
        if (!__instance.spinnerGrinding) Plugin.Instance.movementLogger.AddActions(__instance, Grind);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.Dash)), HarmonyPrefix]
    public static void LogHookshotJump(BasicCharacterController __instance, float impulseStrength)
    {
        if (impulseStrength == 0 && __instance.hookshotClimbing)
            Plugin.Instance.movementLogger.AddActions(__instance, JumpWhileHanging);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.DashCoroutine)), HarmonyPrefix]
    public static void LogJump(BasicCharacterController __instance, float impulseStrength)
    {
        if (impulseStrength == 0 && !__instance.climbingDash)
        {
            if (!__instance.upwardAttack)
            {
                HashSet<PlayerAction> actions = new() { Dodge, Jump };
                if (__instance.jumpedWhileRiddingSpinner) actions.Add(JumpOffSpinner);
                if (__instance.wallKicked) actions.Add(WallJump);
                if (__instance.axis == Vector3.zero) actions.Add(JumpUp);
                if (__instance.jumpedWhileRiddingSpinner && __instance.axis == Vector3.zero) actions.Add(JumpUpOffSpinner);
                Plugin.Instance.movementLogger.AddActions(__instance, actions.ToArray());
            }
            else if (__instance.wallKicked) Plugin.Instance.movementLogger.AddActions(__instance, Jump, ClimbSlash);
        }
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.RollCoroutine)), HarmonyPrefix]
    public static void LogDodge(BasicCharacterController __instance)
    {
        Plugin.Instance.movementLogger.AddActions(__instance, Dodge);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.LiftObjectCoroutine)), HarmonyPrefix]
    public static void LogBoxGrab(BasicCharacterController __instance)
    {
        if (__instance.myHoldingObject.breakable) Plugin.Instance.movementLogger.AddActions(__instance, GrabBox);
    }

    [HarmonyPatch(typeof(HoldableObject), nameof(HoldableObject.PlacedOnGround)), HarmonyPrefix]
    public static void LogBoxPlace(HoldableObject __instance)
    {
        if (__instance.breakable) Plugin.Instance.movementLogger.AddActions(__instance.transform.position, PlaceBox);
    }

    [HarmonyPatch(typeof(HoldableObject), nameof(HoldableObject.ThrownAt)), HarmonyPrefix]
    public static void LogBoxThrow(HoldableObject __instance)
    {
        if (__instance.breakable) Plugin.Instance.movementLogger.AddActions(__instance.transform.position, ThrowBox);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.GetDashInput)), HarmonyPrefix]
    public static void LogBoxJump(BasicCharacterController __instance)
    {
        if (!PlayerInfo.cutscene && ButtonSystem.GetKeyDown(PseudoSingleton<GlobalInputManager>.instance.inputData.GetInputProfile(__instance.myInfo.playerNum).dash) && (__instance.holdingObject || __instance.carryingRaquel) && !__instance.myInfo.canJump && ButtonSystem.GetKey(PseudoSingleton<GlobalInputManager>.instance.inputData.GetInputProfile(__instance.myInfo.playerNum).guard, false))
            Plugin.Instance.movementLogger.AddActions(__instance, BoxJump);
    }

    /*[HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.CastHookshotRaycast)), HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> EnableHookshotDebug(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> result = new(instructions);

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].opcode == OpCodes.Ldc_I4_0)
            {
                result[i] = new CodeInstruction(OpCodes.Ldc_I4_1);
                break;
            }
        }

        return result;
    }*/

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.CastHookshotRaycast)), HarmonyPostfix]
    public static void ReportHookshotWallHit(BasicCharacterController __instance, RaycastHit2D ___tempWallHit1, RaycastHit2D ___tempWallHit2, RaycastHit2D ___tempWallHit3, EnemyHitBox ___currentEnemyHitBox)
    {
        if (__instance.hookshotFiring)
        {
            List<RaycastHit2D> hits = new() { ___tempWallHit1, ___tempWallHit2, ___tempWallHit3 };
            foreach (RaycastHit2D hit in hits) if (hit.collider == null || !__instance.HookshotNotBeingIgnored(hit) || __instance.HookpointNotBlockedByWall(hit)) return;
            if (___currentEnemyHitBox != null) Plugin.Instance.movementLogger.AddActions(__instance, Wierdshot);
        }
        /*RaycastHit2D r = __instance.currentWallHit;
        StringBuilder sb = new();
        sb.Append(r.point);
        sb.Append(", ");
        sb.Append(r.centroid);
        sb.Append(", ");
        sb.Append(r.normal);
        sb.Append(", ");
        sb.Append(r.distance);
        sb.Append(", ");
        if (r.collider != null && r.collider.transform.parent != null) sb.Append(r.collider.transform.parent.name);
        else sb.Append("null");
        sb.Append(", ");
        if (r.rigidbody != null && r.rigidbody.transform.parent != null) sb.Append(r.rigidbody.transform.parent.name);
        else sb.Append("null");
        sb.Append(", ");
        if (r.transform != null && r.transform.parent != null) sb.Append(r.transform.parent.name);
        else sb.Append("null");
        Plugin.Instance.GetLogger().LogInfo($"Hookshot results: {sb}");*/
    }

}
