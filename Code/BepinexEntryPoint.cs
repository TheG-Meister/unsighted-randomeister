namespace ItemRandomizer;

[BepInPlugin(GUID, NAME, VERSION)]
public class BepinexEntryPoint : BaseUnityPlugin
{
    public const string GUID = "dev.gmeister.unsighted.randomeister";
    public const string NAME = "Unsighted Randomeister";
    public const string VERSION = "0.2.0";

    private void Awake()
    {
        Debug.Log($"Applying {typeof(HarmonyHooks)} ...");
        Harmony.CreateAndPatchAll(typeof(HarmonyHooks));
    }
}