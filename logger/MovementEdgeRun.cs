using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementEdgeRun : IMovementData
{

    public static readonly List<string> FIELDS = new() { "edge id", "real time", "game time", "timestamp", "version" };

    public int edgeID;
    public float realTime;
    public float gameTime;
    public long timestamp;
    public string version;

    public MovementEdgeRun(int edgeID, float realTime, float gameTime, long timestamp, string version)
    {
        this.edgeID = edgeID;
        this.realTime = realTime;
        this.gameTime = gameTime;
        this.timestamp = timestamp;
        this.version = version;
    }

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { nameof(this.edgeID), this.edgeID.ToString() },
            { nameof(this.realTime), this.realTime.ToString() },
            { nameof(this.gameTime), this.gameTime.ToString() },
            { nameof(this.timestamp), this.timestamp.ToString() },
            { nameof(this.version), this.version.ToString() },
        };
    }
}
