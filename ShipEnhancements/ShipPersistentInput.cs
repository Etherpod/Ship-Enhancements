using System;
using UnityEngine;

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
        return _currentInput;
    }

    private void OnEnterFlightConsole(OWRigidbody shipBody)
    {
        _currentInput = Vector3.zero;
        enabled = false;
    }

    private void OnExitFlightConsole()
    {
        _currentInput = GetComponent<ShipThrusterController>()._lastTranslationalInput;
        if (_currentInput != Vector3.zero)
        {
            enabled = true;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
    }
}
