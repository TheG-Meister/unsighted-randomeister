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

    public bool VerifyHeader(Dictionary<string, string> header);

    public bool VerifyColNames(List<string> colNames);

    public string GetTypeKey();

    public string GetTypeValue();

    public string GetVersionKey();

    public string GetVersionValue();
}
