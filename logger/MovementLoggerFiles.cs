using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementLoggerFiles
{

    public string path;
    public string backupsPath;
    public IndexedMovementDataFile<MovementAction> actionsFile;
    public IndexedMovementDataFile<MovementState> statesFile;
    public IndexedMovementDataFile<MovementNode> nodesFile;
    public MovementDataFile<MovementObject> objectsFile;
    public IndexedMovementDataFile<MovementEdge> edgesFile;
    public MovementDataFile<MovementEdgeRun> edgeRunsFile;
    public MovementDataFile<MovementEdgeRun> haileeEdgeRunsFile;

    public MovementLoggerFiles(string path)
    {
        this.path = path;

        this.actionsFile = new(Path.Combine(path, "actions.tsv"), );

    }



}
