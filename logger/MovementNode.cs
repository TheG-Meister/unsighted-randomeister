using dev.gmeister.unsighted.randomeister.core;
using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementNode : IndexedMovementData
{

    public static readonly List<string> FIELDS = new() { "id", "scene", "location", "x", "y", "height" };

    public string scene;
    public string location;
    public Vector3 position;
    
    public MovementNode(int id, string scene, string location, Vector3 position) : base(id)
    {
        this.scene = scene;
        this.location = location;
        this.position = position;
    }

    public string GetStringID()
    {
        return string.Join(Constants.ID_SEPARATOR.ToString(), this.scene, this.location);
    }

    public override Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { nameof(id), id.ToString() },
            { nameof(scene), scene.ToString() },
            { nameof(location), location.ToString() },
            { "x", position.x.ToString() },
            { "y", position.y.ToString() },
            { "height", position.z.ToString() },
        };
    }

}
