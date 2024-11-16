using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementLoggerFileZip : MovementLoggerFiles
{

    private readonly ZipArchive archive;

    public MovementLoggerFileZip(string path) : this(ZipFile.Open(path, ZipArchiveMode.Update))
    {
    }

    public MovementLoggerFileZip(ZipArchive archive)
    {
        this.archive = archive;
        List<Stream> streams = files.Select(f => archive.GetEntry(f).Open()).ToList();
        Initialise(streams[0], streams[1], streams[2], streams[3], streams[4], streams[5], streams[6]);
    }

    public override void Dispose()
    {
        base.Dispose();
        this.archive.Dispose();
        GC.SuppressFinalize(this);
    }

}
