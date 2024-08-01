using static ShipEnhancements.ShipEnhancements.Settings;

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
        if ((PlayerState.AtFlightConsole() || (bool)disableScoutRecall.GetProperty()) && !(_probeLauncherEffects.componentDamaged || ShipEnhancements.Instance.probeDestroyed))
        {
            if ((bool)disableScoutRecall.GetProperty())
            {
                _interactReceiver.ChangePrompt("Insert Scout");
            }
            _interactReceiver.EnableInteraction();
        }
    }

    protected override void OnLaunchProbe()
    {
        _interactReceiver.DisableInteraction();
    }

    protected override void OnPressInteract()
    {
        if (_probeLauncherEffects.componentDamaged || ShipEnhancements.Instance.probeDestroyed || (bool)disableScoutLaunching.GetProperty()) return;

        if ((bool)disableScoutRecall.GetProperty() && _probeLauncher._preLaunchProbeProxy.activeSelf)
        {
            _interactReceiver.ChangePrompt("Pick up Scout");
            _interactReceiver.ResetInteraction();
            _probeLauncherEffects.OnPressInteract();
            return;
        }

        canRetrieveProbe = true;
        _probeLauncher.RetrieveProbe(true, false);

        if ((bool)disableScoutRecall.GetProperty())
        {
            _interactReceiver.ResetInteraction();
        }
        else
        {
            _interactReceiver.DisableInteraction();
        }
        _probeLauncherEffects.OnPressInteract();
        canRetrieveProbe = false;
    }

    public void OnForceRetrieveProbe()
    {
        _interactReceiver.EnableInteraction();
    }
}