using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class IndexedMovementDataFile<T> : MovementDataFile<T> where T : IndexedMovementData
{

    public int largestID;

    public IndexedMovementDataFile(string path, List<string> fieldNames) : base(path, fieldNames)
    {
        this.largestID = -1;
    }

    public void UpdateLargestID()
    {
        foreach (T obj in this.parsedData.Values) if (obj != null && obj.id > this.largestID) this.largestID = obj.id; 
    }

    public override void Add(T obj)
    {
        base.Add(obj);
        if (obj.id > this.largestID) this.largestID = obj.id;
    }

}
