using dev.gmeister.unsighted.randomeister.core;
using dev.gmeister.unsighted.randomeister.unsighted;
using HarmonyLib;
using System;
using System.Drawing;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.SceneManagement;
using static dev.gmeister.unsighted.randomeister.unsighted.PlayerAction;
using System.Text;
using System.Linq;
using System.Collections;
using System.Reflection.Emit;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using static dev.gmeister.unsighted.randomeister.logger.MovementLogger;
using static TopdownPhysics;

namespace dev.gmeister.unsighted.randomeister.logger;

[Harmony]
public class MovementLogger : Logger
{

    public class Announcement
    {
        public string text;
        public ColorNames colour;
        public Vector3 position;

        public Announcement()
        {
        }

        public Announcement(string text, ColorNames colour, Vector3 position)
        {
            this.text = text;
            this.colour = colour;
            this.position = position;
        }
    }

    public bool announce;
    public bool uniqueAnnouncements;
    public bool log;
    public float lastAnnouncementTime;
    public float announcementDelay;
    public float cameraPadding;
    public List<Announcement> announcements;

    public Logger nodeLogger;
    public Logger edgeLogger;

    public List<MovementNode> nodes;
    public List<MovementEdge> edges;
    public List<MovementState> states;
    public int largestNodeID;
    public int largestStateID;
    public MovementNode currentNode;

    private readonly HashSet<string> tags;
    private readonly HashSet<PlayerAction> actions;
    private readonly HashSet<string> gameStates;
    private readonly HashSet<string> roomStates;
    private bool changingScene;
    private float gameTime;
    private float realTime;

    private HashSet<PlayerAction> silentActions = new() { Walk, Run, StaminaRecharge, Attack, DashAttack, SpinAttack, Parry, SpinnerAttack, JumpOffSpinner, Grind, JumpUp };

    public MovementLogger(string path, bool log, bool announce, bool uniqueAnnouncements, float announcementDelay, float cameraPadding) : base(path)
    {
        this.actions = new();
        this.tags = new();
        this.gameStates = new();
        this.roomStates = new();
        this.changingScene = false;

        this.announce = announce;
        this.log = log;
        this.uniqueAnnouncements = uniqueAnnouncements;
        this.lastAnnouncementTime = float.MinValue;
        this.announcements = new();
        this.announcementDelay = 0.33333f;
        this.cameraPadding = -4f;

        this.nodes = new();
        this.edges = new();
        this.states = new();
        this.largestNodeID = -1;
        this.currentNode = null;

        foreach (MovementNode node in this.nodes) if (node.id > this.largestNodeID) this.largestNodeID = node.id;

        this.nodeLogger = new Logger(Constants.PATH_DEFAULT + Constants.PATH_LOGS + "nodes.tsv");
        this.edgeLogger = new Logger(Constants.PATH_DEFAULT + Constants.PATH_LOGS + "edges.tsv");
    }

    public MovementNode GetNode(string scene, string location, HashSet<PlayerAction> actions = null, HashSet<string> states = null)
    {
        bool intermediate = (actions != null && actions.Count > 0) || (states != null && states.Count > 0);

        MovementNode node = this.nodes.Find(n => n.scene == scene && n.location == location && (!intermediate || (n.actions.SetEquals(actions) && n.states.SetEquals(states))));
        if (node == null)
        {
            this.largestNodeID++;
            node = new MovementNode(this.largestNodeID, scene, location);
            string actionsString = "";
            string statesString = "";

            if (intermediate)
            {
                if (actions != null)
                {
                    foreach (PlayerAction action in actions) node.actions.Add(action);
                    actionsString = string.Join(",", actions);
                }
                if (states != null)
                {
                    foreach (string state in states) node.states.Add(state);
                    statesString = string.Join(",", states);
                }
            }
            this.nodes.Add(node);

            this.nodeLogger.stream.WriteLine(string.Join("\t", node.id, node.scene, node.location, actionsString, statesString));
            this.nodeLogger.stream.Flush();
        }

        return node;
    }

