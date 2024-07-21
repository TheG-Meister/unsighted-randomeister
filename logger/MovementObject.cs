using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementObject : IMovementData
{

    public static readonly Dictionary<string, MovementDataFileVersion<MovementObject>> versions;
    public static readonly string currentVersion;

    public string type;
    public string scene;
    public string name;
    public float x;
    public float y;
    public float height;

    static MovementObject()
    {
        versions = new();

        string version = "1.0";
        List<string> fields = new() { nameof(type), nameof(scene), nameof(name), nameof(x), nameof(y), nameof(height) };
        Dictionary<string, string> colNameDict = new();
        foreach (string field in fields) colNameDict[field] = field;
        versions[version] = new(version, fields, colNameDict);

        currentVersion = version;
    }

    public MovementObject()
    {
    }

    public MovementObject(string type, string scene, string name, Vector3 position = new Vector3())
    {
        this.type = type;
        this.scene = scene;
        this.name = name;
        this.x = position.x;
        this.y = position.y;
        this.height = position.z;
    }

    public MovementObject(Dictionary<string, string> fields)
    {
        this.type = fields[GetColName(nameof(type))];
        this.scene = fields[GetColName(nameof(scene))];
        this.name = fields[GetColName(nameof(name))];
        this.x = float.Parse(fields[GetColName(nameof(x))]);
        this.y = float.Parse(fields[GetColName(nameof(y))]);
        this.height = float.Parse(fields[GetColName(nameof(height))]);
    }

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { GetColName(nameof(type)), type },
            { GetColName(nameof(scene)), scene },
            { GetColName(nameof(name)), name },
            { GetColName(nameof(x)), x.ToString() },
            { GetColName(nameof(y)) , y.ToString() },
            { GetColName(nameof(height)), height.ToString() },
        };
    }

    public static string GetColName(string field) => versions[currentVersion].GetColName(field);

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
