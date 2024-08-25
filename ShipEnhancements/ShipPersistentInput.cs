using System;
using UnityEngine;

namespace ShipEnhancements;

public class ShipPersistentInput : ThrusterController
{
    private Vector3 _currentInput;

    private void Start()
    {
        GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
        enabled = false;
    }

    public override Vector3 ReadRotationalInput()
    {
        return Vector3.zero;
    }

    public override Vector3 ReadTranslationalInput()
    {
        return _currentInput * ShipEnhancements.Instance.thrustModulatorLevel / 5f;
    }

    private void OnEnterFlightConsole(OWRigidbody shipBody)
    {
        _currentInput = Vector3.zero;
        enabled = false;
    }

    private void OnExitFlightConsole()
    {
        ShipThrusterController thrusterController = GetComponent<ShipThrusterController>();
        _currentInput = GetComponent<ShipThrusterController>()._lastTranslationalInput;
        Autopilot autopilot = GetComponent<Autopilot>();
        bool autopilotEnabled = autopilot.IsMatchingVelocity() || autopilot.IsFlyingToDestination() || autopilot.IsApproachingDestination() || autopilot.IsLiningUpDestination();

        if (_currentInput != Vector3.zero && !autopilotEnabled && !thrusterController.RequiresIgnition())
        {
            enabled = true;
        }
        else if (thrusterController._isIgniting && (bool)ShipEnhancements.Settings.shipIgnitionCancelFix.GetProperty())
        {
            thrusterController._isIgniting = false;
            GlobalMessenger.FireEvent("CancelShipIgnition");
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
    }
}
