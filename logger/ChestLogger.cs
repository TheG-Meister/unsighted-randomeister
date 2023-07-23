using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class ChestLogger
{
    private string dir;

    public ChestLogger(string dir)
    {
        this.dir = dir;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }



}
