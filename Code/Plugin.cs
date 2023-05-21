﻿using BepInEx.Configuration;
using BepInEx.Logging;
using System;

namespace dev.gmeister.unsighted.randomeister;

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

    public ConfigSlot? CurrentSlot;
    public List<ConfigSlot>? Slots;
    public List<RandomisationData> data;

    private ChestList? originalChestList;
    private ChestList? randomChestList;

    public static Plugin Instance { get; private set; } = null!;

    public Plugin()
    {
        this.data = new(PAGES_PER_MODE * SLOTS_PER_PAGE);

        if (Instance == null)
        {
            Instance = this;

            Logger.LogInfo($"Applying {typeof(HarmonyHooks)} ...");
            Harmony.CreateAndPatchAll(typeof(HarmonyHooks));

            Slots = new List<ConfigSlot>();
            this.data = new();

            for (int i = 0; i < PAGES_PER_MODE * SLOTS_PER_PAGE; i++)
            {
                string slotName = "File " + (i + 1);
                string slotCategory = slotName + " Settings";

                ConfigEntry<bool> randomiseChests = Config.Bind(slotCategory, slotName + " - Randomise Chests", true, "Randomise chest items in Main Story " + slotName + ". Turning this setting off makes chests give their usual items (may require a restart)");
                ConfigEntry<bool> useRandomSeed = Config.Bind(slotCategory, slotName + " - Use random seed", true, "If \"Randomise Chests\" is enabled for this file, generate a random seed for randomisation when starting a new game. Turn this setting off to set the seed yourself.");
                ConfigEntry<int> seed = Config.Bind(slotCategory, slotName + " - Seed", 0, "If any randomisation options are enabled for this file, use this value to seed the randomisation. If \"Use Random Seed\" is enabled for this file, this number will be randomised when starting a new game.");

                ConfigSlot slot = new(randomiseChests, useRandomSeed, seed);

                Slots.Add(slot);
            }
        }
    }

    public string GetRandomisationDataPath(int storyFile)
    {
        return "unsighted-randomeister/saves/file-" + (storyFile + 1) + ".dat";
    }

    public bool HasRandomisationData(int storyFile)
    {
        return File.Exists(this.GetRandomisationDataPath(storyFile));
    }

    public RandomisationData ReadRandomisationData(int storyFile)
    {
        if (storyFile < 0 || storyFile >= PAGES_PER_MODE * SLOTS_PER_PAGE) throw new ArgumentException(storyFile + " is not a valid story file slot index");
        if (!this.HasRandomisationData(storyFile)) throw new ArgumentException("There is no randomisation data for story file " + storyFile);
        return Serializer.Load<RandomisationData>(this.GetRandomisationDataPath(storyFile));
    }

    public void CopyRandomisationData(int fromStoryFile, int toStoryFile)
    {
        if (fromStoryFile < 0 || fromStoryFile >= PAGES_PER_MODE * SLOTS_PER_PAGE) throw new ArgumentException(fromStoryFile + " is not a valid story file slot index");
        if (!this.HasRandomisationData(fromStoryFile)) throw new ArgumentException("There is no randomisation data for story file " + fromStoryFile);
        if (toStoryFile < 0 || toStoryFile >= PAGES_PER_MODE * SLOTS_PER_PAGE) throw new ArgumentException(toStoryFile + " is not a valid story file slot index");

        File.Copy(this.GetRandomisationDataPath(fromStoryFile), this.GetRandomisationDataPath(toStoryFile), true);
    }

    public void WriteRandomisationData(int storyFile, RandomisationData data)
    {
        string path = this.GetRandomisationDataPath(storyFile);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Serializer.Save<RandomisationData>(path, data);
    }

    public void DeleteRandomisationData(int storyFile)
    {
        File.Delete(this.GetRandomisationDataPath(storyFile));
    }

    public ManualLogSource GetLogger() { return Logger; }

    public void SetOriginalChestList(Lists lists)
    {
        if (this.originalChestList == null) this.originalChestList = lists.chestList;
    }

    private ChestObject CloneChest(ChestObject other)
    {
        ChestObject chest = new()
        {
            reward = other.reward,
            chestName = other.chestName,
            roomName = other.roomName,
            abilitiesNeeded = (Abilities[])other.abilitiesNeeded,
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
        ChestList chestList = this.CloneChestList(this.originalChestList);
        int i = 0;
        foreach (AreaChestList areaChestList in chestList.areas) foreach (ChestObject chestObject in areaChestList.chestList) chestObject.reward = items[i++];
        return chestList;
    }

    public void UnshuffleChests()
    {
        Logger.LogInfo("Unshuffling chests");
        randomChestList = null;
        PseudoSingleton<Lists>.instance.chestList = originalChestList;
    }

    public List<string> GetRandomItems(System.Random random)
    {
        List<string> items = new() { "Key", "JumpBoots", "DisposableSyringe", "Bolts1", "Bolts2", "Bolts3", "Bolts4", "AncientClockGear", "AncientClockPendulum", "AncientClockHands", "AncientClockFace", "AttackCogBlueprint", "DefenseCogBlueprint", "ReloadCogBlueprint", "StaminaCogBlueprint", "SpeedCogBlueprint", "SyringeCogBlueprint", "ReviveCogBlueprint", "HealthChip", "StaminaChip", "StrengthChip", "DefenseChip", "InvincibilityChip", "SpinnerChip", "SteadyChip", "ShurikenChip", "SwordChip", "AxeChip", "RiskChip", "PowerChip", "VirusChip", "FatigueChip", "SpinChipA", "SpinChipB", "JumperChip", "RunnerChip", "SpeedChipA", "ReloadChip", "BulletChip", "DrifterChip", "SpeedChipB", "BoltChip", "WalletChip", "FasterHealChip", "VigorChip", "VampireChip", "ComboChipA", "ComboChipB", "SyringeChip", "AutoSyringeChip", "DoubleBarrelChip", "OffenseChip", "DogChip", "MerchantChip", "ScavengerChip", "AnimaChip", "ParryMasterChip", "CogChip", "BigHeartChip", "GlitchChip", "Blaster", "DoctorsGun", "Spinner", "Hookshot1", "AutomaticBlaster", "Shotgun", "Flamethrower", "Icethrower", "GranadeLauncher", "IceGranade", "GranadeShotgun", "IronEdge", "ThunderEdge", "Frostbite", "Flameblade", "ElementalBlade", "WarAxe", "IceAxe", "FireAxe", "ThunderAxe", "RaquelAxe", "IronStar", "IceStar", "FireStar", "ThunderStar", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "JumpBoots", "JumpBoots", "Hookshot1", "AttackCog", "DefenseCog", "ReloadCog", "StaminaCog", "SpeedCog", "SyringeCog", "ReviveCog", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust" };
        return items.OrderBy(item => random.NextDouble()).ToList();
    }

    public void SetChestItems(List<string> items)
    {
        ChestList chestList = this.CreateChestList(items);
        PseudoSingleton<Lists>.instance.chestList = chestList;
    }

    public void SetDataFromSettings(RandomisationSettings settings)
    {
        if (settings.useRandomeister)
        {
            if (settings.randomSeed) settings.seed = new System.Random().Next();

            if (settings.randomiseChests)
            {
                System.Random random = new System.Random(settings.seed);
                settings.items = this.GetRandomItems(random);
            }
        }
    }

    public void CreateStoryFileRandomiser(int storySlot, RandomisationSettings settings)
    {
        this.SetDataFromSettings(settings);
        this.WriteRandomisationData(storySlot, settings);
        this.LogChestRandomisation(settings.items);
        this.LoadStoryFileRandomiser(settings);
    }

    public void LoadStoryFileRandomiser(RandomisationData data)
    {
        this.SetChestItems(data.items);
    }

    public void LogChestRandomisation(List<string> newItems)
    {
        if (originalChestList != null)
        {
            Logger.LogInfo("Writing log data");
            List<string> logLines = new();
            List<string> originalItems = originalChestList.areas.SelectMany(areaChestList => areaChestList.chestList).Select(chest => chest.reward).ToList();
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
        return (int) Math.Floor((double) gameSlot / SLOTS_PER_PAGE) % SLOTS_PER_PAGE;
    }

    public int GetSlotPage(int gameSlot)
    {
        return (int) Math.Floor((double) gameSlot / (MODES * SLOTS_PER_PAGE));
    }

    public int GameSlotToStorySlot(int gameSlot)
    {
        int page = this.GetSlotPage(gameSlot);
        if (page >= PAGES_PER_MODE || page < 0) throw new ArgumentException("file index " + gameSlot + " is not within the page limits for saved games");
        if (this.GetSlotMode(gameSlot) != STORY_MODE) throw new ArgumentException("file index " + gameSlot + " is not a story game slot");
        return (gameSlot % SLOTS_PER_PAGE) + (SLOTS_PER_PAGE * page);
    }

    public int StorySlotToGameSlot(int storySlot)
    {
        return (storySlot % SLOTS_PER_PAGE) + (SLOTS_PER_PAGE * MODES) * (int) Math.Floor((double) storySlot / SLOTS_PER_PAGE);
    }

    public void SetCurrentSlotAndRandomise(int gameSlot, bool newGame)
    {
        if (this.GetSlotMode(gameSlot) == 0 && this.GetSlotPage(gameSlot) >= 0 && this.GetSlotPage(gameSlot) < PAGES_PER_MODE)
        {
            int storySlot = this.GameSlotToStorySlot(gameSlot);

            CurrentSlot = Slots[storySlot];

            if (newGame)
            {
                if (CurrentSlot.RandomiseChests.Value)
                {
                    RandomisationSettings settings = new RandomisationSettings(true, originalChestList.areas.SelectMany(areaChestList => areaChestList.chestList).Select(chest => chest.reward).ToList());
                    settings.randomSeed = CurrentSlot.UseRandomSeed.Value;
                    settings.seed = CurrentSlot.Seed.Value;

                    this.CreateStoryFileRandomiser(storySlot, settings);
                }
                else
                {
                    this.UnshuffleChests();
                    this.DeleteRandomisationData(storySlot);
                }
            }
            else
            {
                if (this.HasRandomisationData(storySlot)) this.LoadStoryFileRandomiser(this.ReadRandomisationData(storySlot));
                else this.UnshuffleChests();
            }
        }
        else
        {
            CurrentSlot = null;
            this.UnshuffleChests();
        }
    }

    public void OnFileErased(SaveSlotButton button)
    {
        this.DeleteRandomisationData(button.saveSlot);
    }

    public void OnFileCopied(SaveSlotPopup popup, int gameSlot)
    {
        int storySlot = this.GameSlotToStorySlot(gameSlot);

        if (this.HasRandomisationData(storySlot)) for (int newSlot = 0; newSlot < PAGES_PER_MODE * SLOTS_PER_PAGE; newSlot++)
        {
            if (!popup.SaveExist(this.StorySlotToGameSlot(newSlot)))
            {
                this.CopyRandomisationData(storySlot, newSlot);
                break;
            }
        }
    }

}