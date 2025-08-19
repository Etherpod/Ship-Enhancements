namespace ShipEnhancements;

public class PumpOxygenDetector : OxygenDetector
{
    public override void AddVolume(EffectVolume effectVolume)
    {
        if (effectVolume.GetComponentInParent<ShipBody>() == null
            && effectVolume.GetComponentInParent<ResourcePump>() == null)
        {
            base.AddVolume(effectVolume);
        }
    }

    public override void OnVolumeRemoved(EffectVolume effectVol)
    {
        if (effectVol.GetComponentInParent<ShipBody>() == null
            && effectVol.GetComponentInParent<ResourcePump>() == null)
        {
            base.OnVolumeRemoved(effectVol);
        }
    }
}
