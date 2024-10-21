using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class IndexedMovementDataFile<T> : MovementDataFile<T> where T : IndexedMovementData
{

    public int nextID;

    public IndexedMovementDataFile(string path, Func<Dictionary<string, string>, T> factory, List<MovementDataFileVersion<T>> versions) : base(path, factory, versions)
    {
        this.nextID = -1;
    }

    public IndexedMovementDataFile(Stream stream, Func<Dictionary<string, string>, T> factory, List<MovementDataFileVersion<T>> versions) : base(stream, factory, versions)
    {
        this.nextID = -1;
    }

    public void ResetNextID()
    {
        foreach (T obj in this.parsedData.Values) if (obj != null && obj.id >= this.nextID) this.nextID = obj.id + 1; 
    }

    public override void Add(T obj)
    {
        base.Add(obj);
        if (obj.id >= this.nextID) this.nextID = obj.id + 1;
    }

    public override Dictionary<int, bool> Parse()
    {
        Dictionary<int, bool> parses = base.Parse();
        this.nextID = -1;
        foreach (int key in this.parsedData.Keys)
        {
            if (this.parsedData[key].id >= this.nextID) this.nextID = this.parsedData[key].id + 1;
        }

        return parses;
    }

    public int GetNextID()
    {
        return this.nextID++;
    }

}
