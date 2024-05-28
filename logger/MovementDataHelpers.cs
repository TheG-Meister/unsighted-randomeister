using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementDataHelpers
{

    public static Dictionary<string, string> GetFieldToColNameDict(Type type)
    {
        Dictionary<string, string> result = new();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields) result.Add(field.Name, field.Name);
        return result;
    }

    public static List<string> GetColNamesFromDict(List<string> fields, Dictionary<string, string> colNamesDict)
    {
        List<string> result = new();
        foreach (string field in fields) result.Add(colNamesDict[field]);
        return result;
    }

    public static string GetColName(Dictionary<string, string> colNamesDict, string field)
    {
        if (!colNamesDict.ContainsKey(field)) throw new ArgumentException($"{field} is not a valid field");
        return colNamesDict[field];
    }

}
