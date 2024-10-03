using RimWorld;
using Vehicles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace OutlanderVehicles;

[StaticConstructorOnStartup]
public class Comp_MechCommandRelay : ThingComp
{
    public Pawn tunedTo;

    public int tuningTimeLeft;

    public Pawn tuningTo;

    private Effecter effecter;

    private CompPowerTrader PowerTrader => parent.TryGetComp<CompPowerTrader>();

    public CompProperties_MechCommandRelay Props => (CompProperties_MechCommandRelay)props;

    private int RetuneTimeTicks => (int)(60000f * Props.retuneDays);

    private int TuningTimeTicks => (int)(Props.tuneSeconds * 60f);

    private BandNodeState State
    {
        get
        {
            if (tunedTo != null && tuningTo != null)
            {
                return BandNodeState.Retuning;
            }
            if (tuningTo != null)
            {
                return BandNodeState.Tuning;
            }
            if (tunedTo != null)
            {
                return BandNodeState.Tuned;
            }
            return BandNodeState.Untuned;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (!ModLister.CheckBiotech("Band node"))
        {
            parent.Destroy();
        }
        else
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }
    }

    public override void PostExposeData()
    {
        Scribe_References.Look(ref tunedTo, "tunedTo");
        Scribe_References.Look(ref tuningTo, "tuningTo");
        Scribe_Values.Look(ref tuningTimeLeft, "tuningTimeLeft", 0);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        Command_Action command_Action = new Command_Action();
        command_Action.defaultLabel = ((tunedTo == null) ? ("BandNodeTuneTo".Translate() + "...") : ("BandNodeRetuneTo".Translate() + "..."));
        command_Action.defaultDesc = ((tunedTo == null) ? "BandNodeTuningDesc".Translate("PeriodSeconds".Translate(Props.tuneSeconds)) : "BandNodeRetuningDesc".Translate(Props.retuneDays + " " + "Days".Translate()));
        command_Action.onHover = (Action)Delegate.Combine(command_Action.onHover, (Action)delegate
        {
            Pawn pawn2 = ((tuningTo != null) ? tuningTo : tunedTo);
            if (pawn2 != null)
            {
                GenDraw.DrawLineBetween(parent.DrawPos, pawn2.DrawPos);
            }
        });
        bool flag = false;
        foreach (Pawn item in parent.Map.mapPawns.AllPawnsSpawned)
        {
            if (MechanitorUtility.IsMechanitor(item) && tunedTo != item && tuningTo != item)
            {
                flag = true;
                break;
            }
        }
        command_Action.Disabled = !flag;
        command_Action.icon = (Texture)(object)ContentFinder<Texture2D>.Get("UI/Gizmos/BandNodeTuning");
        command_Action.action = (Action)Delegate.Combine(command_Action.action, (Action)delegate
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned)
            {
                if (MechanitorUtility.IsMechanitor(pawn))
                {
                    IEnumerable<Comp_MechCommandRelay> relays = from t in Find.Selector.SelectedObjects
                                                            where t is Thing thing && thing.TryGetComp<Comp_MechCommandRelay>() != null
                                                            select ((Thing)t).TryGetComp<Comp_MechCommandRelay>() into n
                                                            where n.tunedTo != pawn && n.tuningTo != pawn
                                                            select n;
                    if (relays.Any())
                    {
                        Pawn localPawn = pawn;
                        string text = pawn.Name.ToStringFull;
                        if (relays.All((Comp_MechCommandRelay b) => b.tunedTo == null) || relays.All((Comp_MechCommandRelay b) => b.tunedTo != null))
                        {
                            text = ((tunedTo != null) ? (text + " (" + RetuneTimeTicks.ToStringTicksToPeriod() + ")") : ((string)(text + (" (" + Props.tuneSeconds + " " + "SecondsLower".Translate() + ")"))));
                        }
                        list.Add(new FloatMenuOption(text, delegate
                        {
                            foreach (Comp_MechCommandRelay item2 in relays)
                            {
                                item2.TuneTo(localPawn);
                            }
                        }));
                    }
                }
            }
            Find.WindowStack.Add(new FloatMenu(list));
        });
        yield return command_Action;
        if (DebugSettings.ShowDevGizmos)
        {
            Command_Action command_Action2 = new Command_Action();
            command_Action2.defaultLabel = "DEV: complete tuning";
            command_Action2.action = delegate
            {
                tuningTimeLeft = 0;
            };
            yield return command_Action2;
        }
    }

    public void TuneTo(Pawn pawn)
    {
        tuningTimeLeft = ((tunedTo == null) ? TuningTimeTicks : RetuneTimeTicks);
        tuningTo = pawn;
    }

    public override void CompTick()
    {
        if (PowerTrader != null) { PowerTrader.PowerOutput = ((tunedTo == null && tuningTo == null) ? ((float)(-Props.powerConsumptionIdle)) : (0f - PowerTrader.Props.PowerConsumption)); }
        if (tunedTo != null && tunedTo.Dead) { tunedTo = null; }
        if (tuningTo != null && tuningTo.Dead) { tuningTo = null; }
        if (PowerTrader != null && !PowerTrader.PowerOn)
        {
            if (parent as VehiclePawn != null)
            {
                if (((VehiclePawn)parent).ignition.Drafted) { }
            }
            else
            {

                effecter?.Cleanup();
                effecter = null;
                return;
            }
        } 
        if (tuningTo != null)
        {
            tuningTimeLeft--;
            if (tuningTimeLeft <= 0)
            {
                tunedTo = tuningTo;
                tuningTo = null;
                if (Props.tuningCompleteSound != null)
                {
                    Props.tuningCompleteSound.PlayOneShot(parent);
                }
            }
        }
        if (tuningTo == null && tunedTo != null)
        {
            if (tunedTo.health.hediffSet.HasHediff<Hediff_MechCommandRelay>())
            {
                tunedTo.health.hediffSet.GetFirstHediff<Hediff_MechCommandRelay>().Register(this, Props.signalLockDuration);
            }
            else
            {
                tunedTo.health.AddHediff(DefDatabase<HediffDef>.GetNamed("OSVehicles_SignalRelay"), tunedTo.health.hediffSet.GetBrain());
            }
        }
        if (State == BandNodeState.Untuned)
        {
            if (effecter == null || effecter.def != Props.untunedEffect)
            {
                effecter?.Cleanup();
                effecter = Props.untunedEffect.SpawnAttached(parent, parent.Map);
                effecter.offset = Props.effecterOffset;
            }
        }
        else if (State == BandNodeState.Tuned)
        {
            if (effecter == null || effecter.def != Props.tunedEffect)
            {
                effecter?.Cleanup();
                effecter = Props.tunedEffect.SpawnAttached(parent, parent.Map);
                effecter.offset = Props.effecterOffset;
            }
        }
        else if (State == BandNodeState.Tuning)
        {
            if (effecter == null || effecter.def != Props.tuningEffect)
            {
                effecter?.Cleanup();
                effecter = Props.tuningEffect.SpawnAttached(parent, parent.Map);
                effecter.offset = Props.effecterOffset;
            }
        }
        else if (State == BandNodeState.Retuning)
        {
            if (effecter == null || effecter.def != Props.retuningEffect)
            {
                effecter?.Cleanup();
                effecter = Props.retuningEffect.SpawnAttached(parent, parent.Map);
                effecter.offset = Props.effecterOffset;
            }
        }
        else
        {
            effecter?.Cleanup();
            effecter = null;
        }
        if (effecter != null)
        {
            effecter.EffectTick(parent, parent);
        }
    }

    public override string CompInspectStringExtra()
    {
        string text = null;
        /*if (!PowerTrader.PowerOn)
        {
            text = "\n" + "Unpowered".Translate().CapitalizeFirst().Resolve();
        }*/
        if (tuningTo != null)
        {
            return "BandNodeTuningTo".Translate() + ": " + tuningTo.Name.ToStringFull + " - " + tuningTimeLeft.ToStringTicksToPeriod() + text;
        }
        return "BandNodeTunedTo".Translate() + ": " + ((tunedTo == null) ? "Nobody".Translate().Resolve() : tunedTo.Name.ToStringFull) + text;
    }

}
