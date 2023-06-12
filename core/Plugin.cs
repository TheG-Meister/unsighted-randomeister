using BepInEx;
using BepInEx.Logging;
using dev.gmeister.unsighted.randomeister.data;
using dev.gmeister.unsighted.randomeister.io;
using dev.gmeister.unsighted.randomeister.unsighted;
using HarmonyLib;
using UnityEngine;
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
                settings.data.items = GetRandomItems(GetItemPool(settings.chestItemPool), random);
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
        if (FileNumbers.GetMode(gameSlot) == STORY_MODE && FileNumbers.GetPage(gameSlot) >= 0 && FileNumbers.GetPage(gameSlot) < PAGES_PER_MODE)
        {
            int index = FileNumbers.GetIndex(gameSlot);
            FileDataIO fileDataIO = new(index);

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
        FileDataIO dataIO = new(button.saveSlot);
        if (dataIO.Exists()) dataIO.Delete();
    }

    public void OnFileCopied(SaveSlotPopup popup, int gameSlot)
    {
        if (FileNumbers.GetMode(gameSlot) == STORY_MODE)
        {
            int index = FileNumbers.GetIndex(gameSlot);
            FileDataIO fileDataIO = new(index);

            if (fileDataIO.Exists()) for (int newSlot = 0; newSlot < PAGES_PER_MODE * SLOTS_PER_PAGE; newSlot++)
                {
                    if (!popup.SaveExist(FileNumbers.ToGameSlot(index, STORY_MODE)))
                    {
                        fileDataIO.CopyTo(newSlot);
                        break;
                    }
                }
        }
    }

}