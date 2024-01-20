using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.data;

/**
 * Contains data on how a save slot was randomised, and how to restore that randomisation when the file is reloaded.
**/
[Serializable]
public class FileData
{
    public int seed = 0;
    public Dictionary<string, string> chestItems;
    public bool removeFragileOnJumpBootsChest = false;

    public bool newChestRadar = false;
    public bool chestRadarMoreOften = false;

    public Dictionary<string, DropController> enemyDropTables = null;
    public Dictionary<string, float> itemPrices = null;
    public Dictionary<string, List<string>> shopListings = null;
    public Dictionary<string, string> crystalItems = null;

    public FileData(Dictionary<string, string> chestItems)
    {
        this.chestItems = chestItems;
    }
}
