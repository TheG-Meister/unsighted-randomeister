﻿using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementEdge : IndexedMovementData
{

    public static readonly Dictionary<string, MovementDataFileVersion<MovementEdge>> versions;
    public static readonly string currentVersion;

    public MovementNode source;
    public MovementNode target;
    public bool sceneChange;
    public HashSet<MovementAction> actions;
    public HashSet<MovementState> states;

    static MovementEdge()
    {
        versions = new();

        string version = "1.0";
        List<string> fields = new() { nameof(id), nameof(source), nameof(target), nameof(sceneChange), nameof(actions), nameof(states) };
        Dictionary<string, string> colNameDict = new();
        foreach (string field in fields) colNameDict[field] = field;
        colNameDict[nameof(sceneChange)] = "scene change";
        versions[version] = new(version, fields, colNameDict);

        currentVersion = version;
    }

    public MovementEdge(int id, MovementNode source, MovementNode target, bool sceneChange) : base(id)
    {
        this.source = source;
        this.target = target;
        this.sceneChange = sceneChange;
        this.actions = new();
        this.states = new();
    }

    public MovementEdge(Dictionary<string, string> fields, Dictionary<int, MovementNode> nodes, Dictionary<int, MovementAction> actions, Dictionary<int, MovementState> states) : base(fields)
    {
        this.source = nodes[int.Parse(fields[GetColName(nameof(this.source))])];
        this.target = nodes[int.Parse(fields[GetColName(nameof(this.target))])];
        this.sceneChange = bool.Parse(fields[GetColName(nameof(this.sceneChange))]);

        //Parse the actions field as a comma separated list of IDs, then index into the actions dictionary
        this.actions = new();
        string actionsField = fields[GetColName(nameof(this.actions))];
        if (actionsField.Length > 0)
        {
            List<int> actionIDs = actionsField.Split(',').Select(id => int.Parse(id)).ToList();
            foreach (int actionID in actionIDs) this.actions.Add(actions[actionID]);
        }

        //Parse the states field as a comma separated list of IDs, then index into the states dictionary
        this.states = new();
        string statesField = fields[GetColName(nameof(this.states))];
        if (statesField.Length > 0)
        {
            List<int> stateIDs = statesField.Split(',').Select(id => int.Parse(id)).ToList();
            foreach (int stateID in stateIDs) this.states.Add(states[stateID]);
        }
    }

    public override Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { GetColName(nameof(this.id)), this.id.ToString() },
            { GetColName(nameof(this.source)), this.source.id.ToString() },
            { GetColName(nameof(this.target)), this.target.id.ToString() },
            { GetColName(nameof(this.sceneChange)), this.sceneChange.ToString() },
            { GetColName(nameof(this.actions)), string.Join(",", this.actions.Select(action => action.id)) },
            { GetColName(nameof(this.states)), string.Join(",", this.states.Select(state => state.id)) },
        };
    }

    public static string GetColName(string field) => versions[currentVersion].GetColName(field);

}
