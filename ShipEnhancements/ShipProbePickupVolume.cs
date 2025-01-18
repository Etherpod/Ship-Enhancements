using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipProbePickupVolume : ProbePickupVolume
{
    private PlayerProbeLauncher _shipProbeLauncher;
    private ShipProbeLauncherEffects _probeLauncherEffects;
    private readonly string _pickUpPrompt = "Pick up Scout";
    private readonly string _insertPrompt = "Insert Scout";

    public static bool probeInShip;
    public static bool canTransferProbe;

    protected override void Start()
    {
        base.Start();
        _probeLauncher = Locator.GetPlayerTransform().GetComponentInChildren<PlayerProbeLauncher>();
        _shipProbeLauncher = GetComponentInParent<PlayerProbeLauncher>();
        _probeLauncherEffects = _shipProbeLauncher.GetComponent<ShipProbeLauncherEffects>();
        if (!(bool)disableScoutRecall.GetProperty() || (bool)disableScoutLaunching.GetProperty())
        {
            _interactReceiver.DisableInteraction();
        }
        else if ((bool)disableScoutRecall.GetProperty())
        {
            _interactReceiver.ChangePrompt(_insertPrompt);
        }
        _shipProbeLauncher._preLaunchProbeProxy.SetActive(false);
        probeInShip = false;
    }

    protected override void OnRetrieveProbe()
    {
        if (_probeLauncherEffects.componentDamaged || ShipEnhancements.Instance.probeDestroyed) return;

        if ((bool)disableScoutRecall.GetProperty() && !(bool)disableScoutLaunching.GetProperty() && !PlayerState.AtFlightConsole())
        {
            probeInShip = false;
            _interactReceiver.ChangePrompt(_insertPrompt);
            _interactReceiver.EnableInteraction();
        }
        else if (PlayerState.AtFlightConsole() && (bool)enableManualScoutRecall.GetProperty())
        {
            probeInShip = true;
            _interactReceiver.EnableInteraction();
        }
    }

    protected override void OnLaunchProbe()
    {
        probeInShip = false;
        _interactReceiver.DisableInteraction();
    }

    protected override void OnPressInteract()
    {
        if (_probeLauncherEffects.componentDamaged || ShipEnhancements.Instance.probeDestroyed) return;

        if ((bool)disableScoutRecall.GetProperty() && !(bool)disableScoutLaunching.GetProperty() && _probeLauncher._preLaunchProbeProxy.activeSelf)
        {
            probeInShip = true;
            canTransferProbe = true;
            _interactReceiver.ChangePrompt(_pickUpPrompt);
            _interactReceiver.ResetInteraction();
            _probeLauncher.ExitPhotoMode();
            _probeLauncherEffects.OnPressInteract();
            canTransferProbe = false;
            return;
        }

        probeInShip = false;
        canRetrieveProbe = true;
        _probeLauncher.RetrieveProbe(false, false);

        if ((bool)disableScoutRecall.GetProperty())
        {
            _interactReceiver.ChangePrompt(_insertPrompt);
            _interactReceiver.ResetInteraction();
        }
        else
        {
            _interactReceiver.DisableInteraction();
        }
        _probeLauncherEffects.OnPressInteract();
        canRetrieveProbe = false;
    }

    public void RecallProbeFromShip()
    {
        probeInShip = false;
        canRetrieveProbe = true;
        _probeLauncher.RetrieveProbe(false, false);
        _probeLauncherEffects.OnPressInteract();
        canRetrieveProbe = false;
    }

    public void OnForceRetrieveProbe()
    {
        if (ShipEnhancements.Instance.probeDestroyed) return;
        probeInShip = true;
        _interactReceiver.EnableInteraction();
    }
}