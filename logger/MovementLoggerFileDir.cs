using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementLoggerFileDir : MovementLoggerFiles
{

    public string directory;
    public FileMode mode;

    public MovementLoggerFileDir()
    {
    }

    public MovementLoggerFileDir(string directory) : this(directory, FileMode.OpenOrCreate)
    {
    }

    public MovementLoggerFileDir(string directory, FileMode mode)
    {
        this.directory = directory;
        this.mode = mode;
    }

    public void CreateAll(string directory)
    {
        this.directory = directory;
        this.CreateAll();
    }

    public override void CreateAll()
    {
        this.mode = FileMode.Create;
        this.Open();
    }

    public void Open(string directory, FileMode mode)
    {
        this.directory = directory;
        this.mode = mode;
        this.Open();
    }

    public void Open()
    {
        List<Stream> streams = files.Select(f => new FileStream(Path.Combine(this.directory, f + ".tsv"), this.mode)).Cast<Stream>().ToList();
        this.Open(streams[0], streams[1], streams[2], streams[3], streams[4], streams[5], streams[6]);
    }

}
