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

    public static readonly List<string> FIELDS = new() { nameof(id), nameof(scene), nameof(location), nameof(x), nameof(y), nameof(height) };

    public string scene;
    public string location;
    public float x;
    public float y;
    public float height;
    
    public MovementNode(int id, string scene, string location, Vector3 position) : base(id)
    {
        this.scene = scene;
        this.location = location;
        this.x = position.x;
        this.y = position.y;
        this.height = position.z;
    }

    public MovementNode(Dictionary<string, string> fields) : base(fields)
    {
        this.scene = fields[FieldToColName(nameof(scene))];
        this.location = fields[FieldToColName(nameof(location))];
        this.x = int.Parse(fields[FieldToColName(nameof(x))]);
        this.y = int.Parse(fields[FieldToColName(nameof(y))]);
        this.height = int.Parse(fields[FieldToColName(nameof(height))]);
    }

    public Vector3 GetPositionVector()
    {
        return new Vector3(this.x, this.y, this.height);
    }

    public string GetStringID()
    {
        return string.Join(Constants.ID_SEPARATOR.ToString(), this.scene, this.location);
    }

    public override Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { FieldToColName(nameof(id)), id.ToString() },
            { FieldToColName(nameof(scene)), scene.ToString() },
            { FieldToColName(nameof(location)), location.ToString() },
            { FieldToColName(nameof(x)), x.ToString() },
            { FieldToColName(nameof(y)), y.ToString() },
            { FieldToColName(nameof(height)), height.ToString() },
        };
    }

    public static string FieldToColName(string field)
    {
        ColNameDict ??= MovementDataHelpers.GetFieldToColNameDict(typeof(MovementNode));

        if (!ColNameDict.ContainsKey(field)) throw new ArgumentException($"{field} is not a valid field");
        return ColNameDict[field];
    }

    private static Dictionary<string, string> ColNameDict = null;

    public override bool Equals(object obj)
    {
        return obj is MovementNode node &&
               scene == node.scene &&
               location == node.location;
    }

    public override int GetHashCode()
    {
        int hashCode = 179142321;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(scene);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(location);
        return hashCode;
    }

}
