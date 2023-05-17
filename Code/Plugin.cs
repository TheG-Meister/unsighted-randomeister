using BepInEx.Configuration;
using BepInEx.Logging;

namespace dev.gmeister.unsighted.randomeister;

[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string GUID = "dev.gmeister.unsighted.randomeister";
    public const string NAME = "Unsighted Randomeister";
    public const string VERSION = "0.2.0";

    public ConfigSlot? CurrentSlot;
    public List<ConfigSlot>? Slots;

    private ChestList? originalChestList;
    private ChestList? randomChestList;

    public static Plugin Instance { get; private set; } = null!;

    public Plugin()
    {
        if (Instance == null)
        {
            Instance = this;

            Logger.LogInfo($"Applying {typeof(HarmonyHooks)} ...");
            Harmony.CreateAndPatchAll(typeof(HarmonyHooks));

            Slots = new List<ConfigSlot>();

            for (int i = 0; i < 6; i++)
            {
                string slotName = "File " + (i + 1);
                string slotCategory = slotName + " Settings";

                ConfigEntry<bool> randomiseChests = Config.Bind(slotCategory, slotName + " - Randomise Chests", true, "Randomise chest items in Main Story " + slotName + ". Turning this setting off makes chests give their usual items (may require a restart)");
                ConfigEntry<bool> useRandomSeed = Config.Bind(slotCategory, slotName + " - Use random seed", true, "If \"Randomise Chests\" is enabled for this file, generate a random seed for randomisation when starting a new game. Turn this setting off to set the seed yourself.");
                ConfigEntry<int> seed = Config.Bind(slotCategory, slotName + " - Seed", 0, "If any randomisation options are enabled for this file, use this value to seed the randomisation. If \"Use Random Seed\" is enabled for this file, this number will be randomised when starting a new game.");

                ConfigSlot slot = new(randomiseChests, useRandomSeed, seed);

                randomiseChests.SettingChanged += (o, e) => { if (CurrentSlot != null && CurrentSlot.Equals(slot)) this.UnshuffleChests(); };
                seed.SettingChanged += (o, e) =>
                {
                    if (CurrentSlot != null && CurrentSlot.Equals(slot) && slot.RandomiseChests.Value) this.ShuffleChests(slot.Seed.Value);
                };

                Slots.Add(slot);
            }
        }
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

    private void ReplaceChestItems(ChestList chestList, List<string> items)
    {
        int i = 0;
        foreach (AreaChestList areaChestList in chestList.areas) foreach (ChestObject chestObject in areaChestList.chestList)
            {
                chestObject.reward = items[i++];
            }
    }

    public void UnshuffleChests()
    {
        Logger.LogInfo("Unshuffling chests");
        randomChestList = null;
        PseudoSingleton<Lists>.instance.chestList = originalChestList;
    }

    public void ShuffleChests(int seed)
    {
        if (originalChestList != null)
        {
            List<string> items = new() { "Key", "JumpBoots", "DisposableSyringe", "Bolts1", "Bolts2", "Bolts3", "Bolts4", "AncientClockGear", "AncientClockPendulum", "AncientClockHands", "AncientClockFace", "AttackCogBlueprint", "DefenseCogBlueprint", "ReloadCogBlueprint", "StaminaCogBlueprint", "SpeedCogBlueprint", "SyringeCogBlueprint", "ReviveCogBlueprint", "HealthChip", "StaminaChip", "StrengthChip", "DefenseChip", "InvincibilityChip", "SpinnerChip", "SteadyChip", "ShurikenChip", "SwordChip", "AxeChip", "RiskChip", "PowerChip", "VirusChip", "FatigueChip", "SpinChipA", "SpinChipB", "JumperChip", "RunnerChip", "SpeedChipA", "ReloadChip", "BulletChip", "DrifterChip", "SpeedChipB", "BoltChip", "WalletChip", "FasterHealChip", "VigorChip", "VampireChip", "ComboChipA", "ComboChipB", "SyringeChip", "AutoSyringeChip", "DoubleBarrelChip", "OffenseChip", "DogChip", "MerchantChip", "ScavengerChip", "AnimaChip", "ParryMasterChip", "CogChip", "BigHeartChip", "GlitchChip", "Blaster", "DoctorsGun", "Spinner", "Hookshot1", "AutomaticBlaster", "Shotgun", "Flamethrower", "Icethrower", "GranadeLauncher", "IceGranade", "GranadeShotgun", "IronEdge", "ThunderEdge", "Frostbite", "Flameblade", "ElementalBlade", "WarAxe", "IceAxe", "FireAxe", "ThunderAxe", "RaquelAxe", "IronStar", "IceStar", "FireStar", "ThunderStar", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "Key", "JumpBoots", "JumpBoots", "Hookshot1", "AttackCog", "DefenseCog", "ReloadCog", "StaminaCog", "SpeedCog", "SyringeCog", "ReviveCog", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust", "MeteorDust" };

            System.Random random = new(seed);
            List<string> shuffledItems = items.OrderBy(item => random.NextDouble()).ToList();

            randomChestList = CloneChestList(originalChestList);
            ReplaceChestItems(randomChestList, shuffledItems);

            Logger.LogInfo("Randomising chests");
            PseudoSingleton<Lists>.instance.chestList = randomChestList;
        }
    }

    public void LogChestRandomisation()
    {
        if (originalChestList != null && randomChestList != null)
        {
            Logger.LogInfo("Writing log data");
            List<string> logLines = new();
            List<string> originalItems = originalChestList.areas.SelectMany(areaChestList => areaChestList.chestList).Select(chest => chest.reward).ToList();
            List<string> shuffledItems = randomChestList.areas.SelectMany(areaChestList => areaChestList.chestList).Select(chest => chest.reward).ToList();
            for (int i = 0; i < originalItems.Count; i++) logLines.Add(originalItems[i] + " is now " + shuffledItems[i]);
            string logDir = "unsighted-randomeister/logs/randomisation/";
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            File.WriteAllLines(logDir + DateTime.Now.ToString("yy-MM-dd-HH-mm-ss-fff") + ".txt", logLines);
        }
    }

    public bool GameSlotIsStory(int gameSlot)
    {
        int page = (int)Math.Floor(gameSlot / 9d);
        int mode = (int)Math.Floor(gameSlot / 3d) % 3;
        return page >= 0 && page < 2 && mode == 0;
    }

    public void SetCurrentSlotAndRandomise(int gameSlot, bool newGame)
    {
        if (Slots != null)
        {
            if (GameSlotIsStory(gameSlot))
            {
                CurrentSlot = Slots[gameSlot % 9 + 3 * (int)Math.Floor((double)gameSlot / 9)];

                if (CurrentSlot.RandomiseChests.Value)
                {
                    if (newGame && CurrentSlot.UseRandomSeed.Value)
                    {
                        Logger.LogInfo("Generating new seed");
                        CurrentSlot.Seed.Value = new System.Random().Next();
                        //Randomisation is performed by event handler... though if you can find a way to change this please do
                    }
                    else ShuffleChests(CurrentSlot.Seed.Value);

                    if (newGame) LogChestRandomisation();
                }
                else UnshuffleChests();
            }
            else
            {
                CurrentSlot = null;
                UnshuffleChests();
            }
        }
    }
}