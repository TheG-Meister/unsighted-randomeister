using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public abstract class MovementLoggerFiles : IDisposable
{
    public static readonly List<string> files = new() { "actions.tsv", "states.tsv", "nodes.tsv", "objects.tsv", "edges.tsv", "edge-runs.tsv", "hailee-edge-runs.tsv" };

    public interface IMovementLoggerFileData<out T> where T : IMovementData
    {
        public Dictionary<int, Exception> Parses { get; set; }
        public IEnumerable<IMovementDataFile> Dependencies { get; }
        public List<Exception> Exceptions { get; set; }
    }

    public class MovementLoggerFileData<T> : IMovementLoggerFileData<T> where T : IMovementData
    {
        public Dictionary<int, Exception> Parses { get; set; }
        public IEnumerable<IMovementDataFile> Dependencies { get; set; }
        public List<Exception> Exceptions { get; set; }

        public MovementLoggerFileData(params IMovementDataFile[] dependencies)
        {
            Parses = new();
            Dependencies = new List<IMovementDataFile>(dependencies);
            this.Exceptions = new();
        }
    }

    public IndexedMovementDataFile<MovementAction> actionsFile;
    public IndexedMovementDataFile<MovementState> statesFile;
    public IndexedMovementDataFile<MovementNode> nodesFile;
    public MovementDataFile<MovementObject> objectsFile;
    public IndexedMovementDataFile<MovementEdge> edgesFile;
    public MovementDataFile<MovementEdgeRun> edgeRunsFile;
    public MovementDataFile<MovementEdgeRun> haileeEdgeRunsFile;

    public bool exists;
    public bool parsed;

    public Dictionary<IMovementDataFile, IMovementLoggerFileData<IMovementData>> data;

    public MovementLoggerFiles()
    {
    }

    protected void Open(Stream actions, Stream states, Stream nodes, Stream objects, Stream edges, Stream edgeRuns, Stream haileeEdgeRuns)
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
    }

    public abstract bool ContainsAll();

    public abstract void Open();

    public abstract void CreateAll();

    protected void CreateAllFiles()
    {
        foreach (IMovementDataFile file in this.data.Keys) file.Create();
    }

    public virtual void ReadAll()
    {
        foreach (IMovementDataFile file in this.data.Keys)
        {
            try
            {
                file.ReadAll();
                file.FindVersion();
            }
            catch (Exception e)
            {
                this.data[file].Exceptions.Add(e);
            }
        }
    }

    public virtual void ParseAll()
    {
        foreach (IMovementDataFile file in this.data.Keys)
        {
            List<IMovementDataFile> dependencies = new(this.data[file].Dependencies);
            for (int i = 0; i < dependencies.Count; i++)
            {
                IMovementDataFile dependency = dependencies[i];
                if (this.data[dependency].Exceptions.Count < 0)
                {
                    this.data[file].Exceptions.Add(new IOException("one or more of this file's dependencies did not parse"));
                    break;
                }

                foreach (IMovementDataFile d2 in this.data[dependency].Dependencies) if (!dependencies.Contains(d2)) dependencies.Add(d2);
            }

            if (this.data[file].Exceptions.Count < 1)
            {
                try
                {
                    this.data[file].Parses = file.Parse();
                }
                catch (Exception e)
                {
                    this.data[file].Exceptions.Add(e);
                }
                if (this.data[file].Parses.Values.Any(e => e != null)) this.data[file].Exceptions.Add(new IOException("not all lines were parsed successfully"));
            }
        }

        this.parsed = !this.data.Values.SelectMany(d => d.Exceptions).Any();
    }

    public virtual void CloseAll()
    {
        List<IMovementDataFile> files = new() { this.actionsFile, this.statesFile, this.nodesFile, this.objectsFile, this.edgesFile, this.edgeRunsFile, this.haileeEdgeRunsFile };
        this.data.Clear();
        foreach (IMovementDataFile file in files) file?.Dispose();
    }

    public virtual void Dispose()
    {
        this.CloseAll();
        GC.SuppressFinalize(this);
    }

}