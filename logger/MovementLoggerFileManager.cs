using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementLoggerFileManager
{

    public string path;
    public string backupsDir;
    public string completeDir;
    public string brokenDir;
    public string currentZip;
    public MovementLoggerFileZip currentBatch;
    public MovementLoggerFileZip currentCommandBatch;

    public MovementLoggerFileManager(string path)
    {
        this.path = path;
        this.backupsDir = Path.Combine(this.path, "backups");
        this.completeDir = Path.Combine(this.path, "complete");
        this.brokenDir = Path.Combine(this.path, "broken");
        this.currentZip = Path.Combine(this.path, "current.zip");

        Directory.CreateDirectory(this.path);
        Directory.CreateDirectory(this.backupsDir);
        Directory.CreateDirectory(this.completeDir);
        Directory.CreateDirectory(this.brokenDir);

        if (!Path.Exists(this.currentZip))
        {
            this.CreateCurrentZip();
            this.currentBatch = new(this.currentZip);
            this.currentBatch.CreateAll();
            this.currentBatch.ResetAll();
        }
        else
        {
            this.currentBatch = new(this.currentZip);
            this.currentBatch.Open();
        }
    }

    private void CreateCurrentZip()
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
        ZipFile.CreateFromDirectory(tempDir, path);
        Directory.Delete(tempDir, true);
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