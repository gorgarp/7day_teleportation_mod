using System.Reflection;
using HarmonyLib;

public class TeleportersInit : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        Log.Out("[Teleporters] Loading mod...");
        var harmony = new HarmonyLib.Harmony("com.greg.teleporters");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        var _ = TeleporterManager.Instance;
        Log.Out("[Teleporters] Mod loaded successfully.");
    }
}
