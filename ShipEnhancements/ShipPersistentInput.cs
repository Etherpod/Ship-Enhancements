using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipPersistentInput : ThrusterController
{
    private RulesetDetector _rulesetDetector;
    private ShipThrusterController _thrustController;
    private Vector3 _currentInput;
    private bool _fuelDepleted;

    public override void Awake()
    {
        base.Awake();
        _rulesetDetector = SELocator.GetShipDetector().GetComponent<RulesetDetector>();
        _thrustController = GetComponent<ShipThrusterController>();
    }

    private void Start()
    {
        enabled = false;
    }

    public override Vector3 ReadRotationalInput()
    {
        return Vector3.zero;
    }

    public override Vector3 ReadTranslationalInput()
    {
        if (SELocator.GetAutopilotPanelController().IsAutopilotActive() || _currentInput == Vector3.zero)
        {
            ShipEnhancements.WriteDebugMessage("Abort");
            enabled = false;
            return Vector3.zero;
        }

        float num = Mathf.Min(_rulesetDetector.GetThrustLimit(), _thrustController._thrusterModel.GetMaxTranslationalThrust()) 
            / _thrustController._thrusterModel.GetMaxTranslationalThrust();
        return _currentInput * ((bool)enableThrustModulator.GetProperty() ? ShipEnhancements.Instance.ThrustModulatorLevel / 5f : 1f) * num;
    }

    public void StartRecordingInput()
    {
        ShipEnhancements.WriteDebugMessage("Start recording");
        _currentInput = Vector3.zero;
        enabled = false;
    }

    public void StopRecordingInput()
    {
        ShipEnhancements.WriteDebugMessage("Stop recording");
        if (!SELocator.GetAutopilotPanelController().IsAutopilotActive())
        {
            _currentInput = _thrustController._lastTranslationalInput;
            ShipEnhancements.WriteDebugMessage("Set input");
            if (_currentInput != Vector3.zero)
            {
                enabled = true;
            }
        }
    }

    public void SetInputEnabled(bool state)
    {
        ShipEnhancements.WriteDebugMessage("Set enabled: " + state);
        if (state && _currentInput != Vector3.zero)
        {
            ShipEnhancements.WriteDebugMessage("Actually enable");
            enabled = true;
        }
        else
        {
            enabled = false;
        }
    }

    public void SetInputRemote(Vector3 newInput)
    {
        if (_fuelDepleted) return;
        _currentInput = newInput;
        SetInputEnabled(true);
    }

    private void OnFuelDepleted()
    {
        _fuelDepleted = true;
        SetInputEnabled(false);
    }

    private void OnFuelRestored()
    {
        _fuelDepleted = false;
        SetInputEnabled(true);
    }

    /*public override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
    }*/
}
