using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.unsighted;

public class NPCDataTools
{

    public static Dictionary<string, List<string>> GetNPCShopListings(Lists lists)
    {
        Dictionary<string, List<string>> result = new();

        foreach (NPCObject npc in lists.npcDatabase.npcList)
        {
            result.Add(npc.npcName, npc.itemsOnSale);
        }

        return result;
    }

    public static void SetNPCShopListings(Lists lists, Dictionary<string, List<string>> listings)
    {
        foreach (NPCObject npc in lists.npcDatabase.npcList) if (listings.ContainsKey(npc.npcName))
            {
                npc.itemsOnSale = listings[npc.npcName];
            }
    }

}
