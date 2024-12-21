using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipPersistentInput : ThrusterController
{
    private Vector3 _currentInput;
    private bool _inputEnabled;
    private ThrustAndAttitudeIndicator _thrustDisplay;
    private MeshRenderer[] _displayRenderers;
    private float[] _lastRendererValues = new float[6];
    private RulesetDetector _rulesetDetector;
    private Autopilot _shipAutopilot;
    private bool _lastAutopilotState;
    private ShipThrusterController _thrustController;
    private bool _fuelDepleted;

    private void Start()
    {
        _thrustDisplay = GetComponentInChildren<ThrustAndAttitudeIndicator>(true);
        _rulesetDetector = SELocator.GetShipDetector().GetComponent<RulesetDetector>();
        _shipAutopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
        _thrustController = GetComponent<ShipThrusterController>();
        _displayRenderers =
        [
            _thrustDisplay._rendererForward,
            _thrustDisplay._rendererBack,
            _thrustDisplay._rendererRight,
            _thrustDisplay._rendererLeft,
            _thrustDisplay._rendererUp,
            _thrustDisplay._rendererDown,

        ];
        GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
        _shipAutopilot.OnInitMatchVelocity += OnInitMatchVelocity;
        _lastAutopilotState = IsAutopilotEnabled();
        enabled = false;
    }

    public override Vector3 ReadRotationalInput()
    {
        return Vector3.zero;
    }

    public override Vector3 ReadTranslationalInput()
    {
        float num = Mathf.Min(_rulesetDetector.GetThrustLimit(), _thrustController._thrusterModel.GetMaxTranslationalThrust()) 
            / _thrustController._thrusterModel.GetMaxTranslationalThrust();
        return _currentInput * ((bool)enableThrustModulator.GetProperty() ? ShipEnhancements.Instance.ThrustModulatorLevel / 5f : 1f) * num;
    }

    private void OnEnterFlightConsole(OWRigidbody shipBody)
    {
        _currentInput = Vector3.zero;
        for (int i = 0; i < _lastRendererValues.Length; i++)
        {
            _lastRendererValues[i] = 0f;
        }
        enabled = false;
    }

    private void OnExitFlightConsole()
    {
        if (!_inputEnabled || _fuelDepleted) return;
        _currentInput = GetComponent<ShipThrusterController>()._lastTranslationalInput;
        if (_currentInput != Vector3.zero && !IsAutopilotEnabled() && !_thrustController.RequiresIgnition())
        {
            if (ShipEnhancements.InMultiplayer && !ShipEnhancements.QSBAPI.GetIsHost())
            {
                foreach (uint id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendInitialPersistentInput(id, _currentInput);
                }
                _currentInput = Vector3.zero;
            }
            else
            {
                enabled = true;
            }
        }
    }

    public void SetInputEnabled(bool setEnabled)
    {
        _inputEnabled = setEnabled;
        if (!setEnabled && _currentInput != Vector3.zero && enabled)
        {
            enabled = false;
            _thrustDisplay._thrusterArrowRoot.gameObject.SetActive(false);
            ShipEnhancements.WriteDebugMessage("Save");
            for (int i = 0; i < _displayRenderers.Length; i++)
            {
                _lastRendererValues[i] = _displayRenderers[i].material.GetFloat(_thrustDisplay._propID_BarPosition);
                _displayRenderers[i].material.SetFloat(_thrustDisplay._propID_BarPosition, 0f);
            }
        }
        else if (setEnabled && _currentInput != Vector3.zero && !enabled && !_fuelDepleted)
        {
            enabled = true;
            _thrustDisplay._thrusterArrowRoot.gameObject.SetActive(true);
            ShipEnhancements.WriteDebugMessage("Load: " + _lastRendererValues[0]);
            for (int i = 0; i < _displayRenderers.Length; i++)
            {
                _displayRenderers[i].material.SetFloat(_thrustDisplay._propID_BarPosition, _lastRendererValues[i]);
                _lastRendererValues[i] = 0f;
            }
        }
    }

    public void OnDisableEngine()
    {
        for (int i = 0; i < _lastRendererValues.Length; i++)
        {
            _lastRendererValues[i] = 0f;
        }

        if (_currentInput != Vector3.zero && enabled)
        {
            enabled = false;
            _thrustDisplay._thrusterArrowRoot.gameObject.SetActive(false);
            for (int i = 0; i < _displayRenderers.Length; i++)
            {
                _displayRenderers[i].material.SetFloat(_thrustDisplay._propID_BarPosition, 0f);
            }
        }

        _currentInput = Vector3.zero;
    }

    public void UpdateLastAutopilotState()
    {
        _lastAutopilotState = IsAutopilotEnabled();
    }

    private void OnInitMatchVelocity()
    {
        if (enabled)
        {
            _currentInput = Vector3.zero;
            for (int i = 0; i < _lastRendererValues.Length; i++)
            {
                _lastRendererValues[i] = 0f;
            }
            enabled = false;
        }
    }

    private bool IsAutopilotEnabled()
    {
        return _shipAutopilot.IsMatchingVelocity() || _shipAutopilot.IsFlyingToDestination() 
            || _shipAutopilot.IsApproachingDestination() || _shipAutopilot.IsLiningUpDestination();
    }

    public bool InputEnabled()
    {
        return _inputEnabled && !_lastAutopilotState && ShipEnhancements.Instance.engineOn;
    }

    public void SetInputRemote(Vector3 newInput)
    {
        if (!_inputEnabled || _fuelDepleted) return;
        _currentInput = newInput;
        if (_currentInput != Vector3.zero && !IsAutopilotEnabled() && !_thrustController.RequiresIgnition())
        {
            enabled = true;
        }
    }

    private void OnFuelDepleted()
    {
        _fuelDepleted = true;
        OnDisableEngine();
    }

    private void OnFuelRestored()
    {
        _fuelDepleted = false;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
    }
}
