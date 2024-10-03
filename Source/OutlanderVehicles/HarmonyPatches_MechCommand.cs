using HarmonyLib;
using Verse;

namespace OutlanderVehicles;

[StaticConstructorOnStartup]
public class HarmonyPatches_MechCommand
{
    static HarmonyPatches_MechCommand()
    {
        var harmony = new Harmony("rimworld.mjeiouws.OSVehicles.MechCommand");
        harmony.PatchAll();
    }
}
