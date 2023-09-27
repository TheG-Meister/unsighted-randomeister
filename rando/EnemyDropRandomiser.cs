using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.rando;

public class EnemyDropRandomiser
{

    public static readonly Dictionary<string, int> ITEM_POOL_LENGTHS = new Dictionary<string, int>()
    {
        {"BubbleHeadEnemy", 3},
        {"MosquitoEnemy", 3},
        {"SpiderEnemy", 3},
        {"ArcherEnemy", 2},
        {"ScrapRobotEnemy", 2},
        {"CrabEnemy", 2},
        {"CyberWarriorEnemy", 2},
        {"GhoulEnemyController", 2},
        {"HopSpiderEnemy", 2},
        {"Moonface", 2},
        {"PantherEnemy", 2},
        {"SamuraiEnemy", 2},
        {"SlugEnemy", 2},
        {"ArmadilloMechaEnemy", 1},
        {"BeetleEnemy", 1},
        {"DrillMechaEnemy", 1},
        {"FireMecha", 1},
        {"Harpie", 1},
        {"JellyfishEnemy", 1},
        {"SharkEnemyController", 1},
    };

    public static readonly List<string> ITEM_POOL = new List<string>() { "MetalCog", "WoodenBranch", "MetalScrap", "IronBar", "Wires", "IceCrystal", "EletricCoil", "FlammableOil", "EngineOil", "Fuse" };

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
