using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementEdgeRun : IMovementData
{

    public static readonly List<string> FIELDS = new() { "edge id", "real time", "game time", nameof(timestamp), nameof(version) };

    public MovementEdge edge;
    public float realTime;
    public float gameTime;
    public long timestamp;
    public string version;

    public MovementEdgeRun(MovementEdge edge, float realTime, float gameTime, long timestamp, string version)
    {
        this.edge = edge;
        this.realTime = realTime;
        this.gameTime = gameTime;
        this.timestamp = timestamp;
        this.version = version;
    }

    public bool SetField(string field, string value)
    {
        switch (field)
        {
            case "real time":
                return float.TryParse(value, out realTime);
            case "game time":
                return float.TryParse(value, out gameTime);
            case nameof(timestamp):
                return long.TryParse(value, out timestamp);
            case nameof(version):
                this.version = value;
                return true;
            default:
                throw new ArgumentException($"{field} is not a parseable field");
        }
    }

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { "edge id", this.edge.id.ToString() },
            { "real time", this.realTime.ToString() },
            { "game time", this.gameTime.ToString() },
            { nameof(this.timestamp), this.timestamp.ToString() },
            { nameof(this.version), this.version.ToString() },
        };
    }
}
