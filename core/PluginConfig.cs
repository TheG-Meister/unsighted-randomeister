using BepInEx.Configuration;
using static dev.gmeister.unsighted.randomeister.core.Constants;

namespace dev.gmeister.unsighted.randomeister.core;

public class PluginConfig
{

    public const string CATEGORY_DEVELOPER = "Developer";
    public ConfigEntry<bool> chestLogging;
    public ConfigEntry<bool> movementLogging;
    public ConfigEntry<bool> movementLoggingAnnouncements;

    public const string CATEGORY_GENERAL = "General";
    public ConfigEntry<bool> useRandomeister;

    public const string CATEGORY_PATCHES = "Patches";
    public ConfigEntry<bool> removeFragileOnJumpBootsChest;

    public const string CATEGORY_CHEST_RADAR = "Chest Radar";
    public ConfigEntry<bool> newChestRadar;
    public ConfigEntry<bool> chestRadarMoreOften;
    public ConfigEntry<float> chestRadarScale;
    public ConfigEntry<float> chestRadarCameraPadding;
    public ConfigEntry<bool> chestRadarSnapping;
    public ConfigEntry<bool> chestRadarCircular;
    public ConfigEntry<float> chestRadarRadius;

    public const string CATEGORY_SEED = "Seed";
    public ConfigEntry<bool> randomSeed;
    public ConfigEntry<int> seed;

    public const string CATEGORY_RANDOMISATION = "Randomisation";
    public ConfigEntry<bool> randomiseChests;
    public ConfigEntry<string> chestItemPool;
    public ConfigEntry<bool> randomiseEnemyDrops;

    public const string CATEGORY_CUSTOMISER = "Customisation";

    public PluginConfig(ConfigFile configFile)
    {
        this.chestLogging = configFile.Bind(CATEGORY_DEVELOPER, "Chest logging", false, new ConfigDescription("Enables primitive chest logging for use in the randomiser logic", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));

        this.movementLogging = configFile.Bind(CATEGORY_DEVELOPER, "Movement logging", false, "Saves player movement information to the system. Send the files to @the_g_meister on discord to help improve the randomiser!");
        this.movementLogging.SettingChanged += (o, e) =>
        {
            Plugin.Instance.movementLogger.log = this.movementLogging.Value;
            if (!this.movementLogging.Value) Plugin.Instance.movementLogger.ClearLocation();
        };
        this.movementLoggingAnnouncements = configFile.Bind(CATEGORY_DEVELOPER, "Movement logging announcements", true, "If movement logging is enabled, announces various various actions. Try to minimise white announcements, which report actions. Green announcements report new locations, yellow announcements report resets, and blue announcements report other useful information.");
        this.movementLoggingAnnouncements.SettingChanged += (o, e) => { Plugin.Instance.movementLogger.announce = this.movementLoggingAnnouncements.Value; };

        useRandomeister = configFile.Bind(CATEGORY_GENERAL, "Use randomeister", true, "Enable the use of the randomeister plugin. Turning this option off will disable all other randomisation options, and cause new story files to be completely unchanged. It does not disable other tweaks and hacks included in this plugin.");

        removeFragileOnJumpBootsChest = configFile.Bind(CATEGORY_PATCHES, "Fix Jump Boots chest", true, "Prevents the item in the jump boots chest from being removed from the player upon death or quit out. This can delete entire stacks of items but lets the item in the chest be collected over and over.");

        newChestRadar = configFile.Bind(CATEGORY_CHEST_RADAR, "Enable chest radar", true, "Every chest now has a radar displaying it's item, similar to the meteor dust radar.");
        chestRadarMoreOften = configFile.Bind(CATEGORY_CHEST_RADAR, "Radar in combat & cutscenes", true, "Enables the chest radar during combat and cutscenes.");
        chestRadarScale = configFile.Bind(CATEGORY_CHEST_RADAR, "Radar icon scale", 1.4f, new ConfigDescription("Scale the radar's item icons", new AcceptableValueRange<float>(0f, 5f)));
        chestRadarCameraPadding = configFile.Bind(CATEGORY_CHEST_RADAR, "Radar padding", 1.5f, new ConfigDescription("Pull radar pings away from the edge of the screen", new AcceptableValueRange<float>(0f, 3f)));
        chestRadarSnapping = configFile.Bind(CATEGORY_CHEST_RADAR, "Radar snapping", true, "Snaps radar pings to chests when they are on screen");
        chestRadarCircular = configFile.Bind(CATEGORY_CHEST_RADAR, "Circular radar", false, "Limits radar pings to a circle around Player 1");
        chestRadarRadius = configFile.Bind(CATEGORY_CHEST_RADAR, "Circular radar radius", 5f, new ConfigDescription("Scale the circular radar size", new AcceptableValueRange<float>(1f, 20f)));

        randomSeed = configFile.Bind(CATEGORY_SEED, "Use random seed", true, "Enabling this option will generate a random seed with which all other randomisation will be performed upon starting a new game. Turn this setting off to set the seed yourself.");
        seed = configFile.Bind(CATEGORY_SEED, "Seed", 0, "This value will be used to \"seed\" the randomisation of the next story file created. Two games created with the same settings, on the same randomiser version, that use the same seed will have all randomisation performed the same way. If \"Use random seed\" is enabled for this file, this number will itself be randomised when starting a new game.");

        randomiseChests = configFile.Bind(CATEGORY_RANDOMISATION, "Randomise chests", true, "If this option is enabled, the items found in chests throughout Arcadia will be randomised upon the creation of a new story file. Items will be placed in chests according to the specified seed, or a random seed if \"Use random seed\" is enabled. Turn this option off to have chests contain their original items.");
        chestItemPool = configFile.Bind(CATEGORY_RANDOMISATION, "Chest item pool", VANILLA_POOL, new ConfigDescription("The name of the item pool to use for randomisation. \"Vanilla\" uses the items found in the unrandomised game, while \"Almost every item\" uses one of almost every item, including a few pairs of jump boots, a collection of keys and a lot of meteor dust.", new AcceptableValueList<string>(VANILLA_POOL, ALMOST_ALL_ITEMS_POOL)));
        this.randomiseEnemyDrops = configFile.Bind(CATEGORY_RANDOMISATION, "Randomise enemy drops", true, "Make each enemy time drop a single, random crafting material after a fixed number of kills");
    }
}