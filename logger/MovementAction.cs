using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementAction : IndexedMovementData
{

    public static readonly List<string> FIELDS = new() { nameof(id), nameof(action) };

    public PlayerAction action;

    public MovementAction(int id, PlayerAction action) : base(id)
    {
        this.action = action;
    }

    public MovementAction(string id, string action) : base(id)
    {
        this.action = (PlayerAction) Enum.Parse(typeof(PlayerAction), action, false);
    }

    public MovementAction(Dictionary<string, string> fields) : this(fields[nameof(id)], fields[nameof(action)])
    {
    }

    public override bool SetField(string field, string value)
    {
        if (field == nameof(action)) return Enum.TryParse(value, out PlayerAction _);
        else return base.SetField(field, value);
    }

    public override Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { nameof(this.id), this.id.ToString() },
            { nameof(this.action), this.action.ToString() },
        };
    }

    public override bool Equals(object obj)
    {
        return obj is MovementAction action &&
               this.action == action.action;
    }

    public override int GetHashCode()
    {
        return -1387187753 + action.GetHashCode();
    }

}
