using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementObject : IMovementData
{

    public static readonly List<string> FIELDS = new() { "type", "scene", "name", "x", "y", "height" };

    public string type;
    public string scene;
    public string name;
    public Vector3 position;

    public MovementObject()
    {
    }

    public MovementObject(string type, string scene, string name, Vector3 position = new Vector3())
    {
        this.type = type;
        this.scene = scene;
        this.name = name;
        this.position = position;
    }

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { nameof(type), type },
            { nameof(scene), scene },
            { nameof(name), name },
            { "x", position.x.ToString() },
            { "y", position.y.ToString() },
            { "height", position.z.ToString() },
        };
    }

    public override bool Equals(object obj)
    {
        return obj is MovementObject @object &&
               type == @object.type &&
               scene == @object.scene &&
               name == @object.name;
    }

    public override int GetHashCode()
    {
        int hashCode = -1080393842;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(type);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(scene);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
        return hashCode;
    }
}
