using BepInEx.Configuration;

namespace dev.gmeister.unsighted.randomeister;

public class ConfigSlot
{
    public ConfigEntry<bool> RandomiseChests { get; set; }
    public ConfigEntry<bool> UseRandomSeed { get; set; }
    public ConfigEntry<int> Seed { get; set; }

    public ConfigSlot(ConfigEntry<bool> randomiseChests, ConfigEntry<bool> useRandomSeed, ConfigEntry<int> seed)
    {
        RandomiseChests = randomiseChests;
        UseRandomSeed = useRandomSeed;
        Seed = seed;
    }
}