using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles;
using Verse;

namespace OutlanderVehicles;
public class Hediff_MechCommandRelay : Hediff
{

    private HediffStage curStage;

    public override bool ShouldRemove => cachedTunedMechCommandRelays.EnumerableCount() == 0;
    private class MechCommand_SignalLock
    {
        public int secondsLeft;
        public Comp_MechCommandRelay commandRelay;
        public MechCommand_SignalLock(Comp_MechCommandRelay commandRelayComp, int lifeSeconds)
        {
            secondsLeft = lifeSeconds;
            commandRelay = commandRelayComp;
        }
        public MechCommand_SignalLock countDown()
        {
            secondsLeft--;
            return this;
        }
    }

    private IEnumerable<MechCommand_SignalLock> cachedTunedMechCommandRelays = new List<MechCommand_SignalLock>();

    public override HediffStage CurStage
    {
        get
        {
            if (curStage == null && cachedTunedMechCommandRelays.EnumerableCount() > 0)
            {
                curStage = new HediffStage();
            }
            return curStage;
        }
    }

    public override void PostTick()
    {
        base.PostTick();

        // concept: cached signals decay over time and are re-added on the Comp end, reducing the burden of checking each time for a signal
        if (pawn.IsHashIntervalTick(60))
        {
            cachedTunedMechCommandRelays = (from registered in cachedTunedMechCommandRelays where registered.secondsLeft > 0 select registered.countDown());
        }
    }

    public void Register(Comp_MechCommandRelay commandRelayComp, int lifeSeconds)
    {
        foreach(MechCommand_SignalLock registered in cachedTunedMechCommandRelays)
        {
            if(registered.commandRelay == commandRelayComp)
            {
                registered.secondsLeft = lifeSeconds;
                return;
            }
        }
        cachedTunedMechCommandRelays.AddItem(new MechCommand_SignalLock(commandRelayComp, lifeSeconds));
    }

    public List<Comp_MechCommandRelay> LinkedComps()
    {
        return (from registered in cachedTunedMechCommandRelays select registered.commandRelay).ToList();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref cachedTunedMechCommandRelays, "cachedTunedMechCommandRelays", new List<MechCommand_SignalLock>());
    }
}

