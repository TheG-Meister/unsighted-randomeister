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
        this.scene = fields[GetColName(nameof(scene))];
        this.location = fields[GetColName(nameof(location))];
        this.x = int.Parse(fields[GetColName(nameof(x))]);
        this.y = int.Parse(fields[GetColName(nameof(y))]);
        this.height = int.Parse(fields[GetColName(nameof(height))]);
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
            { GetColName(nameof(id)), id.ToString() },
            { GetColName(nameof(scene)), scene.ToString() },
            { GetColName(nameof(location)), location.ToString() },
            { GetColName(nameof(x)), x.ToString() },
            { GetColName(nameof(y)), y.ToString() },
            { GetColName(nameof(height)), height.ToString() },
        };
    }

    public static string GetColName(string field) => MovementDataHelpers.GetColName(GetColNameDict(), field);

    public static List<string> GetColNames() => MovementDataHelpers.GetColNamesFromDict(FIELDS, GetColNameDict());

    public static Dictionary<string, string> GetColNameDict()
    {
        ColNameDict ??= MovementDataHelpers.GetFieldToColNameDict(typeof(MovementNode));
        return ColNameDict;
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
