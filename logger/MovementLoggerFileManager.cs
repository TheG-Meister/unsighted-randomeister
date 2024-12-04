using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementLoggerFileManager : IDisposable
{

    public const string BACKUPS_DIR = "backups";
    public const string COMPLETE_DIR = "complete";
    public const string BROKEN_DIR = "broken";
    public const string CURRENT_DIR = "current";
    public const string CURRENT_ZIP = "current.zip";

    public readonly string path;
    public readonly string backupsDirPath;
    public readonly string completeDirPath;
    public readonly string brokenDirPath;
    public readonly string currentDirPath;
    public readonly string currentZipPath;
    public MovementLoggerFiles currentBatch;
    public MovementLoggerFileZip currentZip;
    public MovementLoggerFileDir currentDir;
    public MovementLoggerFiles currentCommandBatch;

    public bool debug;

    public MovementLoggerFileManager(string path, bool debug)
    {
        this.path = path;
        this.debug = debug;

        this.backupsDirPath = Path.Combine(this.path, BACKUPS_DIR);
        this.completeDirPath = Path.Combine(this.path, COMPLETE_DIR);
        this.brokenDirPath = Path.Combine(this.path, BROKEN_DIR);
        this.currentDirPath = Path.Combine(this.path, CURRENT_DIR);
        this.currentZipPath = Path.Combine(this.path, CURRENT_ZIP);

        Directory.CreateDirectory(this.path);
        Directory.CreateDirectory(Path.Combine(this.path, BACKUPS_DIR));
        Directory.CreateDirectory(Path.Combine(this.path, COMPLETE_DIR));
        Directory.CreateDirectory(Path.Combine(this.path, BROKEN_DIR));

        bool zipExists = File.Exists(this.currentZipPath);
        bool dirExists = Directory.Exists(this.currentDirPath);
        bool read = false;

        if (this.debug)
        {
            if (zipExists)
            {
                if (dirExists) Directory.Delete(this.currentDirPath, true);
                Directory.CreateDirectory(this.currentDirPath);
                ZipFile.ExtractToDirectory(this.currentZipPath, this.currentDirPath);

                read = true;
            }
            if (dirExists) read = true;
            this.currentBatch = new MovementLoggerFileDir(this.currentDirPath);
        }
        else
        {
            if (zipExists) read = true;
            else
            {
                if (dirExists)
                {
                    ZipFile.CreateFromDirectory(this.currentDirPath, this.currentZipPath);
                    read = true;
                }
                else this.CreateZip(this.currentZipPath);
            }

            Directory.Delete(this.currentDirPath, true);
            this.currentBatch = new MovementLoggerFileZip(this.currentZipPath);
        }

        if (read)
        {
            this.currentBatch.Open();
            this.currentBatch.ReadAll();
            this.currentBatch.ParseAll();
        }
        else this.currentBatch.CreateAll();

    }

    public void BackupDir()
    {
        if (File.Exists(this.currentDirPath)) ZipFile.CreateFromDirectory(this.currentDirPath, this.currentZipPath);
    }

    public void CreateZip(string path, params string[] files)
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

    public void Dispose()
    {
        this.currentBatch?.Dispose();
        this.currentCommandBatch?.Dispose();
        GC.SuppressFinalize(this);
    }
}