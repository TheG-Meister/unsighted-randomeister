using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementLoggerFileZip : MovementLoggerFiles
{

    private ZipArchive archive;

    public MovementLoggerFileZip()
    {
    }

    public MovementLoggerFileZip(string path) : this(ZipFile.Open(path, ZipArchiveMode.Update))
    {
    }

    public MovementLoggerFileZip(ZipArchive archive)
    {
        this.archive = archive;
    }

    public void CreateAll(string path) => this.CreateAll(ZipFile.Open(path, ZipArchiveMode.Update));

    public void CreateAll(ZipArchive archive)
    {
        if (this.archive != null && this.archive != archive) this.archive.Dispose();
        this.archive = archive;
        this.CreateAll();
    }

    public override void CreateAll()
    {
        this.Clear();
        List<Stream> streams = files.Select(f => archive.CreateEntry(f).Open()).ToList();
        this.Open(streams[0], streams[1], streams[2], streams[3], streams[4], streams[5], streams[6]);
    }

    public void Open(string path) => this.Open(ZipFile.Open(path, ZipArchiveMode.Update));

    public void Open(ZipArchive archive)
    {
        if (this.archive != null && this.archive != archive) this.archive.Dispose();
        this.archive = archive;
        this.Open();
    }

    public void Open()
    {
        List<Stream> streams = files.Select(f => archive.GetEntry(f).Open()).ToList();
        this.Open(streams[0], streams[1], streams[2], streams[3], streams[4], streams[5], streams[6]);
    }

    public void Clear()
    {
        while (this.archive.Entries.Count > 0) this.archive.Entries[0].Delete();
    }

    public override void Dispose()
    {
        base.Dispose();
        this.archive.Dispose();
        GC.SuppressFinalize(this);
    }

}
