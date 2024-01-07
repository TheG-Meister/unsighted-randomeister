using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.unsighted;

public class Items
{

    public List<ItemObject> itemObjects;
    public Lists lists;

    public Items(Lists lists)
    {
        this.lists = lists;
        this.itemObjects = new();

        this.itemObjects.AddRange(this.lists.itemDatabase.itemList);
        this.itemObjects.AddRange(this.lists.cogsDatabase.cogList);
        this.itemObjects.AddRange(this.lists.chipDatabase.chipList);
        this.itemObjects.AddRange(this.lists.weaponDatabase.weaponList);
        this.itemObjects.AddRange(this.lists.armorDatabase.armorList);
    }

    public ItemObject GetItemObject(string name)
    {
        if (name == null) throw new ArgumentNullException("name");

        return this.itemObjects.Find(item => item.itemName == name) ?? throw new ArgumentException($"Could not find an item for name {name}");
    }

}
