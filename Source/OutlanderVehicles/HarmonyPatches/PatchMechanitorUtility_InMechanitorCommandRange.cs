using HarmonyLib;
using System;
using RimWorld.Planet;
using Verse;

namespace OutlanderVehicles;

[HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.InMechanitorCommandRange), new Type[] { typeof(Pawn), typeof(LocalTargetInfo) })]
public class PatchMechanitorUtility_InMechanitorCommandRange
{
    static void Postfix(
        Pawn mech,
        LocalTargetInfo target,
        ref bool __result
        )
    {
        if (__result == true)
        {
            return;
        }

        Pawn overseer = mech.GetOverseer();
        if (overseer.IsCaravanMember())
        {
            Hediff_MechCommandRelay dataGet = overseer.health.hediffSet.GetFirstHediff<Hediff_MechCommandRelay>();
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
        else
        {
            __result = overseer.mechanitor.CanCommandTo(target);
        }

        return;
    }
}
