using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public abstract class MovementFile<T> where T : IMovementData
{

    public DelimitedFile file;
    public List<string> fieldNames;
    public Dictionary<int, T> parsedData;

    public MovementFile(string path, char delim, List<string> fieldNames)
    {
        this.fieldNames = fieldNames;
        this.file = new(path, delim);
    }

    public List<string> GetMissingFieldNames()
    {
        if (this.file.colNames == null || this.file.colNames.Count > 0) this.file.ReadColNames();

        List<string> missingFieldNames = new(this.fieldNames);
        foreach (string colName in this.file.colNames) if (missingFieldNames.Contains(colName)) missingFieldNames.Remove(colName);

        return missingFieldNames;
    }

    public virtual void Read()
    {
        this.file.Read();

        this.parsedData = new();

        foreach (int index in this.file.rows.Keys) this.parsedData[index] = this.ParseObject(this.file.GetEntry(index));
    }

    public virtual void Add(T obj)
    {
        int index = this.file.Add(obj.ToDictionary());
        this.parsedData[index] = obj;
    }

    public abstract T ParseObject(Dictionary<string, string> fields);

}
