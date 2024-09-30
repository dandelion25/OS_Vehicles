using System;
using System.Collections.Generic;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace OutlanderVehicles;

public class VehiclePawn_VehicleHolder : VehiclePawn
{
    public bool HardpointOccupied
    {
        get
        {
            //IL_001c: Unknown result type (might be due to invalid IL or missing references)
            foreach (VehicleHandler handler in base.handlers)
            {
                if ((int)handler.role.HandlingTypes == 0 && handler.handlers.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo item in <> n__0()
        {
            yield return item;
        }
        bool upgrading = ((VehiclePawn)this).CompUpgradeTree != null && ((VehiclePawn)this).CompUpgradeTree.Upgrading;
        Command_Action command_ActionMain = new Command_Action
        {
            defaultLabel = "KurinLoadErikaMain".Translate(),
            defaultDesc = "KurinLoadErikaMainDesc".Translate(),
            icon = (Texture)(object)ContentFinder<Texture2D>.Get("Kurin/UI/LoadErikaMain/Texture"),
            action = delegate
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                HaulTargeter.BeginTargeting(new TargetingParameters
                {
                    canTargetBuildings = false,
                    neverTargetHostileFaction = true,
                    canTargetItems = false,
                    validator = delegate (TargetInfo target)
                    {
                        if (!target.HasThing)
                        {
                            return false;
                        }
                        Thing thing4 = target.Thing;
                        VehiclePawn val5 = (VehiclePawn)(object)((thing4 is VehiclePawn) ? thing4 : null);
                        return val5 != null && ((Thing)(object)val5).def.size.Area > 1 && ((Thing)(object)val5).def.size.Area <= 21 && ((((Thing)(object)val5).Faction == Faction.OfPlayer) ? true : false);
                    }
                }, (Action<LocalTargetInfo>)delegate (LocalTargetInfo target)
                {
                    Thing thing3 = target.Thing;
                    VehiclePawn val4 = (VehiclePawn)(object)((thing3 is VehiclePawn) ? thing3 : null);
                    if (val4 != null && !(target.Thing is VehiclePawn_VehicleHolder))
                    {
                        VehicleHandler handler2 = base.handlers.FirstOrDefault((VehicleHandler handler) => handler.get_AreSlotsAvailable() && handler.role.key == "mainHardpoint");
                        PromptToLoadErika((Pawn)(object)val4, handler2);
                    }
                    else
                    {
                        TransferableOneWay transferableOneWay2 = new TransferableOneWay
                        {
                            things = new List<Thing> { target.Thing }
                        };
                        transferableOneWay2.AdjustTo(1);
                        base.cargoToLoad.Add(transferableOneWay2);
                        MapComponentCache.GetCachedMapComponent<VehicleReservationManager>(((Thing)this).Map).RegisterLister((VehiclePawn)(object)this, "LoadVehicle");
                    }
                }, (VehiclePawn)(object)this, (Action)null, (Texture2D)null);
            }
        };
        yield return command_ActionMain;
        Command_Action command_ActionSecond = new Command_Action
        {
            defaultLabel = "KurinLoadErikaSecondary".Translate(),
            defaultDesc = "KurinLoadErikaSecondaryDesc".Translate(),
            icon = (Texture)(object)ContentFinder<Texture2D>.Get("Kurin/UI/LoadErikaSecondary/Texture"),
            action = delegate
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                HaulTargeter.BeginTargeting(new TargetingParameters
                {
                    canTargetBuildings = false,
                    neverTargetHostileFaction = true,
                    canTargetItems = false,
                    validator = delegate (TargetInfo target)
                    {
                        if (!target.HasThing)
                        {
                            return false;
                        }
                        Thing thing2 = target.Thing;
                        VehiclePawn val3 = (VehiclePawn)(object)((thing2 is VehiclePawn) ? thing2 : null);
                        return val3 != null && ((Thing)(object)val3).def.size.Area == 1 && ((((Thing)(object)val3).Faction == Faction.OfPlayer) ? true : false);
                    }
                }, (Action<LocalTargetInfo>)delegate (LocalTargetInfo target)
                {
                    Thing thing = target.Thing;
                    VehiclePawn val = (VehiclePawn)(object)((thing is VehiclePawn) ? thing : null);
                    if (val != null && !(target.Thing is VehiclePawn_VehicleHolder))
                    {
                        VehicleHandler val2 = base.handlers.FirstOrDefault((VehicleHandler handler) => handler.AreSlotsAvailable && (int)handler.role.HandlingTypes == 0 && handler.role.key != "mainHardpoint");
                        ((VehiclePawn)this).PromptToBoardVehicle((Pawn)(object)val, val2);
                    }
                    else
                    {
                        TransferableOneWay transferableOneWay = new TransferableOneWay
                        {
                            things = new List<Thing> { target.Thing }
                        };
                        transferableOneWay.AdjustTo(1);
                        base.cargoToLoad.Add(transferableOneWay);
                        MapComponentCache.GetCachedMapComponent<VehicleReservationManager>(((Thing)this).Map).RegisterLister((VehiclePawn)(object)this, "LoadVehicle");
                    }
                }, (VehiclePawn)(object)this, (Action)null, (Texture2D)null);
            }
        };
        yield return command_ActionSecond;
        if (upgrading)
        {
            command_ActionMain.Disable("VF_DisabledByVehicleUpgrading".Translate(((Entity)(object)this).LabelCap));
            command_ActionSecond.Disable("VF_DisabledByVehicleUpgrading".Translate(((Entity)(object)this).LabelCap));
        }
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        foreach (FloatMenuOption item in <> n__1(selPawn))
        {
            if (item.Label != "VF_EnterVehicle".Translate())
            {
                yield return item;
                yield break;
            }
        }
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        ((Pawn)this).inventory.DropAllNearPawn(((Thing)this).Position);
        ((VehiclePawn)this).Destroy(mode);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        //IL_0007: Unknown result type (might be due to invalid IL or missing references)
        DrawVehicles();
        ((VehiclePawn)this).DrawAt(drawLoc, flip);
    }

    public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
    {
        //IL_0012: Unknown result type (might be due to invalid IL or missing references)
        if (phase == DrawPhase.Draw)
        {
            ((VehiclePawn)this).Draw();
            DrawVehicles();
        }
        ((VehiclePawn)this).DynamicDrawPhaseAt(phase, drawLoc, flip);
    }

    public void DrawVehicles()
    {
        if (!HardpointOccupied)
        {
            return;
        }
        foreach (VehicleHandler handler in base.handlers)
        {
            RenderVehicles(handler, (VehiclePawn)(object)this);
        }
    }

    public static void RenderVehicles(VehicleHandler item, VehiclePawn vehiclePawn)
    {
        //IL_002d: Unknown result type (might be due to invalid IL or missing references)
        //IL_003e: Unknown result type (might be due to invalid IL or missing references)
        //IL_0043: Unknown result type (might be due to invalid IL or missing references)
        //IL_0048: Unknown result type (might be due to invalid IL or missing references)
        //IL_004d: Unknown result type (might be due to invalid IL or missing references)
        //IL_0059: Unknown result type (might be due to invalid IL or missing references)
        //IL_005a: Unknown result type (might be due to invalid IL or missing references)
        //IL_005f: Unknown result type (might be due to invalid IL or missing references)
        if (item.role.get_PawnRenderer() == null)
        {
            return;
        }
        foreach (Pawn handler in item.handlers)
        {
            VehiclePawn val = (VehiclePawn)(object)((handler is VehiclePawn) ? handler : null);
            if (val != null)
            {
                Vector3 val2 = ((Thing)(object)vehiclePawn).DrawPos + item.role.PawnRenderer.DrawOffsetFor(vehiclePawn.FullRotation);
                VehicleRenderer renderer = val.Drawer.renderer;
                Rot8 north = Rot8.North;
                renderer.RenderPawnAt(val2, north.AsAngle, false);
            }
        }
    }

    public void PromptToLoadErika(Pawn pawn, VehicleHandler handler)
    {
        if (handler == (VehicleHandler)null)
        {
            Messages.Message("VF_HandlerNotEnoughRoom".Translate(pawn, (Thing)(object)this), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Job job = new Job(KurinVehicleDefOf.LoadKurinErika, (Thing)(object)this);
        ((VehiclePawn)this).GiveLoadJob(pawn, handler);
        pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
        if (pawn.Spawned)
        {
            MapComponentCache.GetCachedMapComponent<VehicleReservationManager>(((Thing)this).Map).Reserve<VehicleHandler, VehicleHandlerReservation>((VehiclePawn)(object)this, pawn, pawn.CurJob, handler);
        }
    }
}
