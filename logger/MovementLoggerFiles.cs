using dev.gmeister.unsighted.randomeister.core;
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
    public string completePath;
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
        this.backupsPath = Path.Combine(this.path, "backups");
        this.completePath = Path.Combine(this.path, "complete");

        Directory.CreateDirectory(this.path);
        Directory.CreateDirectory(this.backupsPath);
        Directory.CreateDirectory(this.completePath);

        this.actionsFile = new(Path.Combine(this.path, "actions.tsv"), MovementAction.GetColNames());
        this.statesFile = new(Path.Combine(this.path, "states.tsv"), MovementState.GetColNames());
        this.nodesFile = new(Path.Combine(this.path, "nodes.tsv"), MovementNode.GetColNames());
        this.objectsFile = new(Path.Combine(this.path, "objects.tsv"), MovementObject.GetColNames());
        this.edgesFile = new(Path.Combine(this.path, "edges.tsv"), MovementEdge.GetColNames());
        this.edgeRunsFile = new(Path.Combine(this.path, "edge-runs.tsv"), MovementEdgeRun.GetColNames());
        this.haileeEdgeRunsFile = new(Path.Combine(this.path, "hailee-edge-runs.tsv"), MovementEdgeRun.GetColNames());

        Dictionary<string, string> actionsFileHeader = new()
        {
            { "module", typeof(MovementLogger).Name },
            { "data-type", typeof(MovementAction).Name },
        };

        List<string> actionColNames = (List<string>) typeof(MovementAction).GetMethod(nameof(MovementAction.GetColNames)).Invoke(null, null);

        if (!this.actionsFile.Exists())
        {
            this.actionsFile.Create();
            this.actionsFile.AddComment($"package:{Constants.GUID}");
            this.actionsFile.AddComment($"module:{typeof(MovementLogger)}");
            this.actionsFile.AddComment($"data-type:{typeof(MovementAction)}");
            this.actionsFile.AddComment($"version:{Constants.VERSION}");
            this.actionsFile.AddColNamesLine(this.actionsFile.fieldNames.ToArray());
        }
        else
        {
            this.actionsFile.ReadAll();

            //header
            if (this.actionsFile.unusedLines.Count < 4) 

            //col names
            //line lengths
            //read errors

        }
    }

    public bool ReadActionsFile()
    {
        IndexedMovementDataFile<MovementAction> file = this.actionsFile;
        if (!file.Exists())
        {
            file.Create();
            file.AddComment($"package:{Constants.GUID}");
            file.AddComment($"module:{typeof(MovementLogger)}");
            file.AddComment($"data-type:{typeof(MovementAction)}");
            file.AddComment($"version:{Constants.VERSION}");
            file.AddColNamesLine(file.fieldNames.ToArray());
        }
        else
        {
            file.ReadAll();

            //header
            if (file.unusedLines.Count < 4 || file.colNamesLine < 4) return false;

            Dictionary<string, string> headerFields = new();
            foreach (KeyValuePair<int, string> kvp in file.unusedLines)
            {
                string line = kvp.Value.Substring(1);
                if (kvp.Key < file.colNamesLine && line.Length > 0)
                {
                    int firstDelim = line.IndexOf(':');
                    if (firstDelim != -1) headerFields[line.Substring(0, firstDelim)] = line.Substring(firstDelim + 1);
                }
            }

            if (!headerFields.ContainsKey("package") || headerFields["package"] != Constants.GUID) return false;
            if (!headerFields.ContainsKey("module") || headerFields["module"] != typeof(MovementLogger).ToString()) return false;
            if (!headerFields.ContainsKey("data-type") || headerFields["data-type"] != typeof(MovementAction).ToString()) return false;
            if (!headerFields.ContainsKey("version") || headerFields["version"] != Constants.VERSION) return false;

            //col names
            if (file.fieldNames.Except(file.colNames).Count() > 0) return false;
            if (file.colNames.Except(file.fieldNames).Count() > 0) return false;

            //line lengths and read errors
            foreach (KeyValuePair<int, List<string>> kvp in file.rows)
            {

            }
        }
    }

    public bool IsValid(MovementDataFile file)
    {

    }

}

public class MovementActionsFile : IndexedMovementDataFile<MovementAction>
{
    public MovementActionsFile(string path, List<string> fieldNames) : base(path, fieldNames)
    {
    }
}