    public void Announce()
    {
        float time = Time.realtimeSinceStartup;
        if (this.announce && !this.changingScene && time - this.lastAnnouncementTime > this.announcementDelay && this.announcements.Count > 0)
        {
            Announcement first = this.announcements[0];
            ColorNames colour = this.announcements[0].colour;
            List<string> announcements = new();
            List<Announcement> removed = new();

            foreach (Announcement announcement in this.announcements)
            {
                if (!announcement.colour.Equals(colour)) break;
                announcements.Add(announcement.text);
                removed.Add(announcement);
            }

            foreach (Announcement announcement in removed) this.announcements.Remove(announcement);

            this.lastAnnouncementTime = time;
            PseudoSingleton<InGameTextController>.instance.ShowText(string.Join("\n", announcements.Distinct().ToList()), this.GetPositionInCamera(first.position), color: colour, duration: 2f);
        }
    }

    public void LogLocation(MovementNode node, Vector3 position, bool sceneChange, float realTime, float gameTime)
    {
        if (realTime < 0) realTime = 0;
        if (gameTime < 0) gameTime = 0;

        ColorNames colour = ColorNames.Yellow;
        if (this.currentNode != null && (this.currentNode.scene != node.scene || this.currentNode.location != node.location))
        {
            colour = ColorNames.Green;
            if (this.log)
            {
                List<string> statesList = new(gameStates);
                statesList.AddRange(this.roomStates);

                MovementEdge edge = new MovementEdge(this.currentNode.id, node.id, sceneChange, (realTime - this.realTime), (gameTime - this.gameTime));
                    this.edges.Add(edge);

                string states = string.Join(",", statesList.ToArray());
                string actions = string.Join(",", this.actions.ToArray());
                string realTimeDuration = (realTime - this.realTime).ToString();
                string gameTimeDuration = (gameTime - this.gameTime).ToString();

                this.edgeLogger.stream.WriteLine(string.Join("\t", edge.source, edge.target, actions, states, edge.sceneChange, edge.realTime, edge.gameTime));
                this.edgeLogger.stream.Flush();
            }
        }

        if (this.announce)
        {
            List<string> locationParts = node.location.Split(Constants.MOVEMENT_LOGGER_ID_SEPARATOR).ToList();
            string announcement = locationParts.Select(s => ReplaceSpecialCharsInPascal(s)).Join(delimiter: ", ");
            this.announcements.Add(new(announcement, colour, position));
        }
    }

    public void SetLocation(string scene, string location, Vector3 position, bool intermediate, bool changingScene)
    {
        float realTime = Time.realtimeSinceStartup;
        if (realTime < 0) realTime = 0;
        GameplayTime gameplayTime = PseudoSingleton<Helpers>.instance.GetCurrentTimeData();
        float gameTime = gameplayTime.hours * 60 * 60 + gameplayTime.minutes * 60 + gameplayTime.seconds;

        HashSet<string> statesList = new(gameStates);
        foreach (string state in this.roomStates) statesList.Add(state);

        MovementNode target;
        if (intermediate) target = this.GetNode(scene, location, this.actions, statesList);
        else target = this.GetNode(scene, location);

        this.LogLocation(target, position, this.changingScene, realTime, gameTime);

        if (this.changingScene && !changingScene) this.roomStates.Clear();

        if (this.log) this.currentNode = target;
        else this.currentNode = null;

        this.gameTime = gameTime;
        this.realTime = realTime;
        this.changingScene = changingScene;

        this.actions.Clear();
        this.tags.Clear();
    }

    public void ClearLocation()
    {
        this.currentNode = null;
    }

    public void SetChangingScene(bool changingScene)
    {
        if (this.changingScene && !changingScene) this.roomStates.Clear();
        this.changingScene = changingScene;
    }

