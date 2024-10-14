using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementLoggerFiles
{

    private interface IMovementLoggerFileData<out T> where T : IMovementData
    {
        bool Check { get; set; }
        Dictionary<int, bool> Parses { get; set; }
        IEnumerable<IMovementDataFile> Dependencies { get; set; }
    }

    class MovementLoggerFileData<T> : IMovementLoggerFileData<T> where T : IMovementData
    {
        public bool Check { get; set; }
        public Dictionary<int, bool> Parses { get; set; }
        public IEnumerable<IMovementDataFile> Dependencies { get; set; }

        public MovementLoggerFileData(params IMovementDataFile[] dependencies)
        {
            Check = false;
            Parses = new();
            Dependencies = new List<IMovementDataFile>(dependencies);
        }
    }

    public IndexedMovementDataFile<MovementAction> actionsFile;
    public IndexedMovementDataFile<MovementState> statesFile;
    public IndexedMovementDataFile<MovementNode> nodesFile;
    public MovementDataFile<MovementObject> objectsFile;
    public IndexedMovementDataFile<MovementEdge> edgesFile;
    public MovementDataFile<MovementEdgeRun> edgeRunsFile;
    public MovementDataFile<MovementEdgeRun> haileeEdgeRunsFile;

    private Dictionary<IMovementDataFile, IMovementLoggerFileData<IMovementData>> data;

    public MovementLoggerFiles(string path)
    {
        this.actionsFile = new(Path.Combine(path, "actions.tsv"), (d) => new MovementAction(d), MovementAction.versions);
        this.statesFile = new(Path.Combine(path, "states.tsv"), (d) => new MovementState(d), MovementState.versions);
        this.nodesFile = new(Path.Combine(path, "nodes.tsv"), (d) => new MovementNode(d), MovementNode.versions);
        this.objectsFile = new(Path.Combine(path, "objects.tsv"), (d) => new MovementObject(d), MovementObject.versions);
        this.edgesFile = new(Path.Combine(path, "edges.tsv"), (d) => new MovementEdge(d, this.nodesFile.parsedData, this.actionsFile.parsedData, this.statesFile.parsedData), MovementEdge.versions);
        this.edgeRunsFile = new(Path.Combine(path, "edge-runs.tsv"), (d) => new MovementEdgeRun(d, this.edgesFile.parsedData), MovementEdgeRun.versions);
        this.haileeEdgeRunsFile = new(Path.Combine(path, "hailee-edge-runs.tsv"), (d) => new MovementEdgeRun(d, this.edgesFile.parsedData), MovementEdgeRun.versions);

        this.data = new()
        {
            { this.actionsFile, new MovementLoggerFileData<MovementAction>() },
            { this.statesFile, new MovementLoggerFileData<MovementState>() },
            { this.nodesFile, new MovementLoggerFileData<MovementNode>() },
            { this.objectsFile, new MovementLoggerFileData<MovementObject>() },
            { this.edgesFile, new MovementLoggerFileData<MovementEdge>(this.nodesFile, this.actionsFile, this.statesFile) },
            { this.edgeRunsFile, new MovementLoggerFileData<MovementEdgeRun>(this.edgesFile) },
            { this.haileeEdgeRunsFile, new MovementLoggerFileData<MovementEdgeRun>(this.edgesFile) },
        };

        foreach (IMovementDataFile file in this.data.Keys)
        {
            if (file.Exists()) this.data[file].Check = file.FindVersion();
            else
            {
                file.CreateAndWriteHeader();
                this.data[file].Check = true;
            }
        }

        foreach (IMovementDataFile file in this.data.Keys) if (this.data[file].Check)
            {
                this.data[file].Parses = file.Parse();
            }

    }

    public void CreateZip(List<string> files, string path)
    {
        string tempDir = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
        Random random = new();
        if (Directory.Exists(tempDir))
        {
            tempDir += "-temp-";
            do
            {
                tempDir += Constants.ALPHANUMERIC_CHARS[random.Next(Constants.ALPHANUMERIC_CHARS.Length)];
            }
            while (Directory.Exists(tempDir));
        }
        Directory.CreateDirectory(tempDir);

        foreach (string file in files) File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)));
        ZipFile.CreateFromDirectory(tempDir, path);
        Directory.Delete(tempDir, true);
    }

}