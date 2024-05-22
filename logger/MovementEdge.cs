using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementEdge : IndexedMovementData
{

    public static readonly List<string> FIELDS = new() { "id", "source", "target", "scene change", "actions", "states" };

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
