using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementDataFile<T> : DelimitedFile where T : IMovementData
{

    public List<string> fieldNames;
    public Dictionary<int, T> parsedData;

    public MovementDataFile(string path, List<string> fieldNames) : base(path, '\t')
    {
        this.fieldNames = fieldNames;
        this.parsedData = new();
    }

    public List<string> GetMissingFieldNames()
    {
        List<string> missingFieldNames = new(this.fieldNames);
        foreach (string colName in this.colNames) if (missingFieldNames.Contains(colName)) missingFieldNames.Remove(colName);

        return missingFieldNames;
    }

    public virtual void Add(T obj)
    {
        int index = this.Add(obj.ToDictionary());
        this.parsedData[index] = obj;
    }

}
