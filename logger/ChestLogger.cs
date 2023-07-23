using dev.gmeister.unsighted.randomeister.core;
using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.logger;

public class ChestLogger
{
    private string dir;

    public ChestLogger(string dir)
    {
        this.dir = dir;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    public void LogChest(string scene, string chest, List<ItemObject> items)
    {
        List<Ability> abilities = items.FindAll(item => AbilityTools.itemAbilities.ContainsKey(item.itemName)).SelectMany(item => AbilityTools.itemAbilities[item.itemName]).Distinct().ToList();
        abilities.Sort();

        if (!abilities.Any()) return;

        string file = "";
        for (int i = 0; i < abilities.Count; i++)
        {
            file += abilities[i].ToString().ToLower();
            if (i < abilities.Count - 1) file += "-";
        }
        file += ".tsv";

        string path = Path.Combine(dir, file);

        string line = scene + "-" + chest;
        File.AppendAllLines(path, new List<string>() { line });
    }

}
