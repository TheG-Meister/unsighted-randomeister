using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.rando;

public class MaterialCrystalRandomiser
{

    public static readonly List<string> ITEM_POOL = new List<string>() { "MetalCog", "WoodenBranch", "MetalScrap", "IronBar", "Wires", "IceCrystal", "EletricCoil", "FlammableOil", "EngineOil", "Fuse" };

    public Dictionary<string, string> RandomiseCrystalItems(Random random, List<string> crystals, List<string> items)
    {
        Dictionary<string, string> result = new();

        foreach (string crystal in crystals)
        {
            result.Add(crystal, items[random.Next(items.Count)]);
        }

        return result;
    }

}
