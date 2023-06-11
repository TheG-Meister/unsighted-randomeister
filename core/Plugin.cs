global using BepInEx;
global using HarmonyLib;
global using UnityEngine;

using BepInEx.Logging;
using dev.gmeister.unsighted.randomeister.data;

namespace dev.gmeister.unsighted.randomeister.core;

[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string GUID = "dev.gmeister.unsighted.randomeister";
    public const string NAME = "Unsighted Randomeister";
    public const string VERSION = "0.2.0";

    public const int MODES = 3;
    public const int PAGES_PER_MODE = 2;
    public const int SLOTS_PER_PAGE = 3;

    public const int STORY_MODE = 0;

    public PluginConfig options;

    public RandomisationData? currentData;

    private ChestList? originalChestList;

    public static Plugin Instance { get; private set; } = null!;

    public Dictionary<string, List<string>> itemPools;
    public const string VANILLA_POOL = "Vanilla";
    public const string ALMOST_ALL_ITEMS_POOL = "Almost every item";

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

    public string GetRandomisationDataPath(int storyFile)
    {
        return "unsighted-randomeister/saves/file-" + (storyFile + 1) + ".dat";
    }

    public bool HasRandomisationData(int storyFile)
    {
        return File.Exists(GetRandomisationDataPath(storyFile));
    }

    public RandomisationData ReadRandomisationData(int storyFile)
    {
        if (storyFile < 0 || storyFile >= PAGES_PER_MODE * SLOTS_PER_PAGE) throw new ArgumentException(storyFile + " is not a valid story file slot index");
        if (!HasRandomisationData(storyFile)) throw new ArgumentException("There is no randomisation data for story file " + storyFile);
        return Serializer.Load<RandomisationData>(GetRandomisationDataPath(storyFile));
    }

    public void CopyRandomisationData(int fromStoryFile, int toStoryFile)
    {
        if (fromStoryFile < 0 || fromStoryFile >= PAGES_PER_MODE * SLOTS_PER_PAGE) throw new ArgumentException(fromStoryFile + " is not a valid story file slot index");
        if (!HasRandomisationData(fromStoryFile)) throw new ArgumentException("There is no randomisation data for story file " + fromStoryFile);
        if (toStoryFile < 0 || toStoryFile >= PAGES_PER_MODE * SLOTS_PER_PAGE) throw new ArgumentException(toStoryFile + " is not a valid story file slot index");

        File.Copy(GetRandomisationDataPath(fromStoryFile), GetRandomisationDataPath(toStoryFile), true);
    }

    public void WriteRandomisationData(int storyFile, RandomisationData data)
    {
        string path = GetRandomisationDataPath(storyFile);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Serializer.Save(path, data);
    }

    public void DeleteRandomisationData(int storyFile)
    {
        File.Delete(GetRandomisationDataPath(storyFile));
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

    private ChestObject CloneChest(ChestObject other)
    {
        ChestObject chest = new()
        {
            reward = other.reward,
            chestName = other.chestName,
            roomName = other.roomName,
            abilitiesNeeded = other.abilitiesNeeded,
            dontCountToTotal = other.dontCountToTotal
        };

        return chest;
    }

    private AreaChestList CloneAreaChestList(AreaChestList other)
    {
        AreaChestList areaChestList = new(other.areaName);
        foreach (ChestObject chest in other.chestList) areaChestList.chestList.Add(CloneChest(chest));

        return areaChestList;
    }

    private ChestList CloneChestList(ChestList other)
    {
        ChestList chestList = ScriptableObject.CreateInstance<ChestList>();
        chestList.areas = new List<AreaChestList>();
        chestList.chestList = new List<ChestObject>();

        foreach (AreaChestList areaChestList in other.areas)
        {
            AreaChestList newAreaChestList = CloneAreaChestList(areaChestList);
            chestList.areas.Add(newAreaChestList);
            chestList.chestList.AddRange(newAreaChestList.chestList);
        }

        return chestList;
    }

    public ChestList CreateChestList(List<string> items)
    {
        ChestList chestList = CloneChestList(originalChestList);
        int i = 0;
        foreach (AreaChestList areaChestList in chestList.areas) foreach (ChestObject chestObject in areaChestList.chestList) chestObject.reward = items[i++];
        return chestList;
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

    public void SetChestItems(List<string> items)
    {
        ChestList chestList = CreateChestList(items);
        PseudoSingleton<Lists>.instance.chestList = chestList;
    }

    public RandomisationSettings CreateSettingsFromConfig()
    {
        RandomisationData data = new(itemPools[options.chestItemPool.Value])
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

    public void SetDataFromSettings(RandomisationSettings settings)
    {
        if (settings.useRandomeister)
        {
            if (settings.randomSeed) settings.data.seed = new System.Random().Next();

            if (settings.randomiseChests)
            {
                System.Random random = new System.Random(settings.data.seed);
                settings.data.items = GetRandomItems(GetItemPool(settings.chestItemPool), random);
            }
        }
    }

    public void CreateStoryFileRandomiser(int storySlot, RandomisationSettings settings)
    {
        SetDataFromSettings(settings);
        WriteRandomisationData(storySlot, settings.data);
        LogChestRandomisation(settings.data.items);
        LoadStoryFileRandomiser(settings.data);
    }

    public void LoadStoryFileRandomiser(RandomisationData data)
    {
        currentData = data;
        SetChestItems(data.items);
    }

    public void CreateVanillaStoryFile(int storySlot)
    {
        DeleteRandomisationData(storySlot);
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

    public int GetSlotIndex(int gameSlot)
    {
        return gameSlot & SLOTS_PER_PAGE;
    }

    public int GetSlotMode(int gameSlot)
    {
        return (int)Math.Floor((double)gameSlot / SLOTS_PER_PAGE) % SLOTS_PER_PAGE;
    }

    public int GetSlotPage(int gameSlot)
    {
        return (int)Math.Floor((double)gameSlot / (MODES * SLOTS_PER_PAGE));
    }

    public int GameSlotToStorySlot(int gameSlot)
    {
        int page = GetSlotPage(gameSlot);
        if (page >= PAGES_PER_MODE || page < 0) throw new ArgumentException("file index " + gameSlot + " is not within the page limits for saved games");
        if (GetSlotMode(gameSlot) != STORY_MODE) throw new ArgumentException("file index " + gameSlot + " is not a story game slot");
        return gameSlot % SLOTS_PER_PAGE + SLOTS_PER_PAGE * page;
    }

    public int StorySlotToGameSlot(int storySlot)
    {
        return storySlot % SLOTS_PER_PAGE + SLOTS_PER_PAGE * MODES * (int)Math.Floor((double)storySlot / SLOTS_PER_PAGE);
    }

    public void SetCurrentSlotAndRandomise(int gameSlot, bool newGame)
    {
        if (GetSlotMode(gameSlot) == 0 && GetSlotPage(gameSlot) >= 0 && GetSlotPage(gameSlot) < PAGES_PER_MODE)
        {
            int storySlot = GameSlotToStorySlot(gameSlot);

            if (newGame)
            {
                if (options.useRandomeister.Value) CreateStoryFileRandomiser(storySlot, CreateSettingsFromConfig());
                else CreateVanillaStoryFile(storySlot);
            }
            else
            {
                if (HasRandomisationData(storySlot)) LoadStoryFileRandomiser(ReadRandomisationData(storySlot));
                else LoadVanillaStoryFile();
            }
        }
        else LoadVanillaStoryFile();
    }

    public void OnFileErased(SaveSlotButton button)
    {
        DeleteRandomisationData(button.saveSlot);
    }

    public void OnFileCopied(SaveSlotPopup popup, int gameSlot)
    {
        int storySlot = GameSlotToStorySlot(gameSlot);

        if (HasRandomisationData(storySlot)) for (int newSlot = 0; newSlot < PAGES_PER_MODE * SLOTS_PER_PAGE; newSlot++)
            {
                if (!popup.SaveExist(StorySlotToGameSlot(newSlot)))
                {
                    CopyRandomisationData(storySlot, newSlot);
                    break;
                }
            }
    }

}