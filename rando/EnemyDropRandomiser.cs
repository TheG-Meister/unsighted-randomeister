using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.rando;

public class EnemyDropRandomiser
{

    private Random random;
    private List<string> itemPool;
    private Dictionary<string, int> itemPoolLengths;

    public EnemyDropRandomiser(Random random, List<string> itemPool, Dictionary<string, int> itemPoolLengths)
    {
        this.random = random;
        this.itemPool = itemPool;
        this.itemPoolLengths = itemPoolLengths;
    }

    public Dictionary<string, DropController> Randomise()
    {
        Dictionary<string, DropController> result = new();

        foreach (string enemy in this.itemPoolLengths.Keys)
        {
            List<string> dropList = new List<string>();
            while (dropList.Count < this.itemPoolLengths[enemy] - 1) dropList.Add("");
            dropList.Add(this.itemPool[this.random.Next(this.itemPool.Count)]);

            result.Add(enemy, new DropController() { enemyNameID = enemy, itemList = dropList });
        }

        return result;
    }

}
