using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public interface IMovementDataFileVersion<out T> where T : IMovementData
{

    string Version { get; }
    List<string> Fields { get; }
    List<string> ColNames { get; }

    public string GetColName(string field);

    public List<string> ToHeader();

    public Dictionary<string, string> ToDictionary();

    public void VerifyHeader(Dictionary<string, string> header);

    public void VerifyColNames(List<string> colNames);

    public static string GetTypeKey() => nameof(Type).ToLower();

    public string GetTypeValue();

    public static string GetVersionKey() => nameof(Version).ToLower();

    public string GetVersionValue();
}
