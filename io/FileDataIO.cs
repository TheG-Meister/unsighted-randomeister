using dev.gmeister.unsighted.randomeister.data;
using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static dev.gmeister.unsighted.randomeister.core.Constants;

namespace dev.gmeister.unsighted.randomeister.io;

public class FileDataIO
{

    private FileNumber number;

    public FileDataIO(FileNumber number)
    {
        if (number == null) throw new ArgumentNullException(nameof(number));
        if (!number.IsValid()) throw new ArgumentException("FileNumber is not valid", nameof(number));
        if (!number.IsStory()) throw new ArgumentException("FileNumber is not a story file number", nameof(number));

        this.number = number;
    }

    public string Path()
    {
        return PATH_DEFAULT + PATH_FILE_DATA + "file-" + (this.number.Index() + 1) + ".dat";
    }

    public bool Exists()
    {
        return File.Exists(this.Path());
    }

    public FileData Read()
    {
        if (!this.Exists()) throw new ArgumentException("There is no randomisation data for story file " + this.number.Index());
        return Serializer.Load<FileData>(this.Path());
    }

    public void Copy(FileDataIO other)
    {
        if (!this.Exists()) throw new ArgumentException("There is no randomisation data for story file " + this.number.Index());
        File.Copy(this.Path(), other.Path());
    }

    public void Write(FileData data)
    {
        string path = this.Path();
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        Serializer.Save(path, data);
    }

    public void Delete()
    {
        File.Delete(this.Path());
    }

}
