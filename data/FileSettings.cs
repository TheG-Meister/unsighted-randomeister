using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.data;

public class FileSettings
{
    public bool useRandomeister;
    public bool randomSeed = true;
    public bool randomiseChests = true;
    public bool randomiseEnemyDrops = true;
    public string chestItemPool = "";
    public FileData data;

    public FileSettings(bool useRandomeister, FileData data)
    {
        this.useRandomeister = useRandomeister;
        this.data = data;
    }
}
