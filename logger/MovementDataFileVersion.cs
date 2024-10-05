using FMOD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementDataFileVersion<T> : IMovementDataFileVersion<T> where T : IMovementData
{

    public const char DELIM = ':';
    public const string TYPE_KEY = "type";

    public string Version { get; }
    public List<string> Fields { get; }
    public List<string> ColNames { get; }
    private readonly Dictionary<string, string> colNameDict;

    public MovementDataFileVersion(string version, List<string> fields, Dictionary<string, string> colNameDict)
    {
        this.Version = version;
        this.Fields = new(fields);
        this.colNameDict = new(colNameDict);

        this.ColNames = new();
        foreach (string field in this.Fields) ColNames.Add(this.colNameDict[field]);
    }

    public string GetColName(string field)
    {
        return colNameDict[field];
    }

    public List<string> ToHeader()
    {
        return new()
        {
            string.Join(DELIM.ToString(), GetTypeKey(), GetTypeValue()),
            string.Join(DELIM.ToString(), GetVersionKey(), GetVersionValue()),
        };
    }

    public Dictionary<string, string> ToDictionary()
    {
        return new()
        {
            { GetTypeKey(), GetTypeValue() },
            { GetVersionKey(), GetVersionValue() },
        };
    }

    public bool VerifyHeader(Dictionary<string, string> header)
    {
        if (!header.ContainsKey(GetTypeKey()) || header[GetTypeKey()] != GetTypeValue()) return false;
        if (!header.ContainsKey(GetVersionKey()) || header[GetVersionKey()] != GetVersionValue()) return false;
        return true;
    }

    public bool VerifyColNames(List<string> colNames)
    {
        if (this.ColNames.Except(colNames).Any()) return false;
        if (colNames.Except(this.ColNames).Any()) return false;
        return true;
    }

    public string GetTypeKey() => nameof(Type).ToLower();
    public string GetTypeValue() => typeof(T).FullName;
    public string GetVersionKey() => nameof(Version).ToLower();
    public string GetVersionValue() => this.Version;

    public static Dictionary<string, string> ParseHeader(List<string> lines)
    {
        Dictionary<string, string> result = new();

        foreach (string line in lines)
        {
            int firstDelim = line.IndexOf(DELIM);
            if (firstDelim != -1) result[line.Substring(0, firstDelim)] = line.Substring(firstDelim + 1);
        }

        return result;
    }

    public static Dictionary<string, string> GetFieldToColNameDict(Type type)
    {
        Dictionary<string, string> result = new();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields) result[field.Name] = field.Name;
        return result;
    }

    public static List<string> GetColNamesFromDict(List<string> fields, Dictionary<string, string> colNamesDict)
    {
        List<string> result = new();
        foreach (string field in fields) result.Add(colNamesDict[field]);
        return result;
    }

}
