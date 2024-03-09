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
        else
        {
            this.RemoveTemporaryGameStates();
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

    public IEnumerator AppendCodeToEnumerator(IEnumerator original, Action action)
    {
        while (original.MoveNext()) yield return original.Current;
        action.Invoke();
    }

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

    public void RemoveTemporaryGameStates()
    {
        this.RemoveStates(this.GetCameraPos(), "",
            IDs.GetHighwaysPoleStateID(true),
            IDs.GetHighwaysPoleStateID(false),
            IDs.GetMuseumLightStateID(true),
            IDs.GetMuseumLightStateID(false));
    }

}
