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

    public static Plugin? Instance { get; private set; }

    private void Awake()
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

                ConfigSlot slot = new ConfigSlot(randomiseChests, useRandomSeed, seed);

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
        ChestObject chest = new ChestObject();
        chest.reward = other.reward;
        chest.chestName = other.chestName;
        chest.roomName = other.roomName;
        chest.abilitiesNeeded = (Abilities[])other.abilitiesNeeded;
        chest.dontCountToTotal = other.dontCountToTotal;

        return chest;
    }

    private AreaChestList CloneAreaChestList(AreaChestList other)
    {
        AreaChestList areaChestList = new AreaChestList(other.areaName);
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
        Logger.LogInfo("Unshuffling chests...");
        PseudoSingleton<Lists>.instance.chestList = originalChestList;
    }

    public void ShuffleChests(int seed)
    {
        if (originalChestList != null)
        {
            List<string> items = originalChestList.areas.SelectMany(areaChestList => areaChestList.chestList).Select(chest => chest.reward).ToList();

            System.Random random = new System.Random(seed);
            List<string> shuffledItems = items.OrderBy(item => random.NextDouble()).ToList();
            for (int i = 0; i < items.Count; i++) Logger.LogInfo(items[i] + " is now " + shuffledItems[i]);

            randomChestList = CloneChestList(originalChestList);
            ReplaceChestItems(randomChestList, shuffledItems);

            PseudoSingleton<Lists>.instance.chestList = randomChestList;
        }
    }

    public bool GameSlotIsStory(int gameSlot)
    {
        int slotValue = (int) Math.Floor((double)gameSlot / 3);
        return slotValue >= 0 && slotValue < 2 && slotValue % 3 == 0;
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
                    if (newGame && CurrentSlot.UseRandomSeed.Value) CurrentSlot.Seed.Value = new System.Random().Next();
                    ShuffleChests(CurrentSlot.Seed.Value);
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