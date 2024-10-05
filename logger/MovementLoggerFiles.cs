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
        IEnumerable<IMovementDataFileVersion<T>> Versions { get; }
        Func<Dictionary<string, string>, T> Factory { get; }
        bool Check { get; set; }
    }

    class MovementLoggerFileData<T> : IMovementLoggerFileData<T> where T : IMovementData
    {
        public IEnumerable<IMovementDataFileVersion<T>> Versions { get; }
        public Func<Dictionary<string, string>, T> Factory { get; }
        public bool Check { get; set; }

        public MovementLoggerFileData(IEnumerable<IMovementDataFileVersion<T>> versions, Func<Dictionary<string, string>, T> factory)
        {
            Versions = versions;
            Factory = factory;
            Check = false;
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
        this.actionsFile = new(Path.Combine(path, "actions.tsv"));
        this.statesFile = new(Path.Combine(path, "states.tsv"));
        this.nodesFile = new(Path.Combine(path, "nodes.tsv"));
        this.objectsFile = new(Path.Combine(path, "objects.tsv"));
        this.edgesFile = new(Path.Combine(path, "edges.tsv"));
        this.edgeRunsFile = new(Path.Combine(path, "edge-runs.tsv"));
        this.haileeEdgeRunsFile = new(Path.Combine(path, "hailee-edge-runs.tsv"));

        this.data = new()
        {
            { this.actionsFile, new MovementLoggerFileData<MovementAction>(MovementAction.versions, (d) => new MovementAction(d)) },
            { this.statesFile, new MovementLoggerFileData<MovementState>(MovementState.versions, (d) => new MovementState(d)) },
            { this.nodesFile, new MovementLoggerFileData<MovementNode>(MovementNode.versions, (d) => new MovementNode(d)) },
            { this.objectsFile, new MovementLoggerFileData<MovementObject>(MovementObject.versions, (d) => new MovementObject(d)) },
            { this.edgesFile, new MovementLoggerFileData<MovementEdge>(MovementEdge.versions, (d) => new MovementEdge(d, this.nodesFile.parsedData, this.actionsFile.parsedData, this.statesFile.parsedData)) },
            { this.edgeRunsFile, new MovementLoggerFileData<MovementEdgeRun>(MovementEdgeRun.versions, (d) => new MovementEdgeRun(d, this.edgesFile.parsedData)) },
            { this.haileeEdgeRunsFile, new MovementLoggerFileData<MovementEdgeRun>(MovementEdgeRun.versions, (d) => new MovementEdgeRun(d, this.edgesFile.parsedData)) },
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
                Dictionary<int, bool> parses = file.Parse();
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