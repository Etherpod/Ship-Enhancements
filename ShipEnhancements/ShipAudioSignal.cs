namespace ShipEnhancements;

public class ShipAudioSignal : AudioSignal
{
    FogWarpDetector _shipWarpDetector;

    public override void Awake()
    {
        base.Awake();
        _shipWarpDetector = SELocator.GetShipDetector().GetComponent<FogWarpDetector>();
        _shipWarpDetector.OnOuterFogWarpVolumeChange += OnOuterFogWarpVolumeChange;
        SetSector(SELocator.GetShipTransform().GetComponentInChildren<Sector>());
        _name = ShipEnhancements.Instance.ShipSignalName;
        _frequency = SignalFrequency.Traveler;
    }

    private void OnOuterFogWarpVolumeChange(OuterFogWarpVolume warpVolume)
    {
        _outerFogWarpVolume = warpVolume;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _shipWarpDetector.OnOuterFogWarpVolumeChange -= OnOuterFogWarpVolumeChange;
    }
}
