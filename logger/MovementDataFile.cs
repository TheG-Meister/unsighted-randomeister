using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementDataFile<T> : DelimitedFile, IMovementDataFile where T : IMovementData
{

    public Dictionary<string, string> header;
    public Dictionary<int, T> parsedData { get; set; }
    public List<MovementDataFileVersion<T>> versions;
    public MovementDataFileVersion<T> version;
    public Func<Dictionary<string, string>, T> factory;

    public MovementDataFile(string path) : base(path, '\t')
    {
        
    }

    public override void ReadAll()
    {
        base.ReadAll();

        List<string> headerLines = new();
        foreach (KeyValuePair<int, string> kvp in this.unusedLines)
        {
            string line = kvp.Value;
            if (line.StartsWith(COMMENT_CHAR.ToString()))
            {
                headerLines.Add(line.Substring(line.IndexOf(COMMENT_CHAR.ToString()) + 1));
            }
        }

        this.header = MovementDataFileVersion<T>.ParseHeader(headerLines);
    }

    public virtual void Add(T obj)
    {
        int index = this.Add(obj.ToDictionary());
        this.parsedData[index] = obj;
    }

    public bool FindVersion()
    {
        if (!this.Exists()) return false;
        if (!this.header.TryGetValue("version", out string versionString)) return false;
        MovementDataFileVersion<T> version = this.versions.Find(v => v.Version == versionString);
        if (version == null) return false;
        if (!version.VerifyHeader(this.header)) return false;
        if (!version.VerifyColNames(this.colNames)) return false;

        this.version = version;

        return true;
    }

    public void CreateAndWriteHeader()
    {
        this.Create();
        this.version = this.versions[this.versions.Count];
        this.header = version.ToDictionary();
        List<string> headerLines = version.ToHeader();
        foreach (string line in headerLines) this.AddComment(line);
        this.AddColNamesLine(version.ColNames.ToArray());
    }

    public virtual Dictionary<int, bool> Parse()
    {
        this.parsedData = new();
        Dictionary<int, bool> result = new();
        foreach (int key in this.rows.Keys)
        {
            Dictionary<string, string> entry = this.GetEntry(key);
            try
            {
                this.parsedData[key] = this.factory.Invoke(entry);
                result[key] = true;
            }
            catch (Exception)
            {
                result[key] = false;
            }
        }

        return result;
    }



}
