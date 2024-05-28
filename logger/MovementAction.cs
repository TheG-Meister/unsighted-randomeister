using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementAction : IndexedMovementData
{

    public static readonly List<string> FIELDS = new() { FieldToColName(nameof(id)), FieldToColName(nameof(action)) };

    public PlayerAction action;

    public MovementAction(int id, PlayerAction action) : base(id)
    {
        this.action = action;
    }

    public MovementAction(string id, string action) : base(id)
    {
        this.action = (PlayerAction) Enum.Parse(typeof(PlayerAction), action, false);
    }

    public MovementAction(Dictionary<string, string> fields) : this(fields[FieldToColName(nameof(id))], fields[FieldToColName(nameof(action))])
    {
    }

    public override Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { FieldToColName(nameof(this.id)), this.id.ToString() },
            { FieldToColName(nameof(this.action)), this.action.ToString() },
        };
    }

    public static string FieldToColName(string field)
    {
        fieldToColName ??= MovementDataHelpers.GetFieldToColNameDict(typeof(MovementAction));

        if (!fieldToColName.ContainsKey(field)) throw new ArgumentException($"{field} is not a valid field");
        return fieldToColName[field];
    }

    public static Dictionary<string, string> fieldToColName = null;

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
