using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public abstract class IndexedMovementFile<T> : MovementFile<T> where T : IndexedMovementData
{

    public int largestID;

    public IndexedMovementFile(string path, char delim, List<string> fieldNames) : base(path, delim, fieldNames)
    {
        this.largestID = -1;
    }

    public override void Read()
    {
        base.Read();
        foreach (T obj in this.parsedData.Values) if (obj.id > this.largestID) this.largestID = obj.id;
    }

    public override void Add(T obj)
    {
        base.Add(obj);
        if (obj.id > this.largestID) this.largestID = obj.id;
    }

}
