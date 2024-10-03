using RimWorld;
using UnityEngine;
using Verse;

namespace OutlanderVehicles;

public class CompProperties_MechCommandRelay : CompProperties
{
    public float retuneDays = 3f;

    public float tuneSeconds = 5f;

    public int powerConsumptionIdle = 0;

    public int emissionInterval;

    public float relayCommandRange = 12f;

    public int signalLockDuration = 10;

    public EffecterDef untunedEffect;

    public EffecterDef tuningEffect;

    public EffecterDef tunedEffect;

    public EffecterDef retuningEffect;

    public Vector3 effecterOffset;

    public SoundDef tuningCompleteSound;

    public CompProperties_MechCommandRelay()
    {
        compClass = typeof(Comp_MechCommandRelay);
    }
}
