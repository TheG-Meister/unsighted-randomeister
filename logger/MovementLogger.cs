﻿using dev.gmeister.unsighted.randomeister.core;
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

    public Logger actionLogger;
    public Logger stateLogger;
    public Logger nodeLogger;
    public Logger edgeLogger;
    public Logger objectLogger;

    public Dictionary<PlayerAction, int> actionIDs;
    public List<MovementState> states;
    public List<MovementNode> nodes;
    public List<MovementEdge> edges;
    public HashSet<MovementObject> objects;
    public int largestActionID;
    public int largestStateID;
    public int largestNodeID;
    public MovementNode currentNode;

    private readonly HashSet<PlayerAction> currentActions;
    private readonly HashSet<MovementState> currentStates;
    private bool changingScene;
    private float gameTime;
    private float realTime;

    private readonly HashSet<PlayerAction> silentActions = new() { Walk, Run, StaminaRecharge, Attack, DashAttack, SpinAttack, Parry, SpinnerAttack, JumpOffSpinner, Grind, JumpUp };

    public MovementLogger(string dir)
    {
        this.currentActions = new();
        this.currentStates = new();
        this.changingScene = false;

        this.announce = false;
        this.log = false;
        this.uniqueAnnouncements = true;
        this.lastAnnouncementTime = float.MinValue;
        this.announcements = new();
        this.announcementDelay = 0.2f;
        this.cameraPadding = -4f;

        this.currentNode = null;

        this.InitLoggers(dir);
    }

    public void Dispose()
    {
        this.nodeLogger.Dispose();
        this.edgeLogger.Dispose();
        this.stateLogger.Dispose();
    }

    public void InitLoggers(string dir)
    {
        string actionsPath = Path.Combine(dir, "actions.tsv");
        string statesPath = Path.Combine(dir, "states.tsv");
        string nodesPath = Path.Combine(dir, "nodes.tsv");
        string edgesPath = Path.Combine(dir, "edges.tsv");
        string objectsPath = Path.Combine(dir, "objects.tsv");

        this.actionIDs = new();
        this.states = new();
        this.nodes = new();
        this.edges = new();
        this.objects = new();

        this.largestActionID = -1;
        this.largestStateID = -1;
        this.largestNodeID = -1;

        if (File.Exists(actionsPath))
        {
            List<string> headers = new() { "id", "action" };

            List<List<string>> parsedActionsFile = DelimitedFileReader.ReadDelimitedFile(actionsPath, '\t', headers.ToArray());

            foreach (List<string> parsedAction in parsedActionsFile)
            {
                if (int.TryParse(parsedAction[headers.IndexOf("id")], out int id) && Enum.TryParse(parsedAction[headers.IndexOf("action")], out PlayerAction action))
                {
                    this.actionIDs[action] = id;
                    if (id > this.largestActionID) this.largestActionID = id;
                }
            }

            this.actionLogger = new(actionsPath);
        }
        else
        {
            Logger logger = new(actionsPath);
            logger.stream.WriteLine(string.Join("\t", "id", "action"));
            logger.stream.Flush();

            this.actionLogger = logger;
        }

        if (File.Exists(statesPath))
        {
            List<string> lines = new(File.ReadAllLines(statesPath));

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

                        while (this.states.Count <= state.id) this.states.Add(null);
                        this.states[state.id] = state;

                        if (state.id > this.largestStateID) this.largestStateID = state.id;
                    }
                }
            }

            this.stateLogger = new(statesPath);
        }
        else
        {
            Logger logger = new(statesPath);
            logger.stream.WriteLine(string.Join("\t", "id", "name", "scene"));
            logger.stream.Flush();

            this.stateLogger = logger;
        }

        if (File.Exists(nodesPath))
        {
            List<string> headers = new() { "id", "scene", "location", "x", "y", "height", "actions", "states" };

            List<List<string>> parsedNodesFile = DelimitedFileReader.ReadDelimitedFile(nodesPath, '\t', headers.ToArray());

            foreach (List<string> parsedNode in parsedNodesFile)
            {
                int id = int.Parse(parsedNode[headers.IndexOf("id")]);

                if (!float.TryParse(parsedNode[headers.IndexOf("x")], out float x)) x = -3E38f;
                if (!float.TryParse(parsedNode[headers.IndexOf("y")], out float y)) y = -3E38f;
                if (!float.TryParse(parsedNode[headers.IndexOf("height")], out float height)) height = -3E38f;

                MovementNode node = new(id, parsedNode[headers.IndexOf("scene")], parsedNode[headers.IndexOf("location")], new Vector3(x, y, height));

                string actionsString = parsedNode[headers.IndexOf("actions")];
                if (!string.IsNullOrEmpty(actionsString))
                {
                    List<string> actionsSplit = new(actionsString.Split(','));
                    foreach (string action in actionsSplit) if (int.TryParse(action, out int actionID)) node.actions.Add(this.GetAction(actionID));
                }

                string statesString = parsedNode[headers.IndexOf("states")];
                if (!string.IsNullOrEmpty(statesString))
                {
                    List<string> statesSplit = new(statesString.Split(','));
                    foreach (string state in statesSplit) if (int.TryParse(state, out int stateID)) node.states.Add(this.states[stateID]);
                }

                while (this.nodes.Count <= node.id) this.nodes.Add(null);
                this.nodes[node.id] = node;

                if (node.id > this.largestNodeID) this.largestNodeID = node.id;
            }

            this.nodeLogger = new(nodesPath);
        }
        else
        {
            Logger logger = new(nodesPath);
            logger.stream.WriteLine(string.Join("\t", "id", "scene", "location", "x", "y", "height", "actions", "states"));
            logger.stream.Flush();

            this.nodeLogger = logger;
        }

        if (!File.Exists(edgesPath))
        {
            Logger logger = new(edgesPath);
            logger.stream.WriteLine(string.Join("\t", "source", "target", "actions", "states", "scene change", "real time", "game time", "timestamp"));
            logger.stream.Flush();

            this.edgeLogger = logger;
        }
        else this.edgeLogger = new(edgesPath);

        if (File.Exists(objectsPath))
        {
            List<string> headers = new() { "type", "scene", "name", "x", "y", "height" };

            List<List<string>> parsedObjectsFile = DelimitedFileReader.ReadDelimitedFile(objectsPath, '\t', headers.ToArray());

            foreach (List<string> parsedObject in parsedObjectsFile)
            {
                MovementObject obj = new(parsedObject[headers.IndexOf("type")], parsedObject[headers.IndexOf("scene")], parsedObject[headers.IndexOf("name")]);

                if (!float.TryParse(parsedObject[headers.IndexOf("x")], out float x)) x = -3E38f;
                if (!float.TryParse(parsedObject[headers.IndexOf("y")], out float y)) y = -3E38f;
                if (!float.TryParse(parsedObject[headers.IndexOf("height")], out float height)) height = -3E38f;
                obj.position = new Vector3(x, y, height);

                this.objects.Add(obj);
            }

            this.objectLogger = new(objectsPath);
        }
        else
        {
            Logger logger = new(objectsPath);
            logger.stream.WriteLine(string.Join("\t", "type", "scene", "name", "x", "y", "height"));
            logger.stream.Flush();

            this.objectLogger = logger;
        }

    }

    public MovementNode GetNode(string scene, string location, Vector3 position, HashSet<PlayerAction> actions = null, HashSet<MovementState> states = null)
    {
        actions ??= new();
        states ??= new();

        bool intermediate = actions.Count > 0 || states.Count > 0;

        MovementNode node = this.nodes.ToList().Find(n => n.scene == scene && n.location == location && (!intermediate || (n.actions.SetEquals(actions) && n.states.SetEquals(states))));
        if (node == null)
        {
            this.largestNodeID++;
            node = new MovementNode(this.largestNodeID, scene, location, position);
            string actionsString = "";
            string statesString = "";

            if (intermediate)
            {
                if (actions.Count > 0)
                {
                    foreach (PlayerAction action in actions) node.actions.Add(action);
                    actionsString = string.Join(",", actions.Select(a => this.GetActionID(a)));
                }
                if (states.Count > 0)
                {
                    foreach (MovementState state in states) node.states.Add(state);
                    statesString = string.Join(",", states.Select(s => s.id));
                }
            }

            while (this.nodes.Count <= node.id) this.nodes.Add(null);
            this.nodes[node.id] = node;

            this.nodeLogger.stream.WriteLine(string.Join("\t", node.id, node.scene, node.location, node.position.x, node.position.y, node.position.z, actionsString, statesString));
            this.nodeLogger.stream.Flush();
        }

        return node;
    }

    public MovementState GetState(string name, string scene = "")
    {
        MovementState state = this.states.ToList().Find(s => s.name == name && s.scene == scene);
        if (state == null)
        {
            this.largestStateID++;
            state = new MovementState(this.largestStateID, name, scene);

            while (this.states.Count <= state.id) this.states.Add(null);
            this.states[this.largestStateID] = state;

            this.stateLogger.stream.WriteLine(string.Join("\t", state.id, state.name, state.scene));
            this.stateLogger.stream.Flush();
        }

        return state;
    }

    public int GetActionID(PlayerAction action)
    {
        if (!this.actionIDs.ContainsKey(action))
        {
            this.largestActionID++;
            this.actionIDs[action] = this.largestActionID;
            this.actionLogger.stream.WriteLine(string.Join("\t", this.largestActionID, action));
            this.actionLogger.stream.Flush();
            return this.largestActionID;
        }
        else return this.actionIDs[action];
    }

    public PlayerAction GetAction(int id)
    {
        if (!this.actionIDs.ContainsValue(id)) throw new Exception("There is no action corresponding to this ID");
        return this.actionIDs.First(k => k.Value == id).Key;
    }

    public void LogObject(GameObject obj, string name)
    {
        this.LogObject(obj.GetType().Name, obj.scene.name, name, this.Get3DObjectPosition(obj));
    }

    public void LogObject(string type, string scene, string name, Vector3 position)
    {
        MovementObject obj = new(type, scene, name, position);
        if (!this.objects.Contains(obj))
        {
            this.objects.Add(obj);
            if (this.log)
            {
                this.objectLogger.stream.WriteLine(string.Join("\t", obj.type, obj.scene, obj.name, obj.position.x, obj.position.y, obj.position.z));
                this.objectLogger.stream.Flush();
            }
        }
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

    public void LogMovement(MovementNode node, Vector3 position, bool sceneChange, float realTime, float gameTime, long timestamp)
    {
        if (realTime < 0) realTime = 0;
        if (gameTime < 0) gameTime = 0;

        ColorNames colour = ColorNames.Yellow;
        if (this.currentNode != null && (this.currentNode.scene != node.scene || this.currentNode.location != node.location))
        {
            colour = ColorNames.Green;
            if (this.log)
            {
                MovementEdge edge = new(this.currentNode.id, node.id, sceneChange, (realTime - this.realTime), (gameTime - this.gameTime), timestamp);
                this.edges.Add(edge);
                if (!sceneChange)
                {
                    foreach (PlayerAction action in this.currentActions) edge.actions.Add(action);
                    foreach (MovementState state in this.currentStates) edge.states.Add(state);
                }

                string states = string.Join(",", edge.states.Select(s => s.id));
                string actions = string.Join(",", edge.actions.Select(a => this.GetActionID(a)));
                string realTimeDuration = (realTime - this.realTime).ToString();
                string gameTimeDuration = (gameTime - this.gameTime).ToString();

                this.edgeLogger.stream.WriteLine(string.Join("\t", edge.source, edge.target, actions, states, edge.sceneChange ? "1" : "0", edge.realTime, edge.gameTime, edge.timestamp));
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

    public void SetLocation(GameObject obj, string location, bool intermediate, bool changingScene)
    {
        this.SetLocation(obj.scene.name, location, this.Get3DObjectPosition(obj), intermediate, changingScene);
    }

    public void SetLocation(string location, Vector3 position, bool intermediate, bool changingScene)
    {
        this.SetLocation(SceneManager.GetActiveScene().name, location, position, intermediate, changingScene);
    }

    public void SetLocation(string scene, string location, Vector3 position, bool intermediate, bool changingScene)
    {
        float realTime = Time.realtimeSinceStartup;
        GameplayTime gameplayTime = PseudoSingleton<Helpers>.instance.GetCurrentTimeData();
        float gameTime = gameplayTime.hours * 60 * 60 + gameplayTime.minutes * 60 + gameplayTime.seconds;
        if (realTime < 0) realTime = 0;
        if (gameTime < 0) gameTime = 0;

        MovementNode target;
        //if (intermediate) target = this.GetNode(scene, location, position, this.currentActions, this.currentStates);
        target = this.GetNode(scene, location, position);

        this.LogMovement(target, position, this.changingScene, realTime, gameTime, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());

        if (this.log) this.currentNode = target;
        else this.currentNode = null;

        if (changingScene || this.changingScene) this.currentActions.Clear();

        this.SetChangingScene(changingScene);
        if (!changingScene)
        {
            this.PollActions();
            this.PollStates();
        }

        this.gameTime = gameTime;
        this.realTime = realTime;
    }

    public void ClearLocation()
    {
        this.currentNode = null;
    }

    public void Reset()
    {
        this.ClearLocation();
        this.currentStates.Clear();
        this.currentActions.Clear();
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
            if (!this.uniqueAnnouncements || !this.currentActions.Contains(action))
            {
                this.currentActions.Add(action);
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
        this.AddActions(controller.transform.position + Vector3.up * (controller.myPhysics.globalHeight + controller.myPhysics.Zsize * 1.55f), actions);
    }

    public void AddActions(MechaController controller, params PlayerAction[] actions)
    {
        this.AddActions(controller.transform.position + Vector3.up * (controller.myPhysics.globalHeight), actions);
    }

    public void AddStates(GameObject obj, params string[] states) => this.AddStates(this.Get3DLogPosition(obj), obj.scene.name, states);

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

    public void RemoveStates(GameObject obj, params string[] states) => this.RemoveStates(this.Get3DLogPosition(obj), obj.scene.name, states);

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

    public Vector3 GetPlayerPos()
    {
        BasicCharacterController controller = PseudoSingleton<PlayersManager>.instance.players[0].myCharacter;
        return controller.transform.position + Vector3.up * (controller.myPhysics.globalHeight + controller.myPhysics.Zsize * 1.55f);
    }

    public Vector3 GetPositionInCamera(Vector3 pos)
    {
        CameraSystem cameraSystem = PseudoSingleton<CameraSystem>.instance;
        if (!cameraSystem.PositionInsideCamera(pos, this.cameraPadding)) return this.GetCameraPos();
        else return pos;
    }

    public Vector3 Get3DObjectPosition(GameObject obj)
    {
        SortingObject sort = obj.GetComponentInChildren<SortingObject>();
        if (sort != null) return new Vector3(obj.transform.position.x, obj.transform.position.y, sort.globalHeight);
        else return new Vector3(obj.transform.position.x, obj.transform.position.y, 0);
    }

    public Vector3 Get3DLogPosition(GameObject obj)
    {
        SortingObject sort = obj.GetComponentInChildren<SortingObject>();
        if (sort != null) return new Vector3(obj.transform.position.x, obj.transform.position.y + sort.globalHeight);
        else return new Vector3(obj.transform.position.x, obj.transform.position.y, 0);
    }

    // ------------------------- LOCATION CHANGES --------------------- //

    public IEnumerator AppendCodeToEnumerator(IEnumerator original, Action action)
    {
        while (original.MoveNext()) yield return original.Current;
        action.Invoke();
    }

    [HarmonyPatch(typeof(NewGamePopup), nameof(NewGamePopup.NewGameCoroutine)), HarmonyPrefix]
    public static void ResetLocationOnNewGame()
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.Reset();
    }

    [HarmonyPatch(typeof(LabCutscene1), nameof(LabCutscene1.AfterCutscene)), HarmonyPrefix]
    public static void LogNewGame(LabCutscene1 __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;

        string location = IDs.GetNewGameID();
        logger.SetLocation(__instance.gameObject, location, false, false);
    }

    [HarmonyPatch(typeof(SaveSlotButton), nameof(SaveSlotButton.LoadGameCoroutine)), HarmonyPrefix]
    public static void ResetLocationOnLoadGame()
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.Reset();
    }

    [HarmonyPatch(typeof(LevelController), nameof(LevelController.FinishRestartingPlayers)), HarmonyPostfix]
    public static void LogExitCheckpoint(ref IEnumerator __result)
    {
        MovementLogger logger = Plugin.instance.movementLogger;

        PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();

        if (data.lastTerminalData == null || string.IsNullOrEmpty(data.lastTerminalData.areaName))
        {
            TemporaryCheckpointLocation checkpoint = data.lastCheckpoint;
            string location = IDs.GetCheckpointID(checkpoint);
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                logger.SetLocation(location, checkpoint.position, false, false);
            });
        }
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.PlayersEnteredMyTrigger)), HarmonyPostfix]
    public static void LogEnterTerminalTrigger(Terminal __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        logger.SetLocation(__instance.gameObject, IDs.GetTerminalID(), false, false);
    }

    [HarmonyPatch(typeof(InteractionTrigger), nameof(InteractionTrigger.PlayersEnteredMyTrigger)), HarmonyPrefix]
    public static void LogEnterInteractionTrigger(InteractionTrigger __instance)
    {
        if (!ScreenTransition.playerTransitioningScreens)
        {
            MovementLogger logger = Plugin.instance.movementLogger;

            ItemChest chest = __instance.messageReciever.GetComponent<ItemChest>();
            if (chest != null) logger.SetLocation(chest.gameObject, IDs.GetChestID(chest), false, false);

            StoreNPC shop = __instance.messageReciever.GetComponent<StoreNPC>();
            NPCCharacter npc = __instance.messageReciever.GetComponent<NPCCharacter>();
            if (shop != null && shop.npcName != "JoanaNPC") logger.SetLocation(shop.gameObject, IDs.GetNPCID(shop), false, false);
            else if (npc != null && loggedNonShopNPCs.Contains(npc.npcName)) logger.SetLocation(npc.gameObject, IDs.GetNPCID(npc), false, false);

            KeyCard keyCard = __instance.messageReciever.GetComponent<KeyCard>();
            if (keyCard != null) logger.SetLocation(keyCard.gameObject, IDs.GetKeyCardID(), false, false);

            CraftingTable table = __instance.messageReciever.GetComponent<CraftingTable>();
            if (table != null) logger.SetLocation(table.gameObject, IDs.GetCraftingTableID(table), false, false);

            EagleRideTrigger eagle = __instance.messageReciever.GetComponent<EagleRideTrigger>();
            if (eagle != null) logger.SetLocation(eagle.gameObject, IDs.GetEagleRideTriggerID(eagle), false, false);
        }
    }

    [HarmonyPatch(typeof(AbilityCollectable), nameof(AbilityCollectable.ItemCollected)), HarmonyPrefix]
    public static void LogCollectAbility(AbilityCollectable __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetAbilityCollectableID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, false);
    }

    [HarmonyPatch(typeof(ScreenTransition), nameof(ScreenTransition.PlayerScreenTransition)), HarmonyPrefix]
    public static void LogEnterScreenTransition(ScreenTransition __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetScreenTransitionID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(ScreenTransition), nameof(ScreenTransition.EndPlayerScreenTransition)), HarmonyPostfix]
    public static void LogExitScreenTransition(ScreenTransition __instance, ref IEnumerator __result)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        if (ScreenTransition.playerTransitioningScreens &&
            ScreenTransition.currentDoorName == __instance.gameObject.name &&
            (ScreenTransition.teleportCheat ||
            ScreenTransition.lastSceneName == PseudoSingleton<MapManager>.instance.GetNextRoomName(__instance.myDirection, __instance.triggerID)))
        {
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                string location = IDs.GetScreenTransitionID(__instance);
                logger.SetLocation(__instance.gameObject, location, false, false);
            });
        }
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
        Plugin.instance.movementLogger.SetChangingScene(true);
    }

    [HarmonyPatch(typeof(HoleTeleporter), nameof(HoleTeleporter.FallIntoHole)), HarmonyPrefix]
    public static void LogEnterHole(HoleTeleporter __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetHoleID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(HoleTeleporter), nameof(HoleTeleporter.Start)), HarmonyPostfix]
    public static void LogExitHole(HoleTeleporter __instance, ref IEnumerator __result)
    {
        if (__instance.GetComponent<ElevatedGround>() == null && HoleTeleporter.fallingDownOnHole && HoleTeleporter.lastHoleID == __instance.holeIndex)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetHoleID(__instance);
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                logger.SetLocation(location, new Vector3(__instance.transform.position.x, __instance.transform.position.y, 16f), false, false);
            });
        }
    }

    [HarmonyPatch(typeof(Elevator), nameof(Elevator.PlayerScreenTransition)), HarmonyPrefix]
    public static void LogEnterElevator(Elevator __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetElevatorID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(Elevator), nameof(Elevator.Start)), HarmonyPostfix]
    public static void LogExitElevator(Elevator __instance, ref IEnumerator __result)
    {
        if (__instance.elevatorID == Elevator.lastElevatorID && Elevator.ridingElevator)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetElevatorID(__instance);
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                logger.SetLocation(__instance.gameObject, location, false, false);
            });
        }
    }

    [HarmonyPatch(typeof(CrystalAppear), nameof(CrystalAppear.CollectionCoroutine)), HarmonyPrefix]
    public static void LogEnterCrystal(CrystalAppear __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetCrystalAppearID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(CrystalTeleportExit), nameof(CrystalTeleportExit.Start)), HarmonyPostfix]
    public static void LogExitCrystal(CrystalTeleportExit __instance)
    {
        if (CrystalTeleportExit.usingCrystalTeleport)
        {
            string location = IDs.GetCrystalTeleportExitID(__instance);
            MovementLogger logger = Plugin.instance.movementLogger;
            logger.SetLocation(__instance.gameObject, location, false, false);
        }
    }

    [HarmonyPatch(typeof(SceneChangeLadder), nameof(SceneChangeLadder.LadderCoroutine)), HarmonyPrefix]
    public static void LogEnterLadder(SceneChangeLadder __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetLadderID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(SceneChangeLadder), nameof(SceneChangeLadder.Start)), HarmonyPrefix]
    public static void LogExitLadder(SceneChangeLadder __instance)
    {
        if (SceneChangeLadder.currentLadder == __instance.name)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetLadderID(__instance);
            logger.SetLocation(__instance.gameObject, location, false, false);
        }
    }

    [HarmonyPatch(typeof(CraterTowerElevator), nameof(CraterTowerElevator.ElevatorCoroutine)), HarmonyPrefix]
    public static void LogEnterTowerElevator(CraterTowerElevator __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetTowerElevatorID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(CraterTowerElevator), nameof(CraterTowerElevator.Start)), HarmonyPostfix]
    public static void LogExitTowerElevator(CraterTowerElevator __instance, ref IEnumerator __result)
    {
        if (CraterTowerElevator.currentElevator == __instance.name)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetTowerElevatorID(__instance);
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                logger.SetLocation(__instance.gameObject, location, false, false);
            });
        }
    }

    /*
    [HarmonyPatch(typeof(EagleRideTrigger), nameof(EagleRideTrigger.CutsceneCoroutine)), HarmonyPrefix]
    public static void LogEnterEagleFlight(EagleRideTrigger __instance)
    {
        MovementLogger logger = Plugin.Instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;
        string location = IDs.GetEagleRideTriggerID(__instance);
        logger.SetLocation(scene, location, __instance.transform.position, false, true);
    }
    */

    [HarmonyPatch(typeof(Eagle), nameof(Eagle.Start)), HarmonyPostfix]
    public static void LogExitEagleFlight(Eagle __instance, ref IEnumerator __result)
    {
        if (__instance.firstEagle)
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetEagleBossEntranceID(__instance);
            __result = logger.AppendCodeToEnumerator(__result, () =>
            {
                logger.SetLocation(__instance.gameObject, location, false, false);
            });
        }
    }

    [HarmonyPatch(typeof(EaglesController), nameof(EaglesController.Death)), HarmonyPrefix]
    public static void LogEnterEagleBossDeath(EaglesController __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetEagleBossExitID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(AfterEagleBossCutscene), nameof(AfterEagleBossCutscene.Start)), HarmonyPrefix]
    public static void LogSpawnInCrashSite(AfterEagleBossCutscene __instance)
    {
        GlobalGameData data = PseudoSingleton<GlobalGameData>.instance;
        if (!data.currentData.playerDataSlots[data.loadedSlot].dataStrings.Contains("AfterEagleBossCutscene"))
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetCrashSiteEntranceID(__instance);
            logger.SetLocation(__instance.gameObject, location, false, false);
        }
    }

    [HarmonyPatch(typeof(EagleBossCrystal), nameof(EagleBossCrystal.CollectionCoroutine)), HarmonyPrefix]
    public static void LogEnterEagleCrystal(EagleBossCrystal __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetEagleCrystalID(__instance);
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(EagleBossCrystal), nameof(EagleBossCrystal.AfterCrystalCoroutine)), HarmonyPostfix]
    public static void LogExitEagleCrystal(EagleBossCrystal __instance, ref IEnumerator __result)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetEagleCrystalID(__instance);
        __result = logger.AppendCodeToEnumerator(__result, () =>
        {
            logger.SetLocation(__instance.gameObject, location, false, false);
        });
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
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetFlashbackRoomEntranceID(GetFlashbackRoomLayout(__instance));
        __result = logger.AppendCodeToEnumerator(__result, () =>
        {
            logger.SetLocation(__instance.gameObject, location, false, false);
        });
    }

    [HarmonyPatch(typeof(FlashbackRoomController), nameof(FlashbackRoomController.ExitCutscene)), HarmonyPrefix]
    public static void LogExitFlashbackRoom(FlashbackRoomController __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string scene = SceneManager.GetActiveScene().name;

        string exitLoc = IDs.GetFlashbackRoomExitID(GetFlashbackRoomLayout(__instance));
        logger.SetLocation(__instance.gameObject, exitLoc, false, true);

        string itemLoc = IDs.GetMeteorCrystalItemLocID(FlashbackRoomController.myBossName);
        logger.SetLocation(__instance.gameObject, itemLoc, false, true);
    }

    [HarmonyPatch(typeof(DarkMonsterCraterCutscene), nameof(DarkMonsterCraterCutscene.SkipCutsceneInput)), HarmonyPrefix]
    public static void LogWinDarkMonsterFight(DarkMonsterCraterCutscene __instance)
    {
        MovementLogger.LogExitDarkMonsterFight(__instance);

        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetVanaFlamebladeID();
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(DarkMonsterCraterCutscene), nameof(DarkMonsterCraterCutscene.DeathCutsceneCoroutine)), HarmonyPrefix]
    public static void LogExitDarkMonsterFight(DarkMonsterCraterCutscene __instance)
    {
        MovementLogger logger = Plugin.instance.movementLogger;
        string location = IDs.GetDarkMonsterFightExitID();
        logger.SetLocation(__instance.gameObject, location, false, true);
    }

    [HarmonyPatch(typeof(BlacksmithCutscene), nameof(BlacksmithCutscene.Start)), HarmonyPrefix]
    public static void LogEnterBlacksmithCutscene(BlacksmithCutscene __instance)
    {
        if (!PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings.Contains("BlacksmithCutscene"))
        {
            MovementLogger logger = Plugin.instance.movementLogger;
            string location = IDs.GetBlacksmithCutsceneID();
            logger.SetLocation(__instance.gameObject, location, false, false);
        }
    }

    // ----------------------- ACTIONS -------------------------- //

    public void PollActions()
    {
        List<PlayerInfo> players = PseudoSingleton<PlayersManager>.instance.players;
        foreach (PlayerInfo player in players)
        {
            HashSet<PlayerAction> actions = new();
            if (player.myCharacter.ridingSpinner) actions.Add(Spinner);
            if (player.myCharacter.ridingMecha) actions.Add(Hailee);
            Plugin.instance.movementLogger.AddActions(player.myCharacter, actions.ToArray());
        }
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StaminaChargeCoroutine)), HarmonyPrefix]
    public static void LogStaminaRecharge(BasicCharacterController __instance)
    {
        Plugin.instance.movementLogger.AddActions(__instance, StaminaRecharge);
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
            Plugin.instance.movementLogger.AddActions(__instance, actions.ToArray());
        }
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.MeleeAttackCharge)), HarmonyPrefix]
    public static void LogSpinAttack(BasicCharacterController __instance)
    {
        HashSet<PlayerAction> actions = new() { SpinAttack };
        if (__instance.hookshotFiring) actions.Add(Telehook);
        Plugin.instance.movementLogger.AddActions(__instance, actions.ToArray());
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.GuardCoroutine)), HarmonyPrefix]
    public static void LogParry(BasicCharacterController __instance)
    {
        Plugin.instance.movementLogger.AddActions(__instance, Parry);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.ShurikenCoroutine)), HarmonyPrefix]
    public static void LogShuriken(BasicCharacterController __instance)
    {
        if (!__instance.staminaDrained && __instance.CanThrowShuriken()) Plugin.instance.movementLogger.AddActions(__instance, ShurikenThrow);
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
                    Plugin.instance.movementLogger.AddActions(__instance, ShootBullet);
                    break;
                case "Flamethrower":
                case "Icethrower":
                    Plugin.instance.movementLogger.AddActions(__instance, Spray);
                    break;
                case "IceGranade":
                case "GranadeLauncher":
                case "GranadeShotgun":
                    Plugin.instance.movementLogger.AddActions(__instance, PlayerGrenade);
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(GranadeController), nameof(GranadeController.SpawnExplosion)), HarmonyPrefix]
    public static void LogExplosion(GranadeController __instance, bool ___alreadyExploded)
    {
        if (!___alreadyExploded)
        {
            Plugin.instance.movementLogger.AddActions(__instance.transform.position, Grenade);
        }
    }

    [HarmonyPatch(typeof(ScrapRobotEnemy), nameof(ScrapRobotEnemy.InstantiateGranade)), HarmonyPrefix]
    public static void LogScrapRobotGrenade(ScrapRobotEnemy __instance)
    {
        Plugin.instance.movementLogger.AddActions(__instance.transform.position, ScrapRobotGrenade);
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
                if (elevatedGround.deepWater) Plugin.instance.movementLogger.AddActions(raycaster.transform.position, CreateIceOrRockPlatform);
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
                if (elevatedGround.deepWater) Plugin.instance.movementLogger.AddActions(controller.transform.position, CreateIceOrRockPlatform);
            }
            yield return original.Current;
        }
    }

    [HarmonyPatch(typeof(GranadeController), nameof(GranadeController.FallOnWater)), HarmonyPrefix]
    public static void LogIceGrenadePlatformSpawn(GranadeController __instance)
    {
        if (__instance.iceGranade) Plugin.instance.movementLogger.AddActions(__instance.transform.position, CreateIceOrRockPlatform, PlayerIceGrenade);
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

        Plugin.instance.movementLogger.AddActions(__instance, actions.ToArray());
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StartRidingSpinner)), HarmonyPrefix]
    public static void LogSpinner(BasicCharacterController __instance)
    {
        Plugin.instance.movementLogger.AddActions(__instance, Spinner);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.SpinnerAttack)), HarmonyPrefix]
    public static void LogSpinnerAttack(BasicCharacterController __instance, float ___lastTimeWaterSkip)
    {
        if (Time.time - ___lastTimeWaterSkip >= 0.3f && (__instance.myPhysics.height == 0f || __instance.spinnerGrinding || (__instance.myPhysics.currentElevatedGround != null && __instance.myPhysics.currentElevatedGround.deepWater)))
        {
            HashSet<PlayerAction> actions = new() { SpinnerAttack };
            if (__instance.myPhysics.currentElevatedGround != null && __instance.myPhysics.currentElevatedGround.deepWater && !__instance.spinnerGrinding && (__instance.myPhysics.globalHeight < 0.75f && __instance.myPhysics.heightDelta < 0f && PseudoSingleton<CameraSystem>.instance.PositionInsideCamera(__instance.myAnimations.myAnimator.transform.position, 0f)))
                actions.Add(Skip);
            Plugin.instance.movementLogger.AddActions(__instance, actions.ToArray());
        }
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.CheckIfBeganRidingRail)), HarmonyPrefix]
    public static void LogGrind(BasicCharacterController __instance)
    {
        if (!__instance.spinnerGrinding) Plugin.instance.movementLogger.AddActions(__instance, Grind);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.DestroyBlockWithSpinner)), HarmonyPrefix]
    public static void LogBreakRockWithSpinner(BasicCharacterController __instance)
    {
        ElevatedGround currentWall = __instance.myPhysics.currentWall;
        if (currentWall != null && currentWall.transform.childCount >= 3)
        {
            RockBlock rock = currentWall.transform.GetChild(2).GetComponent<RockBlock>();
            if (rock != null && !rock.isSafeDoor) Plugin.instance.movementLogger.AddActions(__instance, BreakRockWithSpinner);
        }
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.Dash)), HarmonyPrefix]
    public static void LogEarlyJump(BasicCharacterController __instance, float impulseStrength)
    {
        if (impulseStrength == 0 && !__instance.climbingDash && !__instance.upwardAttack)
        {
            if (__instance.hookshotClimbing)
            {
                if (__instance.GetComponentInParent<MovingDrone>() == null) Plugin.instance.movementLogger.AddActions(__instance, JumpWhileHanging);
            }
            else if (!__instance.myPhysics.grounded &&
                    !__instance.climbing &&
                    !__instance.climbingDash &&
                    !__instance.wallKicked &&
                    (!__instance.jumpedWhileRiddingSpinner || __instance.myPhysics.height != 1f))
                Plugin.instance.movementLogger.AddActions(__instance, CoyoteJump);
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
                        Plugin.instance.movementLogger.AddActions(character, actions.ToArray());
                    }
                    else if (character.wallJumping) Plugin.instance.movementLogger.AddActions(character, Jump, ClimbSlash);
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
        Plugin.instance.movementLogger.AddActions(__instance, actions.ToArray());
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.LiftObjectCoroutine)), HarmonyPrefix]
    public static void LogBoxGrab(BasicCharacterController __instance)
    {
        if (__instance.myHoldingObject.breakable) Plugin.instance.movementLogger.AddActions(__instance, GrabBox);
    }

    [HarmonyPatch(typeof(HoldableObject), nameof(HoldableObject.PlacedOnGround)), HarmonyPrefix]
    public static void LogBoxPlace(HoldableObject __instance)
    {
        if (__instance.breakable) Plugin.instance.movementLogger.AddActions(__instance.transform.position, PlaceBox);
    }

    [HarmonyPatch(typeof(HoldableObject), nameof(HoldableObject.ThrownAt)), HarmonyPrefix]
    public static void LogBoxThrow(HoldableObject __instance)
    {
        if (__instance.breakable) Plugin.instance.movementLogger.AddActions(__instance.transform.position, ThrowBox);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.GetDashInput)), HarmonyPrefix]
    public static void LogBoxJump(BasicCharacterController __instance)
    {
        if (!PlayerInfo.cutscene && ButtonSystem.GetKeyDown(PseudoSingleton<GlobalInputManager>.instance.inputData.GetInputProfile(__instance.myInfo.playerNum).dash) && (__instance.holdingObject || __instance.carryingRaquel) && !__instance.myInfo.canJump && ButtonSystem.GetKey(PseudoSingleton<GlobalInputManager>.instance.inputData.GetInputProfile(__instance.myInfo.playerNum).guard, false))
            Plugin.instance.movementLogger.AddActions(__instance, BoxJump);
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
            if (___currentEnemyHitBox != null) Plugin.instance.movementLogger.AddActions(__instance, Wierdshot);
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
            Plugin.instance.movementLogger.AddActions(__instance, Respawn);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.StartRidingMecha)), HarmonyPrefix]
    public static void LogHailee(BasicCharacterController __instance)
    {
        Plugin.instance.movementLogger.AddActions(__instance, Hailee);
    }

    [HarmonyPatch(typeof(FactoryButton), nameof(FactoryButton.PressedByMecha)), HarmonyPrefix]
    public static void LogHaileeButton(FactoryButton __instance)
    {
        Plugin.instance.movementLogger.AddActions(__instance.transform.position, HaileeButton);
    }

    [HarmonyPatch(typeof(MechaController), nameof(MechaController.SetPushSettings)), HarmonyPrefix]
    public static void LogHaileePush(MechaController __instance, bool ___canPush)
    {
        if (___canPush && __instance.axis != Vector3.zero)
        {
            ElevatedGround block = __instance.myPhysics.currentPushableObject;
            if (block != null && block.pushable && block.myPushablePhysics != null && block.myPushablePhysics.onlyPushableByMecha)
                Plugin.instance.movementLogger.AddActions(__instance, HaileePush);
        }
    }

    [HarmonyPatch(typeof(MechaController), nameof(MechaController.Fire)), HarmonyPrefix]
    public static void LogHaileeMissile(MechaController __instance, float ___lastFire, BasicCharacterController ___myPlayer)
    {
        if (Time.time - ___lastFire >= 0.2f && !___myPlayer.staminaDrained) Plugin.instance.movementLogger.AddActions(__instance, HaileeMissile);
    }

    [HarmonyPatch(typeof(BasicCharacterController), nameof(BasicCharacterController.Update)), HarmonyPostfix]
    public static void LogStandOnUnclimbableGround(BasicCharacterController __instance)
    {
        ElevatedGround ground = __instance.myPhysics.currentElevatedGround;
        if (__instance.myPhysics.grounded && ground != null)
        {
            HashSet<PlayerAction> actions = new();

            if (ground.infinityWall) actions.Add(StandOnUnclimbableGround);
            if (ground.impossibleToGrab)
            {
                if (!(ground.gameObject.scene.name == "GardenVillage" && 
                    ground.name == "Collider (2)" &&
                    ground.transform.parent.name == "Bridge")) actions.Add(StandOnUnclimbableGround);
            }
            if (ground.GetComponentInParent<MetalScrapOre>() != null) actions.Add(StandOnMaterialCrystal);
            if (ground.GetComponentInChildren<RockBlock>() != null) actions.Add(StandOnRockBlock);
            if (ground.GetComponent<Handcar>() != null) actions.Add(StandOnMinecart);
            if (ground.GetComponentInParent<TrainBarrier>() != null) actions.Add(StandOnBarrier);

            Plugin.instance.movementLogger.AddActions(__instance, actions.ToArray());
        }
    }

    // --------------------- STATES AND OBJECTS --------------------- //

    public void PollStates()
    {
        PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();
        MapManager mapManager = PseudoSingleton<MapManager>.instance;

        List<string> statesToRemove = new();
        List<string> statesToAdd = new();

        bool prologue = !data.dataStrings.Contains("FinishedFirstPart");
        statesToRemove.Add(IDs.GetPrologueStateID(!prologue));
        statesToAdd.Add(IDs.GetPrologueStateID(prologue));

        /*
        bool museumLightUsed = mapManager.areaName == "Museum" && !mapManager.playerRoom.customLight.useCustomLightColors;
        bool museumLight = data.museumLightsOn;
        statesToRemove.Add(IDs.GetMuseumLightStateID(!museumLight));
        if (museumLightUsed) statesToAdd.Add(IDs.GetMuseumLightStateID(museumLight));
        else statesToRemove.Add(IDs.GetMuseumLightStateID(museumLight));

        bool highways = PseudoSingleton<MapManager>.instance.areaName == "SuburbsRails";
        */

        this.RemoveStates(this.GetPlayerPos(), "", statesToRemove.ToArray());
        this.AddStates(this.GetPlayerPos(), "", statesToAdd.ToArray());
    }

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
        if (!data.dataStrings.Contains(__instance.GetDataString()) && !___activated && hit.gameObject.layer == (int) __instance.collisionLayer && hit.gameObject.name == __instance.targetObject.name)
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

}
