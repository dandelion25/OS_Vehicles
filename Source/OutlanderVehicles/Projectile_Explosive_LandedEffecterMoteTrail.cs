
using RimWorld;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System;

namespace OutlanderVehicles;

public class Projectile_Explosive_LandedEffecterMoteTrail : Projectile_Explosive
{
    private Vector3 LookTowards => new Vector3(destination.x - origin.x, def.Altitude, destination.z - origin.z + ArcHeightFactor * (4f - 8f * base.DistanceCoveredFraction));

    private float ArcHeightFactor
    {
        get
        {
            float num = def.projectile.arcHeightFactor;
            float num2 = (destination - origin).MagnitudeHorizontalSquared();
            if (num * num > num2 * 0.2f * 0.2f)
            {
                num = Mathf.Sqrt(num2) * 0.2f;
            }
            return num;
        }
    }

    private IEnumerable<SubEffecterDef> effects;

    public override Quaternion ExactRotation => Quaternion.LookRotation(LookTowards);

    public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
    {
        effects = (from effecter in def.projectile.landedEffecter.children where effecter.subEffecterClass == typeof(SubEffecter_SprayerChance) select effecter);
        base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
    }

    public override void Tick()
    {
        base.Tick();
        if (base.Map != null)
        {
            float num = ArcHeightFactor * GenMath.InverseParabola(base.DistanceCoveredFraction);
            Vector3 drawPos = DrawPos;
            Vector3 val = drawPos + new Vector3(0f, 0f, 1f) * num;
            foreach (SubEffecterDef effecter in effects)
            {
                if(effecter.chancePerTick + effecter.positionLerpFactor * base.DistanceCoveredFraction > Rand.Value)
                {
                    ThrowMoteTrail(val, base.Map, Vector3.Angle(base.origin, val), effecter);
                }
            }
        }
    }

    public void ThrowMoteTrail(Vector3 loc, Map map, float angle, SubEffecterDef effecter)
    {
        FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, effecter.fleckDef);
        dataStatic.scale = effecter.scale.RandomInRange;
        dataStatic.rotationRate = effecter.rotationRate.RandomInRange;
        dataStatic.velocityAngle = angle;
        dataStatic.velocitySpeed = effecter.speed.RandomInRange;
        map.flecks.CreateFleck(dataStatic);
    }
}
