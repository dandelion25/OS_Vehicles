using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace OutlanderVehicles;

[HarmonyPatch(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.CanCommandTo), new Type[] { typeof(LocalTargetInfo) })]
public class PatchMechanitorTracker_CanCommandTo
{
    static void Postfix(
        Pawn_MechanitorTracker __instance,
        LocalTargetInfo target,
        ref bool __result
        )
    {
        // Don't check relays if mechanitor is in range
        if (__result == true)
        {
            return;
        }

        // If there are relays tuned to the mechanitor, check whether they are in range
        Hediff_MechCommandRelay dataGet = __instance.Pawn.health.hediffSet.GetFirstHediff<Hediff_MechCommandRelay>();
        if (dataGet != null)
        {
            foreach (ThingComp relay in dataGet.LinkedComps())
            {
                if (target.Cell.InBounds(relay.parent.MapHeld) && relay.parent.Position.DistanceToSquared(target.Cell) < Math.Pow(((Comp_MechCommandRelay)relay).Props.relayCommandRange, 2))
                {
                    __result = true;
                    return;
                }
            }
        }
    }
}
