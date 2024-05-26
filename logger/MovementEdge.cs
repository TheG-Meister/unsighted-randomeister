using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementEdge : IndexedMovementData
{

    public static readonly List<string> FIELDS = new() { nameof(id), nameof(source), nameof(target), "scene change", nameof(actions), nameof(states) };

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

    public MovementEdge(string id, MovementNode source, MovementNode target, string sceneChange, HashSet<MovementAction> actions, HashSet<MovementState> states) : base(id)
    {
        this.source = source;
        this.target = target;
        this.sceneChange = bool.Parse(sceneChange);
        this.actions = actions;
        this.states = states;
    }

    public override bool SetField(string field, string value)
    {
        if (field == "scene change") return bool.TryParse(value, out sceneChange);
        return base.SetField(field, value);
    }

    public override Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { nameof(this.id), this.id.ToString() },
            { nameof(this.source), this.source.id.ToString() },
            { nameof(this.target), this.target.id.ToString() },
            { nameof(this.sceneChange), this.sceneChange.ToString() },
            { nameof(this.actions), string.Join(",", this.actions.Select(action => action.id)) },
            { nameof(this.states), string.Join(",", this.states.Select(state => state.id)) },
        };
    }
}
