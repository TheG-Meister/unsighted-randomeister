using dev.gmeister.unsighted.randomeister.core;
using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementNode
{

    public int id;
    public string scene;
    public string location;
    public Vector3 position;
    public HashSet<PlayerAction> actions;
    public HashSet<MovementState> states;

    public MovementNode(int id, string scene, string location, Vector3 postition)
    {
        this.id = id;
        this.scene = scene;
        this.location = location;
        this.position = postition;
        this.actions = new();
        this.states = new();
    }

    public string GetStringID()
    {
        return string.Join(Constants.ID_SEPARATOR.ToString(), this.scene, this.location);
    }

}
