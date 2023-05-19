using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister;

internal class RandomisationSettings : FileRandomisationData
{
    public bool useRandomeister;
    public bool randomSeed = true;
    public bool randomiseChests = true;

    public RandomisationSettings(bool useRandomeister, int seed, List<string> items) : base(seed, items)
    {
        this.useRandomeister = useRandomeister;
    }
}
