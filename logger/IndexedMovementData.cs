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

    public IndexedMovementData(string id)
    {
        if (!int.TryParse(id, out this.id)) this.id = -1;
    }

    public abstract Dictionary<string, string> ToDictionary();

}
