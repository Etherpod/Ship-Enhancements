using System;
using UnityEngine;

namespace ShipEnhancements;

public class QuantumShip : VisibilityObject
{
    private bool _visibleInProbeSnapshot;
    private float _maxSnapshotLockRange = 5000f;
    private bool _subscribedToRemoveSnapshotEvent;
    private bool _isPlayerStandingOnObject;
    private bool _hasEverBeenEnabled;
    private bool _wasLocked;

    private void Start()
    {
        CheckEnabled();
    }

    public override void CheckEnabled()
    {
        bool objectEnabled = enabled;
        base.CheckEnabled();
        if (enabled && (!objectEnabled || !_hasEverBeenEnabled))
        {
            _hasEverBeenEnabled = true;
            GlobalMessenger<ProbeCamera>.AddListener("ProbeSnapshot", OnProbeSnapshot);
            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", OnSwitchActiveCamera);
            return;
        }
        if (!enabled && objectEnabled)
        {
            GlobalMessenger<ProbeCamera>.RemoveListener("ProbeSnapshot", OnProbeSnapshot);
            GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", OnSwitchActiveCamera);
        }
    }

    public bool IsPlayerEntangled()
    {
        return _isPlayerStandingOnObject;
    }

    public void SetPlayerStandingOnObject(bool isPlayerStandingOnObject)
    {
        _isPlayerStandingOnObject = isPlayerStandingOnObject;
    }

    public bool IsLocked()
    {
        return IsLockedByProbeSnapshot() || IsLockedByActiveCamera() || IsLockedByPlayerContact();
    }

    private bool IsLockedByProbeSnapshot()
    {
        return _visibleInProbeSnapshot && Locator.GetActiveCamera().CompareTag("MainCamera") 
            && Locator.GetToolModeSwapper().GetToolMode() == ToolMode.Probe;
    }

    private bool IsLockedByActiveCamera()
    {
        return PlayerState.IsInsideShip() || (IsVisible() && IsIlluminated());
    }

    private bool IsLockedByPlayerContact()
    {
        return PlayerState.IsInsideShip() || (IsPlayerEntangled() && (IsIlluminated() || Locator.GetFlashlight().IsFlashlightOn()));
    }

    private void Collapse()
    {
        var body = SELocator.GetShipBody();
        body.gameObject.SetActive(false);

        ReferenceFrameTracker component = Locator.GetPlayerBody().GetComponent<ReferenceFrameTracker>();
        if (component.GetReferenceFrame(true) != null && component.GetReferenceFrame(true).GetOWRigidBody() == body)
        {
            component.UntargetReferenceFrame(false);
        }
        MapMarker component2 = body.GetComponent<MapMarker>();
        if (component2 != null)
        {
            component2.DisableMarker();
        }

        GlobalMessenger.FireEvent("ShipDestroyed");
        enabled = false;
    }

    public override void Update()
    {
        base.Update();
        bool locked = IsLocked();
        if (_wasLocked && !locked)
        {
            Collapse();
        }
        _wasLocked = locked;
    }

    private void OnProbeSnapshot(ProbeCamera probeCamera)
    {
        float num = Vector3.Distance(transform.position, probeCamera.transform.position);
        _visibleInProbeSnapshot = num < _maxSnapshotLockRange && IsIlluminated() && !probeCamera.HasInterference() 
            && CheckVisibilityFromProbe(probeCamera.GetOWCamera());
        if (_visibleInProbeSnapshot && !_subscribedToRemoveSnapshotEvent)
        {
            GlobalMessenger.AddListener("Probe Snapshot Removed", OnProbeSnapshotRemoved);
            _subscribedToRemoveSnapshotEvent = true;
            return;
        }
        if (!_visibleInProbeSnapshot && _subscribedToRemoveSnapshotEvent)
        {
            GlobalMessenger.RemoveListener("Probe Snapshot Removed", OnProbeSnapshotRemoved);
            _subscribedToRemoveSnapshotEvent = false;
        }
    }

    private void OnProbeSnapshotRemoved()
    {
        _visibleInProbeSnapshot = false;
        _subscribedToRemoveSnapshotEvent = false;
        GlobalMessenger.RemoveListener("Probe Snapshot Removed", OnProbeSnapshotRemoved);
    }

    private void OnSwitchActiveCamera(OWCamera activeCamera)
    {
        if (!IsLockedByProbeSnapshot() && !IsLockedByPlayerContact())
        {
            Collapse();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger<ProbeCamera>.RemoveListener("ProbeSnapshot", OnProbeSnapshot);
        GlobalMessenger.RemoveListener("Probe Snapshot Removed", OnProbeSnapshotRemoved);
        GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", OnSwitchActiveCamera);
    }
}
