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

    public IndexedMovementData(Dictionary<string, string> fields)
    {
        foreach (string field in fields.Keys)
        {
            if (!this.SetField(field, fields[field])) throw new ArgumentException($"{fields[field]} could not be parsed into the {field} field");
        }
    }

    public abstract Dictionary<string, string> ToDictionary();
    public virtual bool SetField(string field, string value)
    {
        if (field == nameof(id)) return int.TryParse(value, out this.id);
        else throw new ArgumentException($"{field} is not a parseable field");
    }

}
