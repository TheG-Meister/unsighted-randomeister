using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementStatesFile : IndexedMovementDataFile<MovementState>
{
    public MovementStatesFile(string path) : base(path, MovementState.FIELDS)
    {

    }
}
