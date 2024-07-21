using FMOD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementDataFileVersion<T> where T : IMovementData
{

    public const char DELIM = ':';
    public const string TYPE_KEY = "type";
    public const string VERSION_KEY = "version";

    public readonly string version;
    public readonly List<string> fields;
    public readonly List<string> colNames;
    private readonly Dictionary<string, string> colNameDict;

    public MovementDataFileVersion(string version, List<string> fields, Dictionary<string, string> colNameDict)
    {
        this.version = version;
        this.fields = new(fields);
        this.colNameDict = new(colNameDict);

        this.colNames = new();
        foreach (string field in this.fields) colNames.Add(this.colNameDict[field]);
    }

    public string GetColName(string field)
    {
        return colNameDict[field];
    }

    public List<string> GetHeader()
    {
        return new()
        {
            string.Join(DELIM.ToString(), "type", typeof(T).FullName),
            string.Join(DELIM.ToString(), nameof(version), version),
        };
    }

    public bool VerifyHeader(Dictionary<string, string> header)
    {
        if (!header.ContainsKey("type") || header["type"] != typeof(T).FullName) return false;
        if (!header.ContainsKey(nameof(version)) || header[nameof(version)] != this.version) return false;
        return true;
    }

    public bool VerifyColNames(List<string> colNames)
    {
        if (this.colNames.Except(colNames).Any()) return false;
        if (colNames.Except(this.colNames).Any()) return false;
        return true;
    }

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
