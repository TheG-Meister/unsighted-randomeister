using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister;

public class RandomisationSettings : RandomisationData
{
    public bool useRandomeister;
    public bool randomSeed = true;
    public bool randomiseChests = true;

    public RandomisationSettings(bool useRandomeister, List<string> items) : base(items)
    {
        this.useRandomeister = useRandomeister;
    }
}
