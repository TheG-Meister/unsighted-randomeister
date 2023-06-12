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
    public List<string> items;
    public bool removeFragileOnJumpBootsChest = true;

    public FileData(List<string> items)
    {
        this.items = items;
    }
}
