using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class MovementLoggerFileDir : MovementLoggerFiles
{

    public MovementLoggerFileDir(string directory)
    {
        List<Stream> streams = files.Select(f => new FileStream(Path.Combine(directory, f + ".tsv"), FileMode.Open)).Cast<Stream>().ToList();
        Initialise(streams[0], streams[1], streams[2], streams[3], streams[4], streams[5], streams[6]);
    }

}
