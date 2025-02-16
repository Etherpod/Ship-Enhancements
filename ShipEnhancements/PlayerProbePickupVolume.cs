using UnityEngine;

namespace ShipEnhancements;

public class PlayerProbePickupVolume : ProbePickupVolume
{
    protected override void Start()
    {
        base.Start();
    }

    protected override void OnLaunchProbe()
    {
        _interactReceiver.EnableInteraction();
        _probeLauncher._activeProbe = _probe;
    }

    protected override void OnSuitUp()
    {
        if (ShipEnhancements.Instance.probeDestroyed) return;

        if (_probeLauncher._activeProbe == _probe)
        {
            _interactReceiver.EnableInteraction();
        }
    }

    protected override void OnRemoveSuit()
    {
        _interactReceiver.DisableInteraction();
    }

    protected override void OnPressInteract()
    {
        base.OnPressInteract();
    }
}