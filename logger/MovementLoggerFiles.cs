using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.IO;
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

    public MovementLoggerFiles(string directory) : this(GetStream(directory, "actions"), GetStream(directory, "states"), GetStream(directory, "nodes"), GetStream(directory, "objects"), GetStream(directory, "edges"), GetStream(directory, "edge-runs"), GetStream(directory, "hailee-edge-runs"))
    {

    }

    private static Stream GetStream(string directory, string file) => new FileStream(Path.Combine(directory, file + ".tsv"), FileMode.Open);

    public MovementLoggerFiles(Stream actionsFileStream, Stream statesFileStream, Stream nodesFileStream, Stream objectsFileStream, Stream edgesFileStream, Stream edgeRunsFileStream, Stream haileeEdgeRunsFileStream)
    {
        this.actionsFile = new(actionsFileStream, (d) => new MovementAction(d), MovementAction.versions);
        this.statesFile = new(statesFileStream, (d) => new MovementState(d), MovementState.versions);
        this.nodesFile = new(nodesFileStream, (d) => new MovementNode(d), MovementNode.versions);
        this.objectsFile = new(objectsFileStream, (d) => new MovementObject(d), MovementObject.versions);
        this.edgesFile = new(edgesFileStream, (d) => new MovementEdge(d, this.nodesFile.parsedData, this.actionsFile.parsedData, this.statesFile.parsedData), MovementEdge.versions);
        this.edgeRunsFile = new(edgeRunsFileStream, (d) => new MovementEdgeRun(d, this.edgesFile.parsedData), MovementEdgeRun.versions);
        this.haileeEdgeRunsFile = new(haileeEdgeRunsFileStream, (d) => new MovementEdgeRun(d, this.edgesFile.parsedData), MovementEdgeRun.versions);

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
            this.data[file].Check = file.FindVersion();
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