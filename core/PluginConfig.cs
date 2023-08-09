using BepInEx.Configuration;
using static dev.gmeister.unsighted.randomeister.core.Constants;

namespace dev.gmeister.unsighted.randomeister.core;

public class PluginConfig
{

    public const string CATEGORY_DEVELOPER = "Developer";
    public ConfigEntry<bool> chestLogging;

    public const string CATEGORY_GENERAL = "General";
    public ConfigEntry<bool> useRandomeister;

    public const string CATEGORY_PATCHES = "Patches";
    public ConfigEntry<bool> removeFragileOnJumpBootsChest;
    public ConfigEntry<bool> newChestRadar;
    public ConfigEntry<bool> chestRadarMoreOften;
    public ConfigEntry<float> chestRadarScale;

    public const string CATEGORY_SEED = "Seed";
    public ConfigEntry<bool> randomSeed;
    public ConfigEntry<int> seed;

    public const string CATEGORY_RANDOMISATION = "Randomisation";
    public ConfigEntry<bool> randomiseChests;
    public ConfigEntry<string> chestItemPool;

    public const string CATEGORY_CUSTOMISER = "Customisation";

    public PluginConfig(ConfigFile configFile)
    {
        ConfigDescription chestLoggingDescription = new("Enables primitive chest logging for use in the randomiser logic", null, new ConfigurationManagerAttributes() { IsAdvanced = true });
        this.chestLogging = configFile.Bind(CATEGORY_DEVELOPER, "Chest logging", false, chestLoggingDescription);

        useRandomeister = configFile.Bind(CATEGORY_GENERAL, "Use randomeister", true, "Enable the use of the randomeister plugin. Turning this option off will disable all other randomisation options, and cause new story files to be completely unchanged. It does not disable other tweaks and hacks included in this plugin.");

        removeFragileOnJumpBootsChest = configFile.Bind(CATEGORY_PATCHES, "Fix Jump Boots chest", true, "Prevents the item in the jump boots chest from being removed from the player upon death or quit out. This can delete entire stacks of items but lets the item in the chest be collected over and over.");
        newChestRadar = configFile.Bind(CATEGORY_PATCHES, "New chest radar", true, "Every chest now has a radar displaying it's item, similar to the meteor dust radar.");
        chestRadarMoreOften = configFile.Bind(CATEGORY_PATCHES, "Chest radar more often", true, "Enables the chest radar during combat and cutscenes.");
        chestRadarScale = configFile.Bind(CATEGORY_PATCHES, "Chest radar scale", 1.4f, "Scales the items on the chest radar by the specified amount.");

        randomSeed = configFile.Bind(CATEGORY_SEED, "Use random seed", true, "Enabling this option will generate a random seed with which all other randomisation will be performed upon starting a new game. Turn this setting off to set the seed yourself.");
        seed = configFile.Bind(CATEGORY_SEED, "Seed", 0, "This value will be used to \"seed\" the randomisation of the next story file created. Two games created with the same settings, on the same randomiser version, that use the same seed will have all randomisation performed the same way. If \"Use random seed\" is enabled for this file, this number will itself be randomised when starting a new game.");

        randomiseChests = configFile.Bind(CATEGORY_RANDOMISATION, "Randomise chests", true, "If this option is enabled, the items found in chests throughout Arcadia will be randomised upon the creation of a new story file. Items will be placed in chests according to the specified seed, or a random seed if \"Use random seed\" is enabled. Turn this option off to have chests contain their original items.");

        AcceptableValueList<string> chestItemPoolValues = new(VANILLA_POOL, ALMOST_ALL_ITEMS_POOL);
        ConfigDescription chestItemPoolDescription = new("The name of the item pool to use for randomisation. \"Vanilla\" uses the items found in the unrandomised game, while \"Almost every item\" uses one of almost every item, including a few pairs of jump boots, a collection of keys and a lot of meteor dust.", chestItemPoolValues);
        chestItemPool = configFile.Bind(CATEGORY_RANDOMISATION, "Chest item pool", VANILLA_POOL, chestItemPoolDescription);
    }
}