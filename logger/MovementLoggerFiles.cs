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
    public IndexedMovementDataFile<MovementAction> actionsFile;
    public IndexedMovementDataFile<MovementState> statesFile;
    public IndexedMovementDataFile<MovementNode> nodesFile;
    public MovementDataFile<MovementObject> objectsFile;
    public IndexedMovementDataFile<MovementEdge> edgesFile;
    public MovementDataFile<MovementEdgeRun> edgeRunsFile;
    public MovementDataFile<MovementEdgeRun> haileeEdgeRunsFile;

    public MovementLoggerFiles(string path)
    {
        this.actionsFile = new(Path.Combine(path, "actions.tsv"));
        this.statesFile = new(Path.Combine(path, "states.tsv"));
        this.nodesFile = new(Path.Combine(path, "nodes.tsv"));
        this.objectsFile = new(Path.Combine(path, "objects.tsv"));
        this.edgesFile = new(Path.Combine(path, "edges.tsv"));
        this.edgeRunsFile = new(Path.Combine(path, "edge-runs.tsv"));
        this.haileeEdgeRunsFile = new(Path.Combine(path, "hailee-edge-runs.tsv"));

        List<IMovementDataFile<IMovementData>> files = new() { actionsFile, statesFile, nodesFile, objectsFile, edgesFile, edgeRunsFile, haileeEdgeRunsFile };
        List<bool> checks = new();

        foreach (IMovementDataFile<IMovementData> file in files)
        {
            if (file.Exists()) checks.Add(this.CheckFile(file, MovementAction.versions));
            else
            {
                file.CreateAndWriteHeader();
                checks.Add(true);
            }
        }

        for (int i = 0; i < files.Count; i++)
        {
            IMovementDataFile<IMovementData> file = files[i];
            bool check = checks[i];
            if (check)
            {
                this.ParseFile(file, null);
            }
        }

        if (!this.actionsFile.Exists()) this.actionsFile.CreateAndWriteHeader(MovementAction.GetCurrentVersion());
        else
        {
            IndexedMovementDataFile<MovementAction> file = this.actionsFile;
            file.ReadAll();
            if (!file.header.TryGetValue("version", out string version)) ; //backup and restore

        }

        if (!this.CheckFile(this.actionsFile, MovementAction.versions)) ;


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

    public bool CheckFile<T>(MovementDataFile<T> file, Dictionary<string, MovementDataFileVersion<T>> versions) where T : IMovementData
    {
        if (!file.Exists()) return false;

        file.ReadAll();
        if (!file.header.TryGetValue("version", out string versionString)) return false;
        if (!versions.TryGetValue(versionString, out MovementDataFileVersion<T> version)) return false;
        if (!version.VerifyHeader(file.header)) return false;
        if (!version.VerifyColNames(file.colNames)) return false;

        file.version = version;

        return true;
    }

    public void ParseFile<T>(MovementDataFile<T> file, Func<Dictionary<string, string>, T> factory) where T : IMovementData
    {
        file.parsedData = new();
        foreach (int line in file.rows.Keys)
        {
            Dictionary<string, string> fields = file.GetEntry(line);
            try
            {
                file.parsedData[line] = factory.Invoke(fields);
            }
            catch
            { }
        }
    }

    public List<int> ParseFile<T>(IndexedMovementDataFile<T> file, Func<Dictionary<string, string>, T> factory) where T : IndexedMovementData
    {
        file.parsedData = new();
        List<int> failedIDs = new();
        foreach (int line in file.rows.Keys)
        {
            Dictionary<string, string> fields = file.GetEntry(line);
            try
            {
                file.parsedData[line] = factory.Invoke(fields);
            }
            catch
            {
                if (fields.ContainsKey("id") && int.TryParse(fields["id"], out int id)) failedIDs.Add(id);
            }
        }

        return failedIDs;
    }
}
