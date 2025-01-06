using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class UniqueMovementDataFile<T> : MovementDataFile<T> where T : class, IMovementData
{
    public UniqueMovementDataFile(string path, Func<Dictionary<string, string>, T> factory, List<MovementDataFileVersion<T>> versions) : base(path, factory, versions)
    {
    }

    public UniqueMovementDataFile(Stream stream, Func<Dictionary<string, string>, T> factory, List<MovementDataFileVersion<T>> versions) : base(stream, factory, versions)
    {
    }

    public override int Add(T obj)
    {
        if (this.Contains(obj)) return -1;

        return base.Add(obj);
    }

}
