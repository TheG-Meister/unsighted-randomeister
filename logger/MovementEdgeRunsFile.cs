using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementEdgeRunsFile : IndexedMovementDataFile<MovementEdge>
{
    public MovementEdgeRunsFile(string path) : base(path, MovementEdge.FIELDS)
    {

    }

}
