using dev.gmeister.unsighted.randomeister.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static dev.gmeister.unsighted.randomeister.core.Constants;

namespace dev.gmeister.unsighted.randomeister.io;

public class FileDataIO
{

    public int slot;

    public FileDataIO(int slot)
    {
        this.slot = slot;
    }

    public string GetPath()
    {
        return PATH_DEFAULT + PATH_FILE_DATA + "file-" + (this.slot + 1) + ".dat";
    }

    public bool Exists()
    {
        return File.Exists(this.GetPath());
    }

    public FileData Read()
    {
        if (this.slot < 0 || this.slot >= PAGES_PER_MODE * SLOTS_PER_PAGE) throw new ArgumentException(this.slot + " is not a valid story file slot index");
        if (!this.Exists()) throw new ArgumentException("There is no randomisation data for story file " + this.slot);
        return Serializer.Load<FileData>(this.GetPath());
    }

    public void CopyTo(int slot)
    {
        new FileDataIO(slot).Write(this.Read());
    }

    public void Write(FileData data)
    {
        string path = this.GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Serializer.Save(path, data);
    }

    public void Delete()
    {
        File.Delete(GetPath());
    }

}
