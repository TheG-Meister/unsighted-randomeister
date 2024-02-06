using dev.gmeister.unsighted.randomeister.core;
using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementNode
{

    public int id;
    public string scene;
    public string location;
    public HashSet<PlayerAction> actions;
    public HashSet<int> states;

    public MovementNode(int id, string scene, string location)
    {
        this.id = id;
        this.scene = scene;
        this.location = location;
        this.actions = new();
        this.states = new();
    }

    public string GetStringID()
    {
        return string.Join(Constants.MOVEMENT_LOGGER_ID_SEPARATOR.ToString(), this.scene, this.location);
    }

}
