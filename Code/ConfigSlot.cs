using BepInEx.Configuration;

public class ConfigSlot
{
    public ConfigEntry<bool>? RandomiseChests { get; set; }
    public ConfigEntry<bool>? UseRandomSeed { get; set; }
    public ConfigEntry<int>? Seed { get; set; }
}