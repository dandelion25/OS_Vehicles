using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace OutlanderVehicles;

public class Projectile_CorrelatorDrone : Projectile
{

    /*protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        if (blockedByShield || def.projectile.explosionDelay == 0)
        {
            Explode();
            return;
        }
    }*/
    private float searchRad => base.DistanceCoveredFraction * base.def.projectile.explosionRadius;

    private IntVec3 lastCell = new IntVec3(0,0,0);
    private IEnumerable<Thing> affectedTargets = Enumerable.Empty<Thing>();
    private FleckDef floaty;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref affectedTargets, "ticksToDetonation", Enumerable.Empty<Thing>());

    }

    public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
    {
        floaty = DefDatabase<FleckDef>.GetNamed("PsycastPsychicEffect");
        base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
    }


    public override void Tick()
    {
        base.Tick();
        if (base.Map != null && base.Position != lastCell) // triggers when projectile enters new cell
        {
            int num = (int)(GenRadial.NumCellsInRadius(searchRad) * Rand.Value);
            for (int i = 0; i<num; i++)
            {
                IntVec3 intVec = base.Position + GenRadial.RadialPattern[i];
                if (!GenSight.LineOfSight(base.Position, intVec, base.Map, skipFirstCell: true))
                {
                    continue;
                }
                IEnumerable<Thing> list = intVec.GetThingList(base.Map); //from tthing in intVec.GetThingList(base.Map) where (tthing is Pawn) select (Pawn)tthing;
                affectedTargets.Concat(list);
            }
            lastCell = base.Position;
            ThrowMoteTrail(base.ExactPosition, base.Map, Vector3.Angle(base.origin, base.ExactPosition));
        }
    }

    public void ThrowMoteTrail(Vector3 loc, Map map, float angle)
    {
        FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, floaty);
        dataStatic.scale = searchRad/2.5f; //2.5 is default graphic scale of fleck "PsycastPsychicEffect"
        dataStatic.velocityAngle = angle;
        dataStatic.velocitySpeed = base.def.projectile.speed / 10;
        map.flecks.CreateFleck(dataStatic);
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        base.Impact(hitThing, blockedByShield);
        foreach (Thing thingTarget in affectedTargets)
        {
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, thingTarget, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            if (thingTarget != null)
            {
                Pawn pawn;
                bool instigatorGuilty = (pawn = launcher as Pawn) == null || !pawn.Drafted;
                DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                dinfo.SetWeaponQuality(equipmentQuality);
                thingTarget.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                Pawn pawn2 = thingTarget as Pawn;
                if (def.projectile.extraDamages != null)
                {
                    foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                            thingTarget.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                }

                if (Rand.Chance(def.projectile.bulletChanceToStartFire) && (pawn2 == null || Rand.Chance(FireUtility.ChanceToAttachFireFromEvent(pawn2))))
                {
                    thingTarget.TryAttachFire(def.projectile.bulletFireSizeRange.RandomInRange, launcher);
                }

                
                if (pawn2 != null)
                {
                    // case that affected pawn is mechanoid: use simple mechvirus
                    bool targetingEnemy = launcher.Faction != pawn2.Faction;
                    if (pawn2.RaceProps.IsMechanoid && Rand.Value < ArmorPenetration && targetingEnemy)
                    {
                        pawn2.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.BerserkMechanoid, null, forced: false, forceWake: true);
                    }
                    else
                    { // CORRELATOR SIGNAL EXPOSURE - INCREASED PER HIT BY FACTOR OF ARMOR PENETRATION
                        Hediff affectedHediff = pawn2.health.GetOrAddHediff(DefDatabase<HediffDef>.GetNamed("MJ_CorrelatorHediff"));
                        if (affectedHediff != null)
                        {
                            switch ((Rand.Value < affectedHediff.Severity) ? ((Rand.Value < affectedHediff.Severity) ? ((Rand.Value < affectedHediff.Severity) ? 3 : 2) : 1) : 0)
                            {
                                case 1:
                                    pawn2.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, null, forced: false, forceWake: true);
                                    break;
                                case 2:
                                    pawn2.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Wander_Psychotic, null, forced: false, forceWake: true);
                                    break;
                                case 3:
                                    pawn2.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, forced: false, forceWake: true);
                                    break;

                            }
                            affectedHediff.Severity += ArmorPenetration;
                        }
                    }
                }

                return;
            }
        }
    }
}