using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class Logger : IDisposable
{
    public StreamWriter stream;

    public Logger(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        stream = new StreamWriter(path, true);
    }

    public string BoolToString(bool value)
    {
        return value ? "1" : "";
    }

    public virtual void Dispose()
    {
        this.stream.Dispose();
    }

}
