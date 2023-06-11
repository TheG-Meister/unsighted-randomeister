using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.data;

public class RandomisationSettings
{
    public bool useRandomeister;
    public bool randomSeed = true;
    public bool randomiseChests = true;
    public string chestItemPool = "";
    public RandomisationData data;

    public RandomisationSettings(bool useRandomeister, RandomisationData data)
    {
        this.useRandomeister = useRandomeister;
        this.data = data;
    }
}