    public void AddActions(Vector3 position, params PlayerAction[] actions)
    {
        List<string> announcements = new();
        foreach (PlayerAction action in actions)
        {
            if (!this.uniqueAnnouncements || !this.actions.Contains(action))
            {
                this.actions.Add(action);
                //if (!this.silentActions.Contains(action)) 
                announcements.Add(MovementLogger.ReplaceSpecialCharsInPascal(action.ToString()));
            }
        }
        if (this.announce && announcements.Count > 0)
            foreach (string announcement in announcements)
                this.announcements.Add(new(announcement, ColorNames.White, position));
    }

    public void AddActions(BasicCharacterController controller, params PlayerAction[] actions)
    {
        this.AddActions(controller.gameObject.transform.position + Vector3.up * (controller.myPhysics.globalHeight + controller.myPhysics.Zsize * 1.55f), actions);
    }

    public void AddActions(MechaController controller, params PlayerAction[] actions)
    {
        this.AddActions(controller.transform.position + Vector3.up * (controller.myPhysics.globalHeight), actions);
    }

    public void AddRoomStates(Vector3 position, params string[] states)
    {
        List<string> announcements = new();
        foreach (string state in states)
        {
            if (!this.roomStates.Contains(state))
            {
                this.roomStates.Add(state);
                announcements.Add(MovementLogger.ReplaceSpecialCharsInPascal(state));
            }
        }
        if (this.announce && announcements.Count > 0)
            foreach (string announcement in announcements)
                this.announcements.Add(new(announcement, ColorNames.Orange, position));
    }

    public void AddGameStates(Vector3 position, params string[] states)
    {
        List<string> announcements = new();
        foreach (string state in states)
        {
            if (!this.gameStates.Contains(state))
            {
                this.gameStates.Add(state);
                announcements.Add(MovementLogger.ReplaceSpecialCharsInPascal(state));
            }
        }
        if (this.announce && announcements.Count > 0)
            foreach (string announcement in announcements)
                this.announcements.Add(new(announcement, ColorNames.Orange, position));
    }

    public void RemoveGameStates(Vector3 position, params string[] states)
    {
        List<string> announcements = new();
        foreach (string state in states)
        {
            if (this.gameStates.Contains(state))
            {
                this.gameStates.Remove(state);
                announcements.Add(MovementLogger.ReplaceSpecialCharsInPascal(state));
            }
        }
        if (this.announce && announcements.Count > 0)
            foreach (string announcement in announcements)
                this.announcements.Add(new(announcement, ColorNames.Blue, position));
    }

    public void ClearGameStates()
    {
        this.gameStates.Clear();
    }

    public void AddTags(string[] tags)
    {
        foreach (string tag in tags) this.tags.Add(tag);
    }

    public static Vector3 GetCameraPos()
    {
        Vector3 pos = PseudoSingleton<CameraSystem>.instance.myTransform.position;
        pos.z = 0;
        return pos;
    }

    public Vector3 GetPositionInCamera(Vector3 pos)
    {
        CameraSystem cameraSystem = PseudoSingleton<CameraSystem>.instance;
        if (!cameraSystem.PositionInsideCamera(pos, this.cameraPadding)) return MovementLogger.GetCameraPos();
        else return pos;
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
                else if (char.IsLetter(c))
                {
                    if (lastUnderscore)
                    {
                        builder.Append(char.ToUpperInvariant(c));
                        lastUnderscore = false;
                    }
                    else builder.Append(char.ToLowerInvariant(c));
                }
                else builder.Append(c);
            }

