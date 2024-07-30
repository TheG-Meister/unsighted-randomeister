using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementDataFile<T> : DelimitedFile, IMovementDataFile<T> where T : IMovementData
{

    public Dictionary<string, string> header;
    public Dictionary<int, T> parsedData;
    public IMovementDataFileVersion<T> version;

    public MovementDataFile(string path) : base(path, '\t')
    {}

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

    public string GetVersionString()
    {
        return this.header[nameof(IMovementDataFileVersion<T>.Version)];
    }

    public void CreateAndWriteHeader(IMovementDataFileVersion<T> version)
    {
        this.Create();
        this.version = version;
        this.header = version.ToDictionary();
        List<string> headerLines = version.ToHeader();
        foreach (string line in headerLines) this.AddComment(line);
        this.AddColNamesLine(version.ColNames.ToArray());
    }
}
