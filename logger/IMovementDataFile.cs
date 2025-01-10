using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public interface IMovementDataFile : IDelimitedFile
{

    public Dictionary<int, Exception> Parse();

    public void FindVersion();

    //public IReadOnlyDictionary<int, T> ParsedData { get; }

    public void Create();

}
