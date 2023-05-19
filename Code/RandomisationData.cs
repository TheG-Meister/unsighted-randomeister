using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister;

/**
 * Contains data on how a save slot was randomised, and how to restore that randomisation when the file is reloaded.
**/
[Serializable]
internal class RandomisationData
{
    public int seed = 0;
    public List<string> items;

    public RandomisationData(List<string> items)
    {
        this.items = items;
    }
}
