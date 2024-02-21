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
using System.Runtime.CompilerServices;

namespace dev.gmeister.unsighted.randomeister.logger;

[Harmony]
public class MovementLogger : IDisposable
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

    private static readonly List<string> loggedNonShopNPCs = new() { "BlacksmithNPC", "OlgaNPC", "ElisaNPC", "ClaraNPC", "GrimReaperNPC" };

    public bool announce;
    public bool uniqueAnnouncements;
    public bool log;
    public float lastAnnouncementTime;
    public float announcementDelay;
    public float cameraPadding;
    public List<Announcement> announcements;

    public Logger nodeLogger;
    public Logger edgeLogger;
    public Logger stateLogger;

    public Dictionary<int, MovementNode> nodes;
    public List<MovementEdge> edges;
    public Dictionary<int, MovementState> states;
    public int largestNodeID;
    public int largestStateID;
    public MovementNode currentNode;

    private readonly HashSet<PlayerAction> actions;
    private readonly HashSet<MovementState> currentStates;
    private bool changingScene;
    private float gameTime;
    private float realTime;

    private readonly HashSet<PlayerAction> silentActions = new() { Walk, Run, StaminaRecharge, Attack, DashAttack, SpinAttack, Parry, SpinnerAttack, JumpOffSpinner, Grind, JumpUp };

    public MovementLogger(string nodePath, string edgePath, string statePath)
    {
        this.actions = new();
        this.currentStates = new();
        this.changingScene = false;

        this.announce = false;
        this.log = false;
        this.uniqueAnnouncements = true;
        this.lastAnnouncementTime = float.MinValue;
        this.announcements = new();
        this.announcementDelay = 0.33333f;
        this.cameraPadding = -4f;

        this.currentNode = null;

        this.InitLoggers(nodePath, edgePath, statePath);
    }

    public void Dispose()
    {
        this.nodeLogger.Dispose();
        this.edgeLogger.Dispose();
        this.stateLogger.Dispose();
    }

    public void InitLoggers(string nodePath, string edgePath, string statePath)
    {
        this.nodes = new();
        this.edges = new();
        this.states = new();

        this.largestStateID = 0;
        this.largestNodeID = 0;

        if (File.Exists(statePath))
        {
            List<string> lines = new(File.ReadAllLines(statePath));

            List<string> headers = null;
            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                {
                    if (headers == null) headers = new(line.Split('\t'));
                    else
                    {
                        List<string> split = new(line.Split('\t'));
                        int id = int.Parse(split[headers.IndexOf("id")]);
                        MovementState state = new(id, split[headers.IndexOf("name")], split[headers.IndexOf("scene")]);
                        this.states.Add(state.id, state);
                    }
                }
            }

            this.stateLogger = new(statePath);
            foreach (MovementState state in this.states.Values) if (state.id > this.largestStateID) this.largestStateID = state.id;
        }
        else
        {
            Logger logger = new(statePath);
            logger.stream.WriteLine(string.Join("\t", "id", "name", "scene"));
            logger.stream.Flush();

            this.stateLogger = logger;
        }

        if (File.Exists(nodePath))
        {
            List<string> lines = new(File.ReadAllLines(nodePath));

            List<string> headers = null;
            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                {
                    if (headers == null) headers = new(line.Split('\t'));
                    else
                    {
                        List<string> split = new(line.Split('\t'));

                        int id = int.Parse(split[headers.IndexOf("id")]);
                        MovementNode node = new(id, split[headers.IndexOf("scene")], split[headers.IndexOf("location")]);

                        string actionsString = split[headers.IndexOf("actions")];
                        if (!string.IsNullOrEmpty(actionsString))
                        {
                            List<string> actionsSplit = new(actionsString.Split(','));
                            foreach (string action in actionsSplit) if (Enum.IsDefined(typeof(PlayerAction), action)) node.actions.Add((PlayerAction)Enum.Parse(typeof(PlayerAction), action));
                        }

                        string statesString = split[headers.IndexOf("states")];
                        if (!string.IsNullOrEmpty(statesString))
                        {
                            List<string> statesSplit = new(statesString.Split(','));
                            foreach (string state in statesSplit) if (int.TryParse(state, out int stateInt)) node.states.Add(stateInt);
                        }

                        this.nodes.Add(node.id, node);
                    }
                }
            }

            this.nodeLogger = new(nodePath);
            foreach (MovementNode node in this.nodes.Values) if (node.id > this.largestNodeID) this.largestNodeID = node.id;
        }
        else
        {
            Logger logger = new(nodePath);
            logger.stream.WriteLine(string.Join("\t", "id", "scene", "location", "actions", "states"));
            logger.stream.Flush();

            this.nodeLogger = logger;
        }

        if (!File.Exists(edgePath))
        {
            Logger logger = new(edgePath);
            logger.stream.WriteLine(string.Join("\t", "source", "target", "actions", "states", "scene change", "real time", "game time"));
            logger.stream.Flush();

            this.edgeLogger = logger;
        }
        else this.edgeLogger = new(edgePath);

    }

    public MovementNode GetNode(string scene, string location, HashSet<PlayerAction> actions = null, HashSet<MovementState> states = null)
    {
        actions ??= new();
        states ??= new();

        bool intermediate = actions.Count > 0 || states.Count > 0;
        HashSet<int> stateIds = new(states.Select(s => s.id).ToList());

        MovementNode node = this.nodes.Values.ToList().Find(n => n.scene == scene && n.location == location && (!intermediate || (n.actions.SetEquals(actions) && n.states.SetEquals(stateIds))));
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
                    foreach (MovementState state in states) node.states.Add(state.id);
                    statesString = string.Join(",", states.Select(s => s.scene + Constants.ID_SEPARATOR + s.name));
                }
            }
            this.nodes.Add(node.id, node);

            this.nodeLogger.stream.WriteLine(string.Join("\t", node.id, node.scene, node.location, actionsString, statesString));
            this.nodeLogger.stream.Flush();
        }

        return node;
    }

    public MovementState GetState(string name, string scene = "")
    {
        MovementState state = this.states.Values.ToList().Find(s => s.name == name && s.scene == scene);
        if (state == null)
        {
            this.largestStateID++;
            state = new MovementState(this.largestStateID, name, scene);
            this.states[this.largestStateID] = state;

            this.stateLogger.stream.WriteLine(string.Join("\t", state.id, state.name, state.scene));
            this.stateLogger.stream.Flush();
        }

        return state;
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

    public void LogMovement(MovementNode node, Vector3 position, bool sceneChange, float realTime, float gameTime)
    {
        if (realTime < 0) realTime = 0;
        if (gameTime < 0) gameTime = 0;

        ColorNames colour = ColorNames.Yellow;
        if (this.currentNode != null && (this.currentNode.scene != node.scene || this.currentNode.location != node.location))
        {
            colour = ColorNames.Green;
            if (this.log)
            {
                MovementEdge edge = new(this.currentNode.id, node.id, sceneChange, (realTime - this.realTime), (gameTime - this.gameTime));
                this.edges.Add(edge);
                if (!sceneChange)
                {
                    foreach (PlayerAction action in this.actions) edge.actions.Add(action);
                    foreach (MovementState state in this.currentStates) edge.states.Add(state.id);
                }

                string states = string.Join(",", this.currentStates.Select(s => s.id).ToArray());
                string actions = string.Join(",", this.actions.ToArray());
                string realTimeDuration = (realTime - this.realTime).ToString();
                string gameTimeDuration = (gameTime - this.gameTime).ToString();

                this.edgeLogger.stream.WriteLine(string.Join("\t", edge.source, edge.target, actions, states, edge.sceneChange ? "1" : "0", edge.realTime, edge.gameTime));
                this.edgeLogger.stream.Flush();
            }
        }

        if (this.announce)
        {
            List<string> locationParts = node.GetStringID().Split(Constants.ID_SEPARATOR).ToList();
            string announcement = locationParts.Select(s => Strings.ReplaceSpecialCharsInPascal(s)).Join(delimiter: ", ");
            this.announcements.Add(new(announcement, colour, position));
        }
    }

    public void SetLocation(string scene, string location, Vector3 position, bool intermediate, bool changingScene)
    {
        float realTime = Time.realtimeSinceStartup;
        GameplayTime gameplayTime = PseudoSingleton<Helpers>.instance.GetCurrentTimeData();
        float gameTime = gameplayTime.hours * 60 * 60 + gameplayTime.minutes * 60 + gameplayTime.seconds;
        if (realTime < 0) realTime = 0;
        if (gameTime < 0) gameTime = 0;

        MovementNode target;
        if (intermediate) target = this.GetNode(scene, location, this.actions, this.currentStates);
        else target = this.GetNode(scene, location);

        this.LogMovement(target, position, this.changingScene, realTime, gameTime);

        if (this.log) this.currentNode = target;
        else this.currentNode = null;

        this.SetChangingScene(changingScene);

        this.gameTime = gameTime;
        this.realTime = realTime;

        if (!intermediate)
        {
            this.actions.Clear();
        }
    }

    public void ClearLocation()
    {
        this.currentNode = null;
    }

    public void SetChangingScene(bool changingScene)
    {
        if (this.changingScene && !changingScene && this.currentNode != null) this.RemoveRoomStatesInverse(this.GetCameraPos(), this.currentNode.scene);
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
                announcements.Add(Strings.ReplaceSpecialCharsInPascal(action.ToString()));
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

    public void AddStates(Vector3 position, string scene, params string[] states)
    {
        foreach (string name in states)
        {
            MovementState state = this.GetState(name, scene);
            if (!this.currentStates.Contains(state))
            {
                this.currentStates.Add(state);
                this.announcements.Add(new(Strings.ReplaceSpecialCharsInPascal(state.GetStringID()), ColorNames.Orange, position));
            }
        }
    }

    public void RemoveStates(Vector3 position, string scene, params string[] states)
    {
        foreach (string name in states)
        {
            MovementState state = this.GetState(name, scene);
            if (this.currentStates.Contains(state))
            {
                this.currentStates.Remove(state);
                this.announcements.Add(new(Strings.ReplaceSpecialCharsInPascal(state.GetStringID()), ColorNames.Blue, position));
            }
        }
    }

    public void RemoveRoomStates(Vector3 position, string scene = "")
    {
        List<MovementState> toRemove = new();
        foreach (MovementState state in this.currentStates)
        {
            if (state.scene != "" && (string.IsNullOrEmpty(scene) || state.scene == scene))
            {
                toRemove.Add(state);
                this.announcements.Add(new(Strings.ReplaceSpecialCharsInPascal(state.GetStringID()), ColorNames.Blue, position));
            }
        }

        foreach (MovementState state in toRemove) this.currentStates.Remove(state);
    }

    public void RemoveRoomStatesInverse(Vector3 position, string scene)
    {
        List<MovementState> toRemove = new();
        foreach (MovementState state in this.currentStates)
        {
            if (state.scene != "" && state.scene != scene)
            {
                toRemove.Add(state);
                this.announcements.Add(new(Strings.ReplaceSpecialCharsInPascal(state.GetStringID()), ColorNames.Blue, position));
            }
        }

        foreach (MovementState state in toRemove) this.currentStates.Remove(state);
    }

    public Vector3 GetCameraPos()
    {
        Vector3 pos = PseudoSingleton<CameraSystem>.instance.myTransform.position;
        pos.z = 0;
        return pos;
    }

    public Vector3 GetPositionInCamera(Vector3 pos)
    {
        CameraSystem cameraSystem = PseudoSingleton<CameraSystem>.instance;
        if (!cameraSystem.PositionInsideCamera(pos, this.cameraPadding)) return this.GetCameraPos();
        else return pos;
    }

    // ------------------------- ROOM CHANGES --------------------- //

    public IEnumerator AddLocationChangeToEnumerator(IEnumerator original, string scene, string location, Vector3 position, bool intermediate, bool changingScene)
    {
        while (original.MoveNext()) yield return original.Current;
        Plugin.Instance.movementLogger.SetLocation(scene, location, position, intermediate, changingScene);
        MovementLogger.PollActions();
    }

    [HarmonyPatch(typeof(NewGamePopup), nameof(NewGamePopup.NewGameCoroutine)), HarmonyPrefix]
    public static void ResetLocationOnNewGame()
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        logger.ClearLocation();
        logger.currentStates.Clear();
    }

    [HarmonyPatch(typeof(LabCutscene1), nameof(LabCutscene1.AfterCutscene)), HarmonyPrefix]
    public static void LogNewGame()
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;

        string location = IDs.GetNewGameID();
        logger.SetLocation(scene, location, logger.GetCameraPos(), false, false);
    }

    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.LoadGameCoroutine)), HarmonyPrefix]
    public static void ResetLocationOnLoadGame()
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        logger.ClearLocation();
        logger.currentStates.Clear();
    }

    [HarmonyPatch(typeof(LevelController), nameof(LevelController.FinishRestartingPlayers)), HarmonyPostfix]
    public static void LogExitCheckpoint(ref IEnumerator __result)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;

        PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();

        if (data.lastTerminalData == null || string.IsNullOrEmpty(data.lastTerminalData.areaName))
        {
            TemporaryCheckpointLocation checkpoint = data.lastCheckpoint;
            string location = IDs.GetCheckpointID(checkpoint);
            __result = logger.AddLocationChangeToEnumerator(__result, scene, location, checkpoint.position, false, false);
        }
    }

    [HarmonyPatch(typeof(InteractionTrigger), nameof(InteractionTrigger.PlayersEnteredMyTrigger)), HarmonyPrefix]
    public static void LogEnterInteractionTrigger(InteractionTrigger __instance)
    {
        if (!ScreenTransition.playerTransitioningScreens)
        {
            MovementLogger logger = Plugin.Instance.movementLogger;
            string scene = SceneManager.GetActiveScene().name;

            ItemChest chest = __instance.messageReciever.GetComponent<ItemChest>();
            if (chest != null) logger.SetLocation(scene, IDs.GetChestID(chest), chest.transform.position, false, false);

            StoreNPC shop = __instance.messageReciever.GetComponent<StoreNPC>();
            NPCCharacter npc = __instance.messageReciever.GetComponent<NPCCharacter>();
            if (shop != null && shop.npcName != "JoanaNPC") logger.SetLocation(scene, IDs.GetNPCID(shop), shop.transform.position, false, false);
            else if (npc != null && loggedNonShopNPCs.Contains(npc.npcName)) logger.SetLocation(scene, IDs.GetNPCID(npc), npc.transform.position, false, false);

            KeyCard keyCard = __instance.messageReciever.GetComponent<KeyCard>();
            if (keyCard != null) logger.SetLocation(scene, IDs.GetKeyCardID(), keyCard.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(AbilityCollectable), nameof(AbilityCollectable.ItemCollected)), HarmonyPrefix]
    public static void LogCollectAbility(AbilityCollectable __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;
        string location = IDs.GetAbilityCollectableID(__instance);
        logger.SetLocation(scene, location, __instance.transform.position, false, false);
    }

    [HarmonyPatch(typeof(ScreenTransition), nameof(ScreenTransition.PlayerScreenTransition)), HarmonyPrefix]
    public static void LogEnterScreenTransition(ScreenTransition __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string location = IDs.GetScreenTransitionID(__instance);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, logger.GetCameraPos(), false, true);
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
            string location = IDs.GetScreenTransitionID(__instance);

            __result = logger.AddLocationChangeToEnumerator(__result, SceneManager.GetActiveScene().name, location, logger.GetCameraPos(), false, false);
        }
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.PlayersEnteredMyTrigger)), HarmonyPostfix]
    public static void LogEnterTerminalTrigger(Terminal __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;
        logger.SetLocation(scene, IDs.GetTerminalID(), __instance.transform.position, false, false);
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
        string location = IDs.GetHoleID(__instance);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(HoleTeleporter), nameof(HoleTeleporter.Start)), HarmonyPostfix]
    public static void LogExitHole(HoleTeleporter __instance, ref IEnumerator __result)
    {
        if (__instance.GetComponent<ElevatedGround>() == null && HoleTeleporter.fallingDownOnHole && HoleTeleporter.lastHoleID == __instance.holeIndex)
        {
            MovementLogger logger = Plugin.Instance.movementLogger;
            string location = IDs.GetHoleID(__instance);
            __result = logger.AddLocationChangeToEnumerator(__result, SceneManager.GetActiveScene().name, location, __instance.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(Elevator), nameof(Elevator.PlayerScreenTransition)), HarmonyPrefix]
    public static void LogEnterElevator(Elevator __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string location = IDs.GetElevatorID(__instance);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(Elevator), nameof(Elevator.Start)), HarmonyPostfix]
    public static void LogExitElevator(Elevator __instance, ref IEnumerator __result)
    {
        if (__instance.elevatorID == Elevator.lastElevatorID && Elevator.ridingElevator)
        {
            MovementLogger logger = Plugin.Instance.movementLogger;
            string location = IDs.GetElevatorID(__instance);
            __result = logger.AddLocationChangeToEnumerator(__result, SceneManager.GetActiveScene().name, location, __instance.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(CrystalAppear), nameof(CrystalAppear.CollectionCoroutine)), HarmonyPrefix]
    public static void LogEnterCrystal(CrystalAppear __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string location = string.Join(Constants.ID_SEPARATOR.ToString(), SceneManager.GetActiveScene().name, typeof(CrystalAppear), __instance.myBossName);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(CrystalTeleportExit), nameof(CrystalTeleportExit.Start)), HarmonyPostfix]
    public static void LogExitCrystal(CrystalTeleportExit __instance)
    {
        if (CrystalTeleportExit.usingCrystalTeleport)
        {
            string location = string.Join(Constants.ID_SEPARATOR.ToString(), SceneManager.GetActiveScene().name, typeof(CrystalTeleportExit));
            MovementLogger logger = Plugin.Instance.movementLogger;
            logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(SceneChangeLadder), nameof(SceneChangeLadder.LadderCoroutine)), HarmonyPrefix]
    public static void LogEnterLadder(SceneChangeLadder __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string location = IDs.GetLadderID(__instance);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(SceneChangeLadder), nameof(SceneChangeLadder.Start)), HarmonyPrefix]
    public static void LogExitLadder(SceneChangeLadder __instance)
    {
        if (SceneChangeLadder.currentLadder == __instance.name)
        {
            MovementLogger logger = Plugin.Instance.movementLogger;
            string location = IDs.GetLadderID(__instance);
            logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(CraterTowerElevator), nameof(CraterTowerElevator.ElevatorCoroutine)), HarmonyPrefix]
    public static void LogEnterTowerElevator(CraterTowerElevator __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string location = IDs.GetTowerElevatorID(__instance);
        logger.SetLocation(SceneManager.GetActiveScene().name, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(CraterTowerElevator), nameof(CraterTowerElevator.Start)), HarmonyPostfix]
    public static void LogExitTowerElevator(CraterTowerElevator __instance, ref IEnumerator __result)
    {
        if (CraterTowerElevator.currentElevator == __instance.name)
        {
            MovementLogger logger = Plugin.Instance.movementLogger;
            string location = IDs.GetTowerElevatorID(__instance);
            __result = logger.AddLocationChangeToEnumerator(__result, SceneManager.GetActiveScene().name, location, __instance.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(EagleRideTrigger), nameof(EagleRideTrigger.CutsceneCoroutine)), HarmonyPrefix]
    public static void LogEnterEagleFlight(EagleRideTrigger __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;
        string location = IDs.GetEagleExitID(__instance);
        logger.SetLocation(scene, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(Eagle), nameof(Eagle.Start)), HarmonyPostfix]
    public static void LogExitEagleFlight(Eagle __instance, ref IEnumerator __result)
    {
        if (__instance.firstEagle)
        {
            MovementLogger logger = Plugin.Instance.movementLogger;
            string scene = SceneManager.GetActiveScene().name;
            string location = IDs.GetEagleBossEntranceID(__instance);
            __result = logger.AddLocationChangeToEnumerator(__result, scene, location, __instance.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(EaglesController), nameof(EaglesController.Death)), HarmonyPrefix]
    public static void LogEnterEagleBossDeath(EaglesController __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;
        string location = IDs.GetEagleBossExitID(__instance);
        logger.SetLocation(scene, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(AfterEagleBossCutscene), nameof(AfterEagleBossCutscene.Start)), HarmonyPrefix]
    public static void LogSpawnInCrashSite(AfterEagleBossCutscene __instance)
    {
        GlobalGameData data = PseudoSingleton<GlobalGameData>.instance;
        if (!data.currentData.playerDataSlots[data.loadedSlot].dataStrings.Contains("AfterEagleBossCutscene"))
        {
            MovementLogger logger = Plugin.Instance.movementLogger;
            string scene = SceneManager.GetActiveScene().name;
            string location = IDs.GetCrashSiteEntranceID(__instance);
            logger.SetLocation(scene, location, __instance.transform.position, false, false);
        }
    }

    [HarmonyPatch(typeof(EagleBossCrystal), nameof(EagleBossCrystal.CollectionCoroutine)), HarmonyPrefix]
    public static void LogEnterEagleCrystal(EagleBossCrystal __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;
        string location = IDs.GetEagleCrystalID(__instance);
        logger.SetLocation(scene, location, __instance.transform.position, false, true);
    }

    [HarmonyPatch(typeof(EagleBossCrystal), nameof(EagleBossCrystal.AfterCrystalCoroutine)), HarmonyPostfix]
    public static void LogExitEagleCrystal(EagleBossCrystal __instance, ref IEnumerator __result)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;
        string location = IDs.GetEagleCrystalID(__instance);
        __result = logger.AddLocationChangeToEnumerator(__result, scene, location, __instance.transform.position, false, false);
    }

    public static int GetFlashbackRoomLayout(FlashbackRoomController controller)
    {
        if (controller.layout1.activeSelf) return 1;
        else if (controller.layout2.activeSelf) return 2;
        else if (controller.layout3.activeSelf) return 3;
        else if (controller.layout4.activeSelf) return 4;
        else if (controller.layout5.activeSelf) return 5;
        return -1;
    }

    [HarmonyPatch(typeof(FlashbackRoomController), nameof(FlashbackRoomController.Start)), HarmonyPostfix]
    public static void LogEnterFlashbackRoom(FlashbackRoomController __instance, ref IEnumerator __result)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;
        string location = IDs.GetFlashbackRoomEntranceID(GetFlashbackRoomLayout(__instance));
        __result = logger.AddLocationChangeToEnumerator(__result, scene, location, __instance.transform.position, false, false);
    }

    [HarmonyPatch(typeof(FlashbackRoomController), nameof(FlashbackRoomController.ExitCutscene)), HarmonyPrefix]
    public static void LogEnterFlashbackRoom(FlashbackRoomController __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;

        string exitLoc = IDs.GetFlashbackRoomExitID(GetFlashbackRoomLayout(__instance));
        logger.SetLocation(scene, exitLoc, __instance.transform.position, false, true);

        string itemLoc = IDs.GetMeteorCrystalItemLocID(FlashbackRoomController.myBossName);
        logger.SetLocation(scene, itemLoc, __instance.transform.position, false, true);
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
        string scene = SceneManager.GetActiveScene().name;
        Vector3 position = __instance.transform.position;
        if (PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings.Contains(__instance.GetOreCode()))
        {
            logger.AddStates(position, scene, IDs.GetMetalScrapOreStateID(__instance, false));
        }
        else logger.AddStates(position, scene, IDs.GetMetalScrapOreStateID(__instance, true));
    }

    [HarmonyPatch(typeof(MetalScrapOre), nameof(MetalScrapOre.Destroyed)), HarmonyPostfix]
    public static void LogOreDestroy(MetalScrapOre __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;
        Vector3 position = __instance.transform.position;

        logger.SetLocation(scene, IDs.GetMetalScrapOreLocationID(__instance), position, true, false);
        logger.RemoveStates(position, scene, IDs.GetMetalScrapOreStateID(__instance, true));
        logger.AddStates(position, scene, IDs.GetMetalScrapOreStateID(__instance, false));
    }

}
