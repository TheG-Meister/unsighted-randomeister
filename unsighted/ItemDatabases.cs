using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.unsighted;

internal class ItemDatabases
{

    public ItemDatabase items;
    public CogsDatabase cogs;
    public ChipDatabase chips;
    public WeaponDatabase weapons;
    public ArmorDatabase armours;

    public ItemDatabases()
    {

    }

    public ItemDatabases(ItemDatabase items, CogsDatabase cogs, ChipDatabase chips, WeaponDatabase weapons, ArmorDatabase armours)
    {
        this.items = items;
        this.cogs = cogs;
        this.chips = chips;
        this.weapons = weapons;
        this.armours = armours;
    }

    public static Dictionary<string, float> GetItemPrices(Lists lists)
    {
        Dictionary<string, float> result = new();

        List<ItemObject[]> arrays = new()
        {
            lists.itemDatabase.itemList,
            lists.cogsDatabase.cogList,
            lists.chipDatabase.chipList,
            lists.weaponDatabase.weaponList,
            lists.armorDatabase.armorList,
        };

        foreach (ItemObject[] array in arrays) foreach (ItemObject item in array) result.Add(item.itemName, item.itemValue);

        return result;
    }

    public static void SetItemPrices(Lists lists, Dictionary<string, float> itemPrices)
    {
        List<ItemObject[]> arrays = new()
        {
            lists.itemDatabase.itemList,
            lists.cogsDatabase.cogList,
            lists.chipDatabase.chipList,
            lists.weaponDatabase.weaponList,
            lists.armorDatabase.armorList,
        };

        foreach (ItemObject[] array in arrays) foreach (ItemObject item in array) if (itemPrices.ContainsKey(item.itemName)) item.itemValue = itemPrices[item.itemName];
    }

}
