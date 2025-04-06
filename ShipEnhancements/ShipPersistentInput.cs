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

        ShipEnhancements.Instance.OnFuelDepleted += OnFuelDepleted;
        ShipEnhancements.Instance.OnFuelRestored += OnFuelRestored;
    }

    private void Start()
    {
        SetInputEnabled(false);
    }

    public override Vector3 ReadRotationalInput()
    {
        return Vector3.zero;
    }

    public override Vector3 ReadTranslationalInput()
    {
        if (SELocator.GetAutopilotPanelController().IsAutopilotActive() || _currentInput == Vector3.zero)
        {
            enabled = false;
            return Vector3.zero;
        }

        if (!SELocator.GetShipResources().AreThrustersUsable())
        {
            return Vector3.zero;
        }

        float num = Mathf.Min(_rulesetDetector.GetThrustLimit(), _thrustController._thrusterModel.GetMaxTranslationalThrust()) 
            / _thrustController._thrusterModel.GetMaxTranslationalThrust();
        return _currentInput * ((bool)enableThrustModulator.GetProperty() ? ShipEnhancements.Instance.ThrustModulatorFactor : 1f) * num;
    }

    public void StartRecordingInput()
    {
        _currentInput = Vector3.zero;
        SetInputEnabled(false);
    }

    public void StopRecordingInput()
    {
        if (!SELocator.GetAutopilotPanelController().IsAutopilotActive())
        {
            _currentInput = _thrustController._lastTranslationalInput;
            SetInputEnabled(true);
        }
    }

    public void SetInputEnabled(bool state)
    {
        if (state && _currentInput != Vector3.zero)
        {
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

    public override void OnDestroy()
    {
        base.OnDestroy();
        ShipEnhancements.Instance.OnFuelDepleted -= OnFuelDepleted;
        ShipEnhancements.Instance.OnFuelRestored -= OnFuelRestored;
    }
}
