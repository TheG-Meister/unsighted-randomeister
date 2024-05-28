using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public abstract class IndexedMovementData : IMovementData
{

    public int id;

    public IndexedMovementData(int id)
    {
        this.id = id;
    }

    public IndexedMovementData(string id) : this(int.Parse(id))
    {
    }

    public IndexedMovementData(Dictionary<string, string> fields) : this(fields[nameof(id)])
    {
    }

    public abstract Dictionary<string, string> ToDictionary();

}
