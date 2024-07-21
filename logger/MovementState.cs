using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementState : IndexedMovementData
{

    public static readonly Dictionary<string, MovementDataFileVersion<MovementState>> versions;
    public static readonly string currentVersion;

    public string scene;
    public string name;

    static MovementState()
    {
        versions = new();

        string version = "1.0";
        List<string> fields = new() { nameof(id), nameof(scene), nameof(name) };
        Dictionary<string, string> colNameDict = new();
        foreach (string field in fields) colNameDict[field] = field;
        versions[version] = new(version, fields, colNameDict);

        currentVersion = version;
    }

    public MovementState(int id, string name, string scene) : base(id)
    {
        this.scene = scene;
        this.name = name;
    }

    public MovementState(Dictionary<string, string> fields) : base(fields)
    {
        this.scene = fields[GetColName(nameof(scene))];
        this.name = fields[GetColName(nameof(name))];
    }

    public string GetStringID()
    {
        return string.IsNullOrEmpty(scene) ? this.name : string.Join(Constants.ID_SEPARATOR.ToString(), this.scene, this.name);
    }

    public override Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            { GetColName(nameof(id)), this.id.ToString() },
            { GetColName(nameof(scene)), this.scene },
            { GetColName(nameof(name)), this.name },
        };
    }

    public static string GetColName(string field) => versions[currentVersion].GetColName(field);

}