            return builder.ToString();
        }
    }

    public static string ReplaceSpecialCharsInPascal(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        else
        {
            StringBuilder builder = new();

            bool firstChar = true;
            char lastChar = ' ';
            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    if (!firstChar && !char.IsLetter(lastChar))
                    {
                        builder.Append(' ');
                        builder.Append(char.ToUpperInvariant(c));
                    }
                    else
                    {
                        if (!firstChar && char.IsUpper(c)) builder.Append(' ');
                        builder.Append(c);
                    }
                }
                else if (char.IsNumber(c))
                {
                    if (!firstChar && !char.IsNumber(lastChar)) builder.Append(' ');
                    builder.Append(c);
                }

                firstChar = false;
                lastChar = c;
            }

            return builder.ToString();
        }
    }

    // ----------------------- IDS -------------------- //

    public string GetScreenTransitionID(ScreenTransition transition)
    {
        return string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), transition.GetType(), MovementLogger.SnakeToPascalCase(transition.myDirection.ToString()), transition.triggerID);
    }

    public string GetTerminalID()
    {
        return string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), typeof(Terminal));
    }

    public string GetHoleID(HoleTeleporter hole)
    {
        return string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), hole.GetType(), hole.holeIndex);
    }

    public string GetElevatorID(Elevator elevator)
    {
        return string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), elevator.GetType(), elevator.elevatorID);
    }

    public string GetLadderID(SceneChangeLadder ladder)
    {
        return string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), ladder.GetType(), ladder.name);
    }

    public string GetTowerElevatorID(CraterTowerElevator elevator)
    {
        return string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), elevator.GetType(), elevator.name);
    }

    public string GetMetalScrapOreStateID(MetalScrapOre ore, bool present)
    {
        return string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), ore.name, ore.transform.GetSiblingIndex(), present ? "Present" : "Absent");
    }

    public string GetMetalScrapOreLocationID(MetalScrapOre ore)
    {
        return string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), ore.name, ore.transform.GetSiblingIndex());
    }

    // ------------------------- ROOM CHANGES --------------------- //

    public IEnumerator AddLocationChangeToEnumerator(IEnumerator original, string scene, string location, Vector3 position, bool intermediate, bool changingScene)
    {
        while (original.MoveNext()) yield return original.Current;
        Plugin.Instance.movementLogger.SetLocation(scene, location, position, intermediate, changingScene);
        MovementLogger.PollActions();
    }

    [HarmonyPatch(typeof(ScreenTransition), nameof(ScreenTransition.PlayerScreenTransition)), HarmonyPrefix]
    public static void LogEnterScreenTransition(ScreenTransition __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string location = logger.GetScreenTransitionID(__instance);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, MovementLogger.GetCameraPos(), false, true);
    }

    [HarmonyPatch(typeof(ScreenTransition), nameof(ScreenTransition.EndPlayerScreenTransition)), HarmonyPostfix]
    public static void LogExitScreenTransition(ScreenTransition __instance, ref IEnumerator __result)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        if (ScreenTransition.playerTransitioningScreens &&
            ScreenTransition.currentDoorName == __instance.gameObject.name &&
            (ScreenTransition.teleportCheat ||
            ScreenTransition.lastSceneName == PseudoSingleton<MapManager>.instance.GetNextRoomName(__instance.myDirection, __instance.triggerID)))
        {
            string location = logger.GetScreenTransitionID(__instance);

            __result = logger.AddLocationChangeToEnumerator(__result, SceneManager.GetActiveScene().name, location, MovementLogger.GetCameraPos(), false, false);
        }
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.PlayersEnteredMyTrigger)), HarmonyPostfix]
    public static void LogEnterTerminalTrigger(Terminal __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;
        logger.SetLocation(scene, logger.GetTerminalID(), __instance.transform.position, false, false);
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
        Plugin.Instance.movementLogger.SetChangingScene(true);
    }

    [HarmonyPatch(typeof(HoleTeleporter), nameof(HoleTeleporter.FallIntoHole)), HarmonyPrefix]
    public static void LogEnterHole(HoleTeleporter __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string location = logger.GetHoleID(__instance);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(HoleTeleporter), nameof(HoleTeleporter.Start)), HarmonyPostfix]
    public static void LogExitHole(HoleTeleporter __instance, ref IEnumerator __result)
    {
        if (__instance.GetComponent<ElevatedGround>() == null && HoleTeleporter.fallingDownOnHole && HoleTeleporter.lastHoleID == __instance.holeIndex)
        {
            MovementLogger logger = Plugin.Instance.movementLogger;
            string location = logger.GetHoleID(__instance);
            __result = logger.AddLocationChangeToEnumerator(__result, SceneManager.GetActiveScene().name, location, __instance.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(Elevator), nameof(Elevator.PlayerScreenTransition)), HarmonyPrefix]
    public static void LogEnterElevator(Elevator __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string location = logger.GetElevatorID(__instance);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(Elevator), nameof(Elevator.Start)), HarmonyPostfix]
    public static void LogExitElevator(Elevator __instance, ref IEnumerator __result)
    {
        if (__instance.elevatorID == Elevator.lastElevatorID && Elevator.ridingElevator)
        {
            MovementLogger logger = Plugin.Instance.movementLogger;
            string location = logger.GetElevatorID(__instance);
            __result = logger.AddLocationChangeToEnumerator(__result, SceneManager.GetActiveScene().name, location, __instance.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(CrystalAppear), nameof(CrystalAppear.CollectionCoroutine)), HarmonyPrefix]
    public static void LogEnterCrystal(CrystalAppear __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string location = string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), SceneManager.GetActiveScene().name, typeof(CrystalAppear), __instance.myBossName);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(CrystalTeleportExit), nameof(CrystalTeleportExit.Start)), HarmonyPostfix]
    public static void LogExitCrystal(CrystalTeleportExit __instance, ref IEnumerator __result)
    {
        if (CrystalTeleportExit.usingCrystalTeleport)
        {
            string location = string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), SceneManager.GetActiveScene().name, typeof(CrystalTeleportExit));
            MovementLogger logger = Plugin.Instance.movementLogger;
            logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(SceneChangeLadder), nameof(SceneChangeLadder.LadderCoroutine)), HarmonyPrefix]
    public static void LogEnterLadder(SceneChangeLadder __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string location = logger.GetLadderID(__instance);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(SceneChangeLadder), nameof(SceneChangeLadder.Start)), HarmonyPrefix]
    public static void LogExitLadder(SceneChangeLadder __instance)
    {
        if (SceneChangeLadder.currentLadder == __instance.name)
        {
            MovementLogger logger = Plugin.Instance.movementLogger;
            string location = logger.GetLadderID(__instance);
            logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(CraterTowerElevator), nameof(CraterTowerElevator.ElevatorCoroutine)), HarmonyPrefix]
    public static void LogEnterTowerElevator(CraterTowerElevator __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string location = logger.GetTowerElevatorID(__instance);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(CraterTowerElevator), nameof(CraterTowerElevator.Start)), HarmonyPostfix]
    public static void LogExitTowerElevator(CraterTowerElevator __instance, ref IEnumerator __result)
    {
        if (CraterTowerElevator.currentElevator == __instance.name)
        {
            MovementLogger logger = Plugin.Instance.movementLogger;
            string location = logger.GetTowerElevatorID(__instance);
            __result = logger.AddLocationChangeToEnumerator(__result, SceneManager.GetActiveScene().name, location, __instance.transform.position, false, false);
        }
    }

    // ----------------------- ACTIONS -------------------------- //

    public static void PollActions()
    {
        List<PlayerInfo> players = PseudoSingleton<PlayersManager>.instance.players;
        foreach (PlayerInfo player in players)
        {
            HashSet<PlayerAction> actions = new();
            if (player.myCharacter.ridingSpinner) actions.Add(Spinner);
            if (player.myCharacter.ridingMecha) actions.Add(Hailee);
            Plugin.Instance.movementLogger.AddActions(player.myCharacter, actions.ToArray());
        }
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
            if (!__instance.jumpAttacking)
            {
                if (!__instance.myPhysics.grounded || (___forceJumpAttack && !__instance.myInfo.canJump) || __instance.ridingSpinner) actions.Add(JumpAttack);
                else if (__instance.running) actions.Add(DashAttack);
            }
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
        __instance.CastHookshotRaycast();
        HashSet<PlayerAction> actions = new() { Hookshot };
        if (__instance.hookshotClimbing) actions.Add(HookshotWhileHanging);
        if (__instance.meleeCharging) actions.Add(Telehook);

        bool superLongHookshot = false;
        CameraSystem camera = PseudoSingleton<CameraSystem>.instance;
        if (__instance.currentWallHit.collider != null)
        {
            Vector2 point = __instance.currentWallHit.point + Vector2.up * (__instance.myPhysics.globalHeight + 1f);
            if (!camera.PositionInsideCamera(point, -1f)) actions.Add(LongHookshot);
            if (!camera.PositionInsideCamera(point, 1f)) actions.Add(DoubleHookshot);
            if (!camera.PositionInsideCamera(point, 1.7f)) superLongHookshot = true;
        }
        foreach (Transform transform in camera.targetsList) if (transform.GetComponentInParent<ShurikenController>() != null)
            {
                actions.Add(ShurikenHookshot);
                if (superLongHookshot) actions.Add(HookshotStraightIntoMyPantsDaddy);
                break;
            }

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
    public static void LogEarlyJump(BasicCharacterController __instance, float impulseStrength)
    {
        if (impulseStrength == 0 && !__instance.climbingDash && !__instance.upwardAttack)
        {
            if (__instance.hookshotClimbing)
            {
                if (__instance.GetComponentInParent<MovingDrone>() == null) Plugin.Instance.movementLogger.AddActions(__instance, JumpWhileHanging);
            }
            else if (!__instance.myPhysics.grounded &&
                    !__instance.climbing &&
                    !__instance.climbingDash &&
                    !__instance.wallKicked &&
                    (!__instance.jumpedWhileRiddingSpinner || __instance.myPhysics.height != 1f))
                Plugin.Instance.movementLogger.AddActions(__instance, CoyoteJump);
        }
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.DashCoroutine)), HarmonyPostfix]
    public static void LogLateJump(BasicCharacterController __instance, float impulseStrength, ref IEnumerator __result)
    {
        __result = MovementLogger.GetJumpLoggingEnumerator(__result, __instance, impulseStrength);
    }

    public static IEnumerator GetJumpLoggingEnumerator(IEnumerator original, BasicCharacterController character, float impulseStrength)
    {
        for (int i = 0; original.MoveNext(); i++)
        {
            if (i == 1)
            {
                if (impulseStrength == 0 && !character.climbingDash)
                {
                    if (!character.upwardAttack)
                    {
                        HashSet<PlayerAction> actions = new() { Dodge, Jump };
                        if (character.jumpedWhileRiddingSpinner)
                        {
                            actions.Add(JumpOffSpinner);
                            if (character.axis == Vector3.zero) actions.Add(JumpUpOffSpinner);
                        }
                        if (character.wallJumping) actions.Add(WallJump);
                        if (character.axis == Vector3.zero) actions.Add(JumpUp);
                        Plugin.Instance.movementLogger.AddActions(character, actions.ToArray());
                    }
                    else if (character.wallJumping) Plugin.Instance.movementLogger.AddActions(character, Jump, ClimbSlash);
                }
            }
            yield return original.Current;
        }
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.RollCoroutine)), HarmonyPrefix]
    public static void LogDodge(BasicCharacterController __instance)
    {
        HashSet<PlayerAction> actions = new() { Dodge };
        if (__instance.jumpedWhileRiddingSpinner) actions.Add(DodgeOffSpinner);
        if (!__instance.myPhysics.grounded && (!__instance.jumpedWhileRiddingSpinner || __instance.myPhysics.height != 1f)) actions.Add(CoyoteJump);
        Plugin.Instance.movementLogger.AddActions(__instance, actions.ToArray());
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

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.HoleCoroutine)), HarmonyPrefix]
    public static void LogRespawn(BasicCharacterController __instance)
    {
        if (__instance.myPhysics.currentElevatedGround != null && (!__instance.myPhysics.currentElevatedGround.hole || __instance.myPhysics.currentElevatedGround.GetComponent<HoleTeleporter>() == null))
            Plugin.Instance.movementLogger.AddActions(__instance, Respawn);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StartRidingMecha)), HarmonyPrefix]
    public static void LogHailee(BasicCharacterController __instance)
    {
        Plugin.Instance.movementLogger.AddActions(__instance, Hailee);
    }

    [HarmonyPatch(typeof(FactoryButton), nameof(FactoryButton.PressedByMecha)), HarmonyPrefix]
    public static void LogHaileeButton(FactoryButton __instance)
    {
        Plugin.Instance.movementLogger.AddActions(__instance.transform.position, HaileeButton);
    }

    [HarmonyPatch(typeof(MechaController), nameof(MechaController.SetPushSettings)), HarmonyPrefix]
    public static void LogHaileePush(MechaController __instance, bool ___canPush)
    {
        if (___canPush && __instance.axis != Vector3.zero)
        {
            ElevatedGround block = __instance.myPhysics.currentPushableObject;
            if (block != null && block.pushable && block.myPushablePhysics != null && block.myPushablePhysics.onlyPushableByMecha)
                Plugin.Instance.movementLogger.AddActions(__instance, HaileePush);
        }
    }

    [HarmonyPatch(typeof(MechaController), nameof(MechaController.Fire)), HarmonyPrefix]
    public static void LogHaileeMissile(MechaController __instance, float ___lastFire, BasicCharacterController ___myPlayer)
    {
        if (Time.time - ___lastFire >= 0.2f && !___myPlayer.staminaDrained) Plugin.Instance.movementLogger.AddActions(__instance, HaileeMissile);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.Update)), HarmonyPostfix]
    public static void LogStandOnUnclimbableGround(BasicCharacterController __instance)
    {
        ElevatedGround ground = __instance.myPhysics.currentElevatedGround;
        if (__instance.myPhysics.grounded && ground != null)
        {
            HashSet<PlayerAction> actions = new();

            if (ground.infinityWall || ground.impossibleToGrab) actions.Add(StandOnUnclimbableGround);
            if (ground.GetComponentInParent<MetalScrapOre>() != null) actions.Add(StandOnMaterialCrystal);
            if (ground.GetComponentInChildren<RockBlock>() != null) actions.Add(StandOnRockBlock);
            if (ground.GetComponent<Handcar>() != null) actions.Add(StandOnMinecart);
            if (ground.GetComponentInParent<TrainBarrier>() != null) actions.Add(StandOnBarrier);

            Plugin.Instance.movementLogger.AddActions(__instance, actions.ToArray());
        }
    }

    // --------------------- STATES --------------------- //

    [HarmonyPatch(typeof(MetalScrapOre), nameof(MetalScrapOre.Start)), HarmonyPostfix]
    public static void LogOreStart(MetalScrapOre __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        Vector3 position = __instance.transform.position;
        if (PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings.Contains(__instance.GetOreCode()))
        {
            logger.AddRoomStates(position, logger.GetMetalScrapOreStateID(__instance, false));
        }
        else logger.AddRoomStates(position, logger.GetMetalScrapOreStateID(__instance, true));
    }

    [HarmonyPatch(typeof(MetalScrapOre), nameof(MetalScrapOre.Destroyed)), HarmonyPostfix]
    public static void LogOreDestroy(MetalScrapOre __instance)
    {
        string oreCode = __instance.GetOreCode();
        string state = string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), oreCode, "Absent");
        string location = string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), SceneManager.GetActiveScene().name, oreCode);
        Plugin.Instance.movementLogger.SetLocation(SceneManager.GetActiveScene().name, location, PseudoSingleton<PlayersManager>.instance.players[0].myCharacter.transform.position, true, false);
        Plugin.Instance.movementLogger.AddRoomStates(PseudoSingleton<PlayersManager>.instance.players[0].myCharacter.transform.position, state);
    }

}
