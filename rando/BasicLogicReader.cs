using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.rando;

public class BasicLogicReader
{

    private ChestList chestList;

    public BasicLogicReader(ChestList chestList)
    {
        this.chestList = chestList;
    }

    public Dictionary<HashSet<Ability>, List<ChestObject>> Read()
    {
        Dictionary<HashSet<Ability>, List<ChestObject>> result = new();

        List<string> rawLines = Properties.Resources.BasicLogic.Replace("\r", "").Split('\n').ToList();
        rawLines.RemoveAll(line => string.IsNullOrEmpty(line));

        foreach (string line in rawLines)
        {
            string[] split = line.Split('\t');
            if (split.Length != 2) throw new ApplicationException("A line did not have 2 fields");

            HashSet<Ability> abilities = new();
            string[] abilityNames = split[0].Split(',');
            foreach (Ability ability in Enum.GetValues(typeof(Ability)))
            {
                if (abilityNames.Contains(ability.ToString().ToLower())) abilities.Add(ability);
            }

            List<ChestObject> chests = new();
            string[] chestNames = split[1].Split(',');
            foreach (AreaChestList areaChestList in this.chestList.areas)
                foreach (ChestObject chest in areaChestList.chestList)
                {
                    if (chestNames.Contains(Chests.GetChestID(chest))) chests.Add(chest);
                }

            result.Add(abilities, chests);
        }

        return result;
    }

}
