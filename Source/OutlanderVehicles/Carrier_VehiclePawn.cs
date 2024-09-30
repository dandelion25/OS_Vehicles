using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

using Verse.Sound;
using static System.Net.Mime.MediaTypeNames;

namespace OutlanderVehicles;

public class Carrier_VehiclePawn : VehiclePawn
{
    public static int vehicleContainerCapacity = 0;
    public static int loadDistance;
    public static Rot4 accessDirection;
    public static int ContainerUsage(VehiclePawn vehiclePawn) { return vehiclePawn.VehicleRect().Area; }


    public ThingOwner<VehiclePawn> vehicleContainer = new ThingOwner<VehiclePawn>();
    public int vehicleContainerFillAmount { get { return (from cargo in (IEnumerable<VehiclePawn>)vehicleContainer select cargo.VehicleRect().Area).Sum(); } }
    public Rot4 carrierAccessDirection { get { return Rot4.FromAngleFlat(this.Rotation.AsVector2.RotatedBy(accessDirection).AngleTo(Vector2.up)); } }
    public IEnumerable<IntVec3> carrierAccessCells { get { return from i in Enumerable.Range(0,loadDistance) select this.Position + carrierAccessDirection.FacingCell * i; } }

    public bool Loadable(VehiclePawn thing)
    {
        IntVec3 loadvec = new IntVec3(((int)(this.Rotation.AsVector2.RotatedBy(accessDirection) * loadDistance).x), 0, (int)((this.Rotation.AsVector2.RotatedBy(accessDirection) * loadDistance).y));

        return false;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        base.GetGizmos();

        Command_Action loadVehicleAction = new Command_Action
        {
            defaultLabel = "Load Vehicle",
            defaultDesc = "Load an unmanned vehicle into the transport.",
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
                        return val5 != null && ((Thing)(object)val5).def.size.Area > 1 && ((((Thing)(object)val5).Faction == Faction.OfPlayer) ? true : false);
                    }
                }, (Action<LocalTargetInfo>)delegate (LocalTargetInfo target)
                {
                    Thing thing3 = target.Thing;
                    VehiclePawn val4 = (VehiclePawn)(object)((thing3 is VehiclePawn) ? thing3 : null);
                    if (val4 != null && !(target.Thing is Carrier_VehiclePawn))
                    {
                        if (carrierAccessCells.Contains(val4.Position))
                        {
                            if(vehicleContainerCapacity - vehicleContainerFillAmount >= val4.VehicleRect().Area)
                            {
                                vehicleContainer.TryAdd(val4, false);
                            }
                            else
                            {
                                Messages.Message($"Insufficient space in vehicle: {val4.Label} takes {val4.VehicleRect().Area} cells, when {vehicleContainerCapacity - vehicleContainerCapacity} are available.", MessageTypeDefOf.NeutralEvent);
                            }
                        }
                        else
                        {
                            Messages.Message($"{val4.Label} is not aligned with the transport. Place it up to {loadDistance} cells {carrierAccessDirection} of the transport's position.", MessageTypeDefOf.NeutralEvent);
                        }
                    }
                    else
                    {
                        Messages.Message($"{thing3.Label} is not a valid target for this operation.", MessageTypeDefOf.NeutralEvent);
                    }
                }, (VehiclePawn)(object)this, (Action)null, (Texture2D)null);
            }
        };
        yield return loadVehicleAction;

        foreach (VehiclePawn cargo in vehicleContainer) {
            Command_Action deployVehicle = new Command_Action
            {
                defaultLabel = "Deploy " + cargo.Label,
                defaultDesc = $"deploy a vehicle from the {accessDirection} side of the transport.",
                icon = (Texture)cargo.Graphic.MatSouth.mainTexture,
                action = delegate
                {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    bool deployed = false;
                    bool CanDeployAt(IntVec3 cell)
                    {
                        if (!cell.InBounds(this.Map)) { return false; }
                        if (this.Map.thingGrid.ThingAt<VehiclePawn>(cell) != null) { return false; }
                        return cargo.LocationRestrictedBySize(cell, Rot8.FromAngle(this.Rotation.AsAngle), this.Map);//this.Map.pathing.Normal.pathGrid.WalkableFast(cell);
                    }
                    foreach (IntVec3 deployCell in this.carrierAccessCells)
                    {
                        deployed = vehicleContainer.TryDrop(cargo, deployCell, this.Map, ThingPlaceMode.Direct, out var _, null, CanDeployAt);
                        if (deployed)
                        {
                            break;
                        }
                    }
                    if (!deployed)
                    {
                        Messages.Message($"{this.Label} failed to deploy {cargo.Label}.", MessageTypeDefOf.NeutralEvent);
                    }


                }
            };
        }
    }


    // When carrier is destroyed, attempt to release all contained vehicles.
    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if(Spawned)
        {
            RotatingList<IntVec3> rotatingList = this.VehicleRect().EdgeCells.InRandomOrder().ToRotatingList();
            while (vehicleContainer.Count > 0)
            {
                IntVec3 next = rotatingList.Next;
                bool CanDeployAt(IntVec3 cell)
                {
                    if (!cell.InBounds(this.Map)) { return false; }
                    if (this.Map.thingGrid.ThingAt<VehiclePawn>(cell) != null) { return false; }
                    return vehicleContainer[0].LocationRestrictedBySize(cell, Rot8.FromAngle(this.Rotation.AsAngle), this.Map);
                }
                if (!vehicleContainer.TryDrop(vehicleContainer[0], next, this.Map, ThingPlaceMode.Near, out var _, null, CanDeployAt))
                {
                    Log.Warning($"Failing to drop {vehicleContainer[0]} from container {vehicleContainer.Owner}");
                }
            }
        }
        base.Destroy(mode);
    }
}
