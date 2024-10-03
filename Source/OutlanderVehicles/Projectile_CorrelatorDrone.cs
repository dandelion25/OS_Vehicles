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
    protected int signalOscillationCount;
    private int oscillationsRemaining;

    private IntVec3 positionCell => base.ExactPosition.Yto0().ToIntVec3();

    //private float distanceTraversed => base.DistanceCoveredFraction * (base.destination - base.origin).magnitude;
    //private IEnumerable<Thing> affectedTargets = Enumerable.Empty<Thing>();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref signalOscillationCount, "signalOscillationCount", (int)((intendedTarget.CenterVector3 - origin).magnitude / base.def.projectile.explosionRadius));
        Scribe_Values.Look(ref oscillationsRemaining, "oscillationsRemaining", signalOscillationCount);
    }

    public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
    {
        signalOscillationCount = Math.Max((int)((intendedTarget.CenterVector3 - origin).magnitude / (base.def.projectile.explosionRadius*0.8)),1);
        oscillationsRemaining = signalOscillationCount;
        base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
    }

    public override void Tick()
    {
        base.Tick();
        if (base.Map != null && (1 - base.DistanceCoveredFraction) > (float)oscillationsRemaining / (float)signalOscillationCount - 0.01)
        {
            IEnumerable<Thing> affectedThings = new List<Thing>();
            int num = (int)(GenRadial.NumCellsInRadius(searchRad) * Rand.Value);
            for (int i = 0; i<num; i++)
            {
                IntVec3 intVec = positionCell + GenRadial.RadialPattern[i];
                if (!GenSight.LineOfSight(positionCell, intVec, base.Map, skipFirstCell: true))
                {
                    continue;
                }
                IEnumerable<Thing> thingList = intVec.GetThingList(base.Map); //from tthing in intVec.GetThingList(base.Map) where (tthing is Pawn) select (Pawn)tthing;
                affectedThings.Concat(thingList);
            }
            base.def.projectile.explosionEffect.Spawn(positionCell, base.Map, searchRad*2);
            foreach (Thing target in affectedThings)
            {
                Irradiate(target);
            }

            //MoteMaker.MakeStaticMote(base.Position,base.Map,)
            //ThrowMoteTrail(base.ExactPosition, base.Map, Vector3.Angle(base.origin, base.ExactPosition));W

            oscillationsRemaining--;

        }
    }

    /*protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        base.Impact(hitThing, blockedByShield);
    }*/

    public void Irradiate(Thing target)
    {
        BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, target, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
        Find.BattleLog.Add(battleLogEntry_RangedImpact);
        if (target != null)
        {
            Pawn pawn;
            bool instigatorGuilty = (pawn = launcher as Pawn) == null || !pawn.Drafted;
            DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
            dinfo.SetWeaponQuality(equipmentQuality);
            target.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
            Pawn pawn2 = target as Pawn;
            if (def.projectile.extraDamages != null)
            {
                foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
                {
                    if (Rand.Chance(extraDamage.chance))
                    {
                        DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                        target.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                    }
                }
            }

            if (Rand.Chance(def.projectile.bulletChanceToStartFire) && (pawn2 == null || Rand.Chance(FireUtility.ChanceToAttachFireFromEvent(pawn2))))
            {
                target.TryAttachFire(def.projectile.bulletFireSizeRange.RandomInRange, launcher);
            }


            if (pawn2 != null)
            {
                bool targetingEnemy = launcher.Faction != pawn2.Faction;
                // case that affected pawn is mechanoid: mechvirus behavior
                if (pawn2.RaceProps.IsMechanoid && Rand.Value < ArmorPenetration && targetingEnemy)
                {
                    pawn2.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.BerserkMechanoid, null, forced: false, forceWake: true);
                }
                else
                { // CORRELATOR SIGNAL EXPOSURE - INCREASED PER HIT BY FACTOR OF ARMOR PENETRATION
                    Hediff affectedHediff = pawn2.health.GetOrAddHediff(DefDatabase<HediffDef>.GetNamed("MJ_CorrelatorHediff"));
                    if (affectedHediff != null)
                    {
                        affectedHediff.Severity = affectedHediff.Severity + ArmorPenetration; //.Severity += ArmorPenetration;
                        switch ((Rand.Value < affectedHediff.Severity) ? ((Rand.Value < affectedHediff.Severity) ? ((Rand.Value < affectedHediff.Severity) ? 3 : 2) : 1) : 0)
                        {
                            case 1: pawn2.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, null, forced: false, forceWake: true); break;
                            case 2: pawn2.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Wander_Psychotic, null, forced: false, forceWake: true); break;
                            case 3: pawn2.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, forced: false, forceWake: true); break;

                        }
                    }
                }
                base.def.projectile.explosionEffect.SpawnAttached(pawn2, base.Map, 1);
            }
        }
    }
}