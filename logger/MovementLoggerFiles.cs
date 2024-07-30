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

        this.actionsFile = new(Path.Combine(this.path, "actions.tsv"));
        this.statesFile = new(Path.Combine(this.path, "states.tsv"));
        this.nodesFile = new(Path.Combine(this.path, "nodes.tsv"));
        this.objectsFile = new(Path.Combine(this.path, "objects.tsv"));
        this.edgesFile = new(Path.Combine(this.path, "edges.tsv"));
        this.edgeRunsFile = new(Path.Combine(this.path, "edge-runs.tsv"));
        this.haileeEdgeRunsFile = new(Path.Combine(this.path, "hailee-edge-runs.tsv"));

        List<IMovementDataFile<IMovementData>> files = new();
        files.Add(actionsFile);

        IMovementDataFile<IMovementData> file = files[0];

        if (!this.actionsFile.Exists()) this.actionsFile.CreateAndWriteHeader(MovementAction.GetCurrentVersion());
        else
        {
            IndexedMovementDataFile<MovementAction> file = this.actionsFile;
            file.ReadAll();
            if (!file.header.TryGetValue("version", out string version)) ; //backup and restore
            
        }

        if (!this.CheckFile(this.actionsFile, MovementAction.versions));

        
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
            {}
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