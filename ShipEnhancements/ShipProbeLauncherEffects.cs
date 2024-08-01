using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipProbeLauncherEffects : MonoBehaviour
{
    private SurveyorProbe _probe;
    private PlayerProbeLauncher _playerProbeLauncher;
    private PlayerProbeLauncher _shipProbeLauncher;
    private bool _hasLaunched;
    private bool _warpingOutShip;
    private bool _warpingOutPlayer;
    public bool componentDamaged;

    private void Start()
    {
        _probe = Locator.GetProbe();
        _playerProbeLauncher = Locator.GetPlayerTransform().GetComponentInChildren<PlayerProbeLauncher>();
        _shipProbeLauncher = GetComponent<PlayerProbeLauncher>();

        _probe.OnRetrieveProbe += OnRetrieveProbe;
        _shipProbeLauncher._probeRetrievalEffect.OnWarpComplete += OnWarpComplete;
        GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
        GlobalMessenger<SurveyorProbe>.AddListener("ForceRetrieveProbe", OnForceRetrieveProbe);
    }

    // Manual recall
    private void OnRetrieveProbe()
    {
        if (PlayerState.AtFlightConsole())
        {
            _hasLaunched = true;
        }
    }

    // Manual recall
    public void OnPressInteract()
    {
        if ((bool)disableScoutRecall.GetProperty() && _playerProbeLauncher._preLaunchProbeProxy.activeSelf)
        {
            _warpingOutShip = false;
            _warpingOutPlayer = true;
            _playerProbeLauncher._probeRetrievalEffect.WarpObjectOut(_playerProbeLauncher._probeRetrievalLength);
            _shipProbeLauncher._preLaunchProbeProxy.SetActive(true);
            _shipProbeLauncher._effects.PlayRetrievalClip();
            _shipProbeLauncher._probeRetrievalEffect.WarpObjectIn(_shipProbeLauncher._probeRetrievalLength);
        }
        else
        {
            _warpingOutShip = true;
            _warpingOutPlayer = false;
            _shipProbeLauncher._probeRetrievalEffect.WarpObjectOut(_shipProbeLauncher._probeRetrievalLength);
            _playerProbeLauncher._preLaunchProbeProxy.SetActive(true);
            _playerProbeLauncher._effects.PlayRetrievalClip();
            _playerProbeLauncher._probeRetrievalEffect.WarpObjectIn(_playerProbeLauncher._probeRetrievalLength);
            _hasLaunched = false;
        }
    }
    
    private void OnWarpComplete()
    {
        if (_warpingOutShip)
        {
            _shipProbeLauncher._preLaunchProbeProxy.SetActive(false);
            _warpingOutShip = false;
        }
        else if (_warpingOutPlayer)
        {
            _playerProbeLauncher._preLaunchProbeProxy.SetActive(false);
            _warpingOutPlayer = false;
        }
    }

    // Manual recall
    private void OnEnterFlightConsole(OWRigidbody shipBody)
    {
        if (ShipEnhancements.Instance.probeDestroyed || componentDamaged || (bool)disableScoutRecall.GetProperty()) return;
        _playerProbeLauncher._preLaunchProbeProxy.SetActive(false);
        _shipProbeLauncher._preLaunchProbeProxy.SetActive(true);
        _shipProbeLauncher._probeRetrievalEffect.WarpObjectIn(_shipProbeLauncher._probeRetrievalLength);
    }
    

    // Manual recall
    private void OnExitFlightConsole()
    {
        if (ShipEnhancements.Instance.probeDestroyed || _probe.IsLaunched() || _hasLaunched || (bool)disableScoutRecall.GetProperty()) return;
        _playerProbeLauncher._preLaunchProbeProxy.SetActive(true);
        _warpingOutShip = true;
        _shipProbeLauncher._probeRetrievalEffect.WarpObjectOut(_shipProbeLauncher._probeRetrievalLength);
    }

    // Component
    public void SetDamaged(bool damaged)
    {
        componentDamaged = damaged;
    }

    // Manual recall
    public void OnForceRetrieveProbe(SurveyorProbe probe)
    {
        if (probe != _probe || ShipEnhancements.Instance.probeDestroyed) return;

        if ((bool)disableScoutRecall.GetProperty() || componentDamaged || Locator.GetShipBody().GetComponent<ShipDamageController>().IsSystemFailed())
        {
            _probe.Deactivate();
            ShipEnhancements.Instance.probeDestroyed = true;
            return;
        }

        _shipProbeLauncher._activeProbe = _probe;
        _shipProbeLauncher.RetrieveProbe(true, true);
        GetComponentInChildren<ShipProbePickupVolume>().OnForceRetrieveProbe();
        NotificationData notificationData = new NotificationData(NotificationTarget.All, UITextLibrary.GetString(UITextType.NotificationScoutRecall), 5f, true);
        NotificationManager.SharedInstance.PostNotification(notificationData, false);
        _hasLaunched = true;
    }

    private void OnDestroy()
    {
        _probe.OnRetrieveProbe -= OnRetrieveProbe;
        _shipProbeLauncher._probeRetrievalEffect.OnWarpComplete -= OnWarpComplete;
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
        GlobalMessenger<SurveyorProbe>.RemoveListener("ForceRetrieveProbe", OnForceRetrieveProbe);
    }
}
