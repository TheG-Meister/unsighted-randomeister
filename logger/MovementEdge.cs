using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementEdge
{

    public int source;
    public int target;
    public HashSet<PlayerAction> actions;
    public HashSet<MovementState> states;
    public bool sceneChange;
    public float realTime;
    public float gameTime;
    public long timestamp;

    public MovementEdge(int source, int target, bool sceneChange, float realTime, float gameTime, long timestamp)
    {
        this.source = source;
        this.target = target;
        this.sceneChange = sceneChange;
        this.realTime = realTime;
        this.gameTime = gameTime;
        this.timestamp = timestamp;
        this.actions = new();
        this.states = new();
    }

}
