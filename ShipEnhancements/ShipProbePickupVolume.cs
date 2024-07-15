using UnityEngine;

namespace ShipEnhancements;

public class ShipProbePickupVolume : ProbePickupVolume
{
    private PlayerProbeLauncher _shipProbeLauncher;
    private ShipProbeLauncherEffects _probeLauncherEffects;

    protected override void Start()
    {
        base.Start();
        _probeLauncher = Locator.GetPlayerTransform().GetComponentInChildren<PlayerProbeLauncher>();
        _shipProbeLauncher = GetComponentInParent<PlayerProbeLauncher>();
        _probeLauncherEffects = _shipProbeLauncher.GetComponent<ShipProbeLauncherEffects>();
        _interactReceiver.DisableInteraction();
        _shipProbeLauncher._preLaunchProbeProxy.SetActive(false);
    }

    protected override void OnRetrieveProbe()
    {
        if (PlayerState.AtFlightConsole() && !(_probeLauncherEffects.componentDamaged || ShipEnhancements.Instance.probeDestroyed))
        {
            _interactReceiver.EnableInteraction();
        }
    }

    protected override void OnLaunchProbe()
    {
        _interactReceiver.DisableInteraction();
    }

    protected override void OnPressInteract()
    {
        if (_probeLauncherEffects.componentDamaged || ShipEnhancements.Instance.probeDestroyed) return;

        canRetrieveProbe = true;
        _probeLauncher.RetrieveProbe(true, false);
        _interactReceiver.DisableInteraction();
        _probeLauncherEffects.OnPressInteract();
        canRetrieveProbe = false;
    }

    public void OnForceRetrieveProbe()
    {
        _interactReceiver.EnableInteraction();
    }
}