namespace ItemRandomizer;

[BepInPlugin(GUID, NAME, VERSION)]
public class BepinexEntryPoint : BaseUnityPlugin
{
    public const string GUID = "dev.gmeister.unsighted.randomeister";
    public const string NAME = "Unsighted Randomeister";
    public const string VERSION = "0.0.1-23.04.20-1-SNAPSHOT";

    private void Awake()
    {
        Debug.Log($"Applying {typeof(HarmonyHooks)} ...");
        Harmony.CreateAndPatchAll(typeof(HarmonyHooks));
    }
}