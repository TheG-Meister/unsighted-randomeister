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
    public string backupsPath;
    public string completePath;

    public MovementLoggerFileManager(string path)
    {
        this.path = path;
        this.backupsPath = Path.Combine(this.path, "backups");
        this.completePath = Path.Combine(this.path, "complete");

        Directory.CreateDirectory(this.path);
        Directory.CreateDirectory(this.backupsPath);
        Directory.CreateDirectory(this.completePath);
        
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