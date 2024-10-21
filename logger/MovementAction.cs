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

    public static readonly List<MovementDataFileVersion<MovementAction>> versions;

    public PlayerAction action;

    static MovementAction()
    {
        versions = new();

        string version = "1.0";
        List<string> fields = new() { nameof(id), nameof(action) };
        Dictionary<string, string> colNameDict = new();
        foreach (string field in fields) colNameDict[field] = field;
        versions.Add(new(version, fields, colNameDict));
    }

    public MovementAction(int id, PlayerAction action) : base(id)
    {
        this.action = action;
    }

    public MovementAction(string id, string action) : base(id)
    {
        this.action = (PlayerAction) Enum.Parse(typeof(PlayerAction), action, false);
    }

    public MovementAction(Dictionary<string, string> fields) : this(fields[GetColName(nameof(id))], fields[GetColName(nameof(action))])
    {
    }

    public override Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { GetColName(nameof(this.id)), this.id.ToString() },
            { GetColName(nameof(this.action)), this.action.ToString() },
        };
    }

    public static string GetColName(string field) => GetCurrentVersion().GetColName(field);

    public static MovementDataFileVersion<MovementAction> GetCurrentVersion() => versions[versions.Count - 1];

    public static MovementDataFileVersion<MovementAction> GetVersion(string version) => versions.Find(v => v.Version == version);

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
