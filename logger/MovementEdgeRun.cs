using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementEdgeRun : IMovementData
{

    public static readonly List<string> FIELDS = new() { nameof(edge), nameof(version), nameof(timestamp), nameof(realTime), nameof(gameTime) };

    public MovementEdge edge;
    public string version;
    public long timestamp;
    public float realTime;
    public float gameTime;

    public MovementEdgeRun(MovementEdge edge, float realTime, float gameTime, long timestamp, string version)
    {
        this.edge = edge;
        this.version = version;
        this.timestamp = timestamp;
        this.realTime = realTime;
        this.gameTime = gameTime;
    }

    public MovementEdgeRun(Dictionary<string, string> fields, Dictionary<int, MovementEdge> edges)
    {
        this.edge = edges[int.Parse(fields[FieldToColName(nameof(edge))])];
        this.version = fields[FieldToColName(nameof(version))];
        this.timestamp = long.Parse(fields[FieldToColName(nameof(timestamp))]);
        this.realTime = float.Parse(fields[FieldToColName(nameof(realTime))]);
        this.gameTime = float.Parse(fields[FieldToColName(nameof(gameTime))]);
    }

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { FieldToColName(nameof(edge)), this.edge.id.ToString() },
            { FieldToColName(nameof(version)), this.version.ToString() },
            { FieldToColName(nameof(timestamp)), this.timestamp.ToString() },
            { FieldToColName(nameof(realTime)), this.realTime.ToString() },
            { FieldToColName(nameof(gameTime)), this.gameTime.ToString() },
        };
    }

    public static string FieldToColName(string field)
    {
        if (fieldToColName == null)
        {
            fieldToColName = MovementDataHelpers.GetFieldToColNameDict(typeof(MovementEdgeRun));
            fieldToColName[nameof(edge)] = "edge id";
            fieldToColName[nameof(realTime)] = "real time";
            fieldToColName[nameof(gameTime)] = "game time";
        }

        if (!fieldToColName.ContainsKey(field)) throw new ArgumentException($"{field} is not a valid field.");
        return fieldToColName[field];
    }

    public static Dictionary<string, string> fieldToColName = null;

}
