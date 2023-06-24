using BepInEx;
using BepInEx.Logging;
using dev.gmeister.unsighted.randomeister.data;
using dev.gmeister.unsighted.randomeister.io;
using dev.gmeister.unsighted.randomeister.rando;
using dev.gmeister.unsighted.randomeister.unsighted;
using HarmonyLib;
using static dev.gmeister.unsighted.randomeister.core.Constants;

namespace dev.gmeister.unsighted.randomeister.core;

[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BaseUnityPlugin
{

    public PluginConfig options;

    public FileData? currentData;

    private ChestList? originalChestList;

    public static Plugin Instance { get; private set; } = null!;

    public Dictionary<string, List<string>> itemPools;

    public Plugin()
    {
        if (Instance != null) throw new InvalidOperationException("There cannot be more than one instance of this plugin");

        options = new(Config);

        itemPools = new()
        {
            { ALMOST_ALL_ITEMS_POOL, new() { "Key", "JumpBoots", "DisposableSyringe", "Bolts1", "Bolts2", "Bolts3", "Bolts4", "AncientClockGear", "AncientClockPendulum", "AncientClockHands", "AncientClockFace", "AttackCogBlueprint", "DefenseCogBlueprint", "ReloadCogBlueprint", "StaminaCogBlueprint", "SpeedCogBlueprint", "SyringeCogBlueprint", "ReviveCogBlueprint", "HealthChip", "StaminaChip", "StrengthChip", "DefenseChip", "InvincibilityChip", "SpinnerChip", "SteadyChip", "ShurikenChip", "SwordChip", "AxeChip", "RiskChip", "PowerChip", "VirusChip", "FatigueChip", "SpinChipA", "SpinChipB", "JumperChip", "RunnerChip", "SpeedChipA", "ReloadChip", "BulletChip", "DrifterChip", "SpeedChipB", "BoltChip", "WalletChip", "FasterHealChip", "VigorChip", "VampireChip", "ComboChipA", "ComboChipB", "SyringeChip", "AutoSyringeChip", "DoubleBarrelChip", "OffenseChip", "DogChip", "MerchantChip", "ScavengerChip", "AnimaChip", "ParryMasterChip", "CogChip", "BigHeartChip", "GlitchChip", "Blaster", "DoctorsGun", "Spinner", "Hookshot1", "AutomaticBlaster", "Shotgun", "Flamethrower", "Icethrower", "GranadeLauncher", "IceGranade", "GranadeShotgun", "IronEdge", "ThunderEdge", "Frostbite", "Flameblade", "ElementalBlade", "WarAxe", "IceAxe", "FireAxe", "ThunderAxe", "RaquelAxe", "IronStar", "IceStar", "FireStar", "ThunderStar", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "JumpBoots", "JumpBoots", "Hookshot1", "AttackCog", "DefenseCog", "ReloadCog", "StaminaCog", "SpeedCog", "SyringeCog", "ReviveCog", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust" } }
        };

        Instance = this;

        Logger.LogInfo($"Applying {typeof(Hooks)} ...");
        Harmony.CreateAndPatchAll(typeof(Hooks));
    }

    public ManualLogSource GetLogger() { return Logger; }

    public void SetOriginalChestList(Lists lists)
    {
        if (originalChestList == null)
        {
            originalChestList = lists.chestList;
            itemPools.Add(VANILLA_POOL, originalChestList.areas.SelectMany(areaChestList => areaChestList.chestList).Select(chest => chest.reward).ToList());
        }
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

    public ChestList CreateChestList(List<string> items)
    {
        ChestList chestList = Chests.CloneChestList(this.originalChestList);
        int i = 0;
        foreach (AreaChestList areaChestList in chestList.areas) foreach (ChestObject chestObject in areaChestList.chestList) chestObject.reward = items[i++];
        return chestList;
    }

    public void SetChestItems(List<string> items)
    {
        ChestList chestList = this.CreateChestList(items);
        PseudoSingleton<Lists>.instance.chestList = chestList;
    }

    public FileSettings CreateSettingsFromConfig()
    {
        FileData data = new(itemPools[options.chestItemPool.Value])
        {
            seed = options.seed.Value,
            removeFragileOnJumpBootsChest = options.removeFragileOnJumpBootsChest.Value,
        };

        return new(true, data)
        {
            randomSeed = options.randomSeed.Value,
            chestItemPool = options.chestItemPool.Value,
        };
    }

    public void SetDataFromSettings(FileSettings settings)
    {
        if (settings.useRandomeister)
        {
            if (settings.randomSeed) settings.data.seed = new System.Random().Next();

            if (settings.randomiseChests)
            {
                System.Random random = new(settings.data.seed);

                ChestList randomChestList = new Randomiser(random, this.originalChestList, GetItemPool(settings.chestItemPool)).Randomise();
                settings.data.items = randomChestList.areas.SelectMany(area => area.chestList).Select(chest => chest.reward).ToList();
            }
        }
    }

    public void CreateStoryFileRandomiser(FileDataIO fileDataIO, FileSettings settings)
    {
        SetDataFromSettings(settings);
        fileDataIO.Write(settings.data);
        LogChestRandomisation(settings.data.items);
        LoadStoryFileRandomiser(settings.data);
    }

    public void LoadStoryFileRandomiser(FileData data)
    {
        currentData = data;
        SetChestItems(data.items);
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
    }

    public void LogChestRandomisation(List<string> newItems)
    {
        if (originalChestList != null)
        {
            Logger.LogInfo("Writing log data");
            List<string> logLines = new();
            List<string> originalItems = itemPools[VANILLA_POOL];
            for (int i = 0; i < originalItems.Count; i++) logLines.Add(originalItems[i] + " is now " + newItems[i]);
            string logDir = "unsighted-randomeister/logs/randomisation/";
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            File.WriteAllLines(logDir + DateTime.Now.ToString("yy-MM-dd-HH-mm-ss-fff") + ".txt", logLines);
        }
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