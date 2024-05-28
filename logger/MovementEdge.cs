using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementEdge : IndexedMovementData
{

    public static readonly List<string> FIELDS = new() { nameof(id), nameof(source), nameof(target), nameof(sceneChange), nameof(actions), nameof(states) };

    public MovementNode source;
    public MovementNode target;
    public bool sceneChange;
    public HashSet<MovementAction> actions;
    public HashSet<MovementState> states;

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
        this.source = nodes[int.Parse(fields[FieldToColName(nameof(this.source))])];
        this.target = nodes[int.Parse(fields[FieldToColName(nameof(this.target))])];
        this.sceneChange = bool.Parse(fields[FieldToColName(nameof(this.sceneChange))]);

        //Parse the actions field as a comma separated list of IDs, then index into the actions dictionary
        this.actions = new();
        string actionsField = fields[FieldToColName(nameof(this.actions))];
        if (actionsField.Length > 0)
        {
            List<int> actionIDs = actionsField.Split(',').Select(id => int.Parse(id)).ToList();
            foreach (int actionID in actionIDs) this.actions.Add(actions[actionID]);
        }

        //Parse the states field as a comma separated list of IDs, then index into the states dictionary
        this.states = new();
        string statesField = fields[FieldToColName(nameof(this.states))];
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
            { FieldToColName(nameof(this.id)), this.id.ToString() },
            { FieldToColName(nameof(this.source)), this.source.id.ToString() },
            { FieldToColName(nameof(this.target)), this.target.id.ToString() },
            { FieldToColName(nameof(this.sceneChange)), this.sceneChange.ToString() },
            { FieldToColName(nameof(this.actions)), string.Join(",", this.actions.Select(action => action.id)) },
            { FieldToColName(nameof(this.states)), string.Join(",", this.states.Select(state => state.id)) },
        };
    }

    public static string FieldToColName(string field)
    {
        if (ColNameDict == null)
        {
            ColNameDict = MovementDataHelpers.GetFieldToColNameDict(typeof(MovementEdge));
            ColNameDict[nameof(sceneChange)] = "scene change";
        }

        if (!ColNameDict.ContainsKey(field)) throw new ArgumentException($"{field} is not a valid field");

        return ColNameDict[field];
    }
    public static Dictionary<string, string> ColNameDict = null;

}
