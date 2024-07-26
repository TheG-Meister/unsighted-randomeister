using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementEdgeRun : IMovementData
{

    public static readonly Dictionary<string, MovementDataFileVersion<MovementEdgeRun>> versions;
    public static readonly string currentVersion;

    public MovementEdge edge;
    public string version;
    public long timestamp;
    public float realTime;
    public float gameTime;

    static MovementEdgeRun()
    {
        versions = new();

        string version = "1.0";
        List<string> fields = new() { nameof(edge), nameof(version), nameof(timestamp), nameof(realTime), nameof(gameTime) };
        Dictionary<string, string> colNameDict = new();
        foreach (string field in fields) colNameDict[field] = field;
        versions[version] = new(version, fields, colNameDict);

        currentVersion = version;
    }

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
        this.edge = edges[int.Parse(fields[GetColName(nameof(edge))])];
        this.version = fields[GetColName(nameof(version))];
        this.timestamp = long.Parse(fields[GetColName(nameof(timestamp))]);
        this.realTime = float.Parse(fields[GetColName(nameof(realTime))]);
        this.gameTime = float.Parse(fields[GetColName(nameof(gameTime))]);
    }

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { GetColName(nameof(edge)), this.edge.id.ToString() },
            { GetColName(nameof(version)), this.version.ToString() },
            { GetColName(nameof(timestamp)), this.timestamp.ToString() },
            { GetColName(nameof(realTime)), this.realTime.ToString() },
            { GetColName(nameof(gameTime)), this.gameTime.ToString() },
        };
    }

    public static string GetColName(string field) => versions[currentVersion].GetColName(field);

    public static MovementDataFileVersion<MovementEdgeRun> GetCurrentVersion() => versions[currentVersion];

}
