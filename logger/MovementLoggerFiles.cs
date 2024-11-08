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
        Exception Exception { get; set; }
    }

    class MovementLoggerFileData<T> : IMovementLoggerFileData<T> where T : IMovementData
    {
        public bool Check { get; set; }
        public Dictionary<int, bool> Parses { get; set; }
        public IEnumerable<IMovementDataFile> Dependencies { get; set; }
        public Exception Exception { get; set; }

        public MovementLoggerFileData(params IMovementDataFile[] dependencies)
        {
            Check = false;
            Parses = new();
            Dependencies = new List<IMovementDataFile>(dependencies);
            this.Exception = null;
        }
    }

    public IndexedMovementDataFile<MovementAction> actionsFile;
    public IndexedMovementDataFile<MovementState> statesFile;
    public IndexedMovementDataFile<MovementNode> nodesFile;
    public MovementDataFile<MovementObject> objectsFile;
    public IndexedMovementDataFile<MovementEdge> edgesFile;
    public MovementDataFile<MovementEdgeRun> edgeRunsFile;
    public MovementDataFile<MovementEdgeRun> haileeEdgeRunsFile;

    public bool parsed;

    private Dictionary<IMovementDataFile, IMovementLoggerFileData<IMovementData>> data;

    public MovementLoggerFiles(string directory)
    {
        List<string> files = new() { "actions", "states", "nodes", "objects", "edges", "edge-runs", "hailee-edge-runs" };
        List<FileStream> streams = files.Select(f => new FileStream(Path.Combine(directory, f + ".tsv"), FileMode.Open)).ToList();
        Initialise(streams.Cast<Stream>().ToList());
    }

    public MovementLoggerFiles(ZipArchive zip)
    {
        List<string> files = new() { "actions", "states", "nodes", "objects", "edges", "edge-runs", "hailee-edge-runs" };
        List<Stream> streams = files.Select(f => zip.GetEntry(f + ".tsv").Open()).ToList();
        Initialise(streams);
    }

    public MovementLoggerFiles(Stream actions, Stream states, Stream nodes, Stream objects, Stream edges, Stream edgeRuns, Stream haileeEdgeRuns)
    {
        Initialise(actions, states, nodes, objects, edges, edgeRuns, haileeEdgeRuns);
    }

    protected void Initialise(List<Stream> streams)
    {
        Initialise(streams[0], streams[1], streams[2], streams[3], streams[4], streams[5], streams[6]);
    }

    public void Initialise(Stream actions, Stream states, Stream nodes, Stream objects, Stream edges, Stream edgeRuns, Stream haileeEdgeRuns)
    {
        this.actionsFile = new(actions, (d) => new MovementAction(d), MovementAction.versions);
        this.statesFile = new(states, (d) => new MovementState(d), MovementState.versions);
        this.nodesFile = new(nodes, (d) => new MovementNode(d), MovementNode.versions);
        this.objectsFile = new(objects, (d) => new MovementObject(d), MovementObject.versions);
        this.edgesFile = new(edges, (d) => new MovementEdge(d, this.nodesFile.parsedData, this.actionsFile.parsedData, this.statesFile.parsedData), MovementEdge.versions);
        this.edgeRunsFile = new(edgeRuns, (d) => new MovementEdgeRun(d, this.edgesFile.parsedData), MovementEdgeRun.versions);
        this.haileeEdgeRunsFile = new(haileeEdgeRuns, (d) => new MovementEdgeRun(d, this.edgesFile.parsedData), MovementEdgeRun.versions);

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
            try
            {
                file.ReadAll();
                this.data[file].Check = file.FindVersion();
                if (!this.data[file].Check) this.parsed = false;
            }
            catch (Exception e)
            {
                this.data[file].Exception = e;
                this.data[file].Check = false;
                this.parsed = false;
            }
        }

        foreach (IMovementDataFile file in this.data.Keys)
        {
            bool parse = this.data[file].Check;
            List<IMovementDataFile> dependencies = new(this.data[file].Dependencies);
            for (int i = 0; i < dependencies.Count; i++)
            {
                IMovementDataFile dependency = dependencies[i];
                if (!this.data[dependency].Check)
                {
                    parse = false;
                    break;
                }

                foreach (IMovementDataFile d2 in this.data[dependency].Dependencies) if (!dependencies.Contains(d2)) dependencies.Add(d2);
            }

            if (parse)
            {
                try
                {
                    this.data[file].Parses = file.Parse();
                }
                catch (Exception e)
                {
                    this.data[file].Exception = e;
                }
            }
        }
    }

}