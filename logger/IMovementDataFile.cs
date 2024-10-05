using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public interface IMovementDataFile : IDelimitedFile
{

    public void CreateAndWriteHeader();

    public Dictionary<int, bool> Parse();

    public bool FindVersion();

    //public IReadOnlyDictionary<int, T> ParsedData { get; }

}
