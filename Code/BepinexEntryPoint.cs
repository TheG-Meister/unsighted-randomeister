using BepInEx.Configuration;

namespace ItemRandomizer;

[BepInPlugin(GUID, NAME, VERSION)]
public class BepinexEntryPoint : BaseUnityPlugin
{
    public const string GUID = "dev.gmeister.unsighted.randomeister";
    public const string NAME = "Unsighted Randomeister";
    public const string VERSION = "0.2.0";

    public ConfigSlot currentSlot;
    public static List<ConfigSlot> slots = new List<ConfigSlot>();

    private void Awake()
    {
        Debug.Log($"Applying {typeof(HarmonyHooks)} ...");
        Harmony.CreateAndPatchAll(typeof(HarmonyHooks));

        for (int i = 0; i < 6; i++)
        {
            string slotName = "File " + (i + 1);
            string slotCategory = slotName + " Settings";

            ConfigSlot slot = new ConfigSlot();

            slot.RandomiseChests = Config.Bind(slotCategory, slotName + " - Randomise Chests", true, "Randomise chest items in Main Story " + slotName + ". Turning this setting off makes chests give their usual items (may require a restart)");
            slot.UseRandomSeed = Config.Bind(slotCategory, slotName + " - Use random seed", true, "If \"Randomise Chests\" is enabled for this file, generate a random seed for randomisation when starting a new game. Turn this setting off to set the seed yourself.");
            slot.Seed = Config.Bind(slotCategory, slotName + " - Seed", 0, "If any randomisation options are enabled for this file, use this value to seed the randomisation. If \"Use Random Seed\" is enabled for this file, this number will be randomised when starting a new game.")

            slots.Add(slot);
        }
    }
}