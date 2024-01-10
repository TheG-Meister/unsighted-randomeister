using BepInEx;
using BepInEx.Logging;
using dev.gmeister.unsighted.randomeister.data;
using dev.gmeister.unsighted.randomeister.io;
using dev.gmeister.unsighted.randomeister.logger;
using dev.gmeister.unsighted.randomeister.rando;
using dev.gmeister.unsighted.randomeister.unsighted;
using HarmonyLib;
using static dev.gmeister.unsighted.randomeister.core.Constants;

namespace dev.gmeister.unsighted.randomeister.core;

[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BaseUnityPlugin
{

    public PluginConfig options;

    public FileData currentData;

    private ChestList originalChestList;
    private Dictionary<string, float> originalItemPrices;
    private Dictionary<string, List<string>> originalShopListings;
    public Items items;

    public MovementLogger movementLogger;
    public ChestLogger chestLogger;

    public static Plugin Instance { get; private set; } = null!;

    public Dictionary<string, List<string>> itemPools;

    public Plugin()
    {
        if (Instance != null) throw new InvalidOperationException("There cannot be more than one instance of this plugin");

        options = new(Config);

        itemPools = new()
        {
            { ALMOST_ALL_ITEMS_POOL, new() { "Key", "JumpBoots", "DisposableSyringe", "Bolts1", "Bolts2", "Bolts3", "Bolts4", "AncientClockGear", "AncientClockPendulum", "AncientClockHands", "AncientClockFace", "AttackCogBlueprint", "DefenseCogBlueprint", "ReloadCogBlueprint", "StaminaCogBlueprint", "SpeedCogBlueprint", "SyringeCogBlueprint", "ReviveCogBlueprint", "HealthChip", "StaminaChip", "StrengthChip", "DefenseChip", "InvincibilityChip", "SpinnerChip", "SteadyChip", "ShurikenChip", "SwordChip", "AxeChip", "RiskChip", "PowerChip", "VirusChip", "FatigueChip", "SpinChipA", "SpinChipB", "JumperChip", "RunnerChip", "SpeedChipA", "ReloadChip", "BulletChip", "DrifterChip", "SpeedChipB", "BoltChip", "WalletChip", "FasterHealChip", "VigorChip", "VampireChip", "ComboChipA", "ComboChipB", "SyringeChip", "AutoSyringeChip", "DoubleBarrelChip", "OffenseChip", "DogChip", "MerchantChip", "ScavengerChip", "AnimaChip", "ParryMasterChip", "CogChip", "BigHeartChip", "GlitchChip", "Blaster", "DoctorsGun", "Spinner", "Hookshot1", "AutomaticBlaster", "Shotgun", "Flamethrower", "Icethrower", "GranadeLauncher", "IceGranade", "GranadeShotgun", "IronEdge", "ThunderEdge", "Frostbite", "Flameblade", "ElementalBlade", "WarAxe", "IceAxe", "FireAxe", "ThunderAxe", "RaquelAxe", "IronStar", "IceStar", "FireStar", "ThunderStar", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Hookshot1", "AttackCog", "DefenseCog", "ReloadCog", "StaminaCog", "SpeedCog", "SyringeCog", "ReviveCog", "HealthChip", "StaminaChip", "HealthChip", "StaminaChip", "HealthChip", "StaminaChip", "HealthChip", "StaminaChip", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust" } }
        };

        movementLogger = new MovementLogger("unsighted-randomeister/logs/movement-log.tsv", this.options.movementLogging.Value, this.options.movementLoggingAnnouncements.Value, this.options.movementLoggingUniqueAnnouncements.Value);
        this.chestLogger = new ChestLogger(Path.Combine(Constants.PATH_DEFAULT, PATH_LOGS, Constants.PATH_CHEST_LOGS));

        Instance = this;

        Logger.LogInfo($"Loading {GUID}");
        new Harmony(GUID).PatchAll();
    }

    public void OnDisable()
    {
        this.movementLogger.Dispose();
    }

    public ManualLogSource GetLogger() { return Logger; }

    public void SetOriginalLists(Lists lists)
    {
        if (originalChestList == null)
        {
            originalChestList = lists.chestList;
            this.items = new Items(lists);
            itemPools.Add(VANILLA_POOL, originalChestList.areas.SelectMany(areaChestList => areaChestList.chestList).Select(chest => chest.reward).ToList());
        }

        this.originalItemPrices ??= ItemDatabases.GetItemPrices(lists);
        this.originalShopListings ??= NPCDataTools.GetNPCShopListings(lists);
    }

    public void ResetChestItems()
    {
        Logger.LogInfo("Unshuffling chests");
        PseudoSingleton<Lists>.instance.chestList = originalChestList;
    }

    public List<string> GetItemPool(string name)
    {
        if (itemPools.ContainsKey(name)) return itemPools[name];
        else return itemPools[VANILLA_POOL];
    }

    public List<string> GetRandomItems(List<string> items, System.Random random)
    {
        return items.OrderBy(item => random.NextDouble()).ToList();
    }

    public ChestList CreateChestList(Dictionary<string, string> chestItems)
    {
        ChestList chestList = Chests.CloneChestList(this.originalChestList);
        Dictionary<string, ChestObject> chestObjects = Chests.GetChestObjectDictionary(chestList);

        foreach (KeyValuePair<string, string> chestItem in chestItems) 
        {
            if (chestObjects.ContainsKey(chestItem.Key)) chestObjects[chestItem.Key].reward = chestItem.Value;
        }

        return chestList;
    }

    public void SetChestItems(Dictionary<string, string> chestItems)
    {
        ChestList chestList = this.CreateChestList(chestItems);
        PseudoSingleton<Lists>.instance.chestList = chestList;
    }

    public FileSettings CreateSettingsFromConfig()
    {
        FileData data = new(Chests.GetChestItemDictionary(this.originalChestList))
        {
            seed = options.seed.Value,
            removeFragileOnJumpBootsChest = options.removeFragileOnJumpBootsChest.Value,
            newChestRadar = options.newChestRadar.Value,
            chestRadarMoreOften = options.chestRadarMoreOften.Value,
        };

        return new(true, data)
        {
            randomSeed = options.randomSeed.Value,
            chestItemPool = options.chestItemPool.Value,
            randomiseChests = options.randomiseChests.Value,
            randomiseEnemyDrops = options.randomiseEnemyDrops.Value,
        };
    }

    public void SetDataFromSettings(FileSettings settings)
    {
        if (settings.useRandomeister)
        {
            if (settings.randomSeed) settings.data.seed = new System.Random().Next();
            Random random = new(settings.data.seed);

            Random chestRandom = new(random.Next());
            if (settings.randomiseChests)
            {
                settings.data.chestItems = new Randomiser(chestRandom, this.originalChestList, GetItemPool(settings.chestItemPool)).Randomise();
            }

            Random enemyDropRandom = new(random.Next());
            if (settings.randomiseEnemyDrops)
            {
                settings.data.enemyDropTables = new EnemyDropRandomiser(enemyDropRandom, EnemyDropRandomiser.ITEM_POOL, EnemyDropRandomiser.ITEM_POOL_LENGTHS).Randomise();
            }

            Random shopPriceRandom = new(random.Next());
            if (settings.randomiseItemPrices)
            {
                settings.data.itemPrices = new ShopRandomiser().RandomiseItemPrices(shopPriceRandom);
            }

            Random shopListingsRandom = new(random.Next());
            if (settings.randomiseShopListings)
            {
                settings.data.shopListings = new ShopRandomiser().RandomiseShopListings(shopListingsRandom);
            }
        }
    }

    public void CreateStoryFileRandomiser(FileDataIO fileDataIO, FileSettings settings)
    {
        SetDataFromSettings(settings);
        fileDataIO.Write(settings.data);
        LogChestRandomisation(settings.data.chestItems);
        LoadStoryFileRandomiser(settings.data);
    }

    public void LoadStoryFileRandomiser(FileData data)
    {
        currentData = data;
        SetChestItems(data.chestItems);
        ItemDatabases.SetItemPrices(PseudoSingleton<Lists>.instance, data.itemPrices);
        NPCDataTools.SetNPCShopListings(PseudoSingleton<Lists>.instance, data.shopListings);
    }

    public void CreateVanillaStoryFile(FileDataIO fileDataIO)
    {
        if (fileDataIO.Exists()) fileDataIO.Delete();
        LoadVanillaStoryFile();
    }

    public void LoadVanillaStoryFile()
    {
        currentData = null;
        ResetChestItems();
        ItemDatabases.SetItemPrices(PseudoSingleton<Lists>.instance, this.originalItemPrices);
        NPCDataTools.SetNPCShopListings(PseudoSingleton<Lists>.instance, this.originalShopListings);
    }

    public void LogChestRandomisation(Dictionary<string, string> chestItems)
    {
        Logger.LogInfo("Writing log data");
        List<string> logLines = new();

        foreach (KeyValuePair<string, string> chestItem in chestItems)
        {
            string scene, chest;
            Chests.GetChestLocation(chestItem.Key, out scene, out chest);

            logLines.Add($"{scene}, {chest} = {chestItem.Value}");
        }

        string logDir = "unsighted-randomeister/logs/randomisation/";
        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
        File.WriteAllLines(logDir + DateTime.Now.ToString("yy-MM-dd-HH-mm-ss-fff") + ".txt", logLines);
    }

    public void SetCurrentSlotAndRandomise(int gameSlot, bool newGame)
    {
        FileNumber number = new(gameSlot);
        if (number.IsValid() && number.IsStory())
        {
            FileDataIO fileDataIO = new(number);

            if (newGame)
            {
                if (options.useRandomeister.Value) CreateStoryFileRandomiser(fileDataIO, CreateSettingsFromConfig());
                else CreateVanillaStoryFile(fileDataIO);
            }
            else
            {
                if (fileDataIO.Exists()) LoadStoryFileRandomiser(fileDataIO.Read());
                else LoadVanillaStoryFile();
            }
        }
        else LoadVanillaStoryFile();
    }

    public void OnFileErased(SaveSlotButton button)
    {
        FileDataIO dataIO = new(new(button.saveSlot));
        if (dataIO.Exists()) dataIO.Delete();
    }

    public void OnFileCopied(SaveSlotPopup popup, int gameSlot)
    {
        FileNumber number = new(gameSlot);
        if (number.IsStory())
        {
            FileDataIO fileDataIO = new(number);

            if (fileDataIO.Exists()) for (int newSlot = 0; newSlot < PAGES_PER_MODE * SLOTS_PER_PAGE; newSlot++)
                {
                    FileNumber newNumber = new(newSlot, STORY_MODE);
                    if (!popup.SaveExist(newNumber.Get()))
                    {
                        fileDataIO.Copy(new(newNumber));
                        break;
                    }
                }
        }
    }

}