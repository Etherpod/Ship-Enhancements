using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class ShipPersistentInput : ThrusterController
{
    private Vector3 _currentInput;
    private bool _inputEnabled;
    private ThrustAndAttitudeIndicator _thrustDisplay;
    private MeshRenderer[] _displayRenderers;
    private float[] _lastRendererValues = new float[6];
    private RulesetDetector _rulesetDetector;

    private void Start()
    {
        _thrustDisplay = GetComponentInChildren<ThrustAndAttitudeIndicator>(true);
        _rulesetDetector = Locator.GetShipDetector().GetComponent<RulesetDetector>();
        _displayRenderers =
        [
            _thrustDisplay._rendererForward,
            _thrustDisplay._rendererBack,
            _thrustDisplay._rendererRight,
            _thrustDisplay._rendererLeft,
            _thrustDisplay._rendererUp,
            _thrustDisplay._rendererDown,

        ];
        ShipEnhancements.WriteDebugMessage(_thrustDisplay);
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
        float num = Mathf.Min(_rulesetDetector.GetThrustLimit(), 1);
        return _currentInput * ShipEnhancements.Instance.thrustModulatorLevel / 5f * num;
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
        if (!_inputEnabled) return;
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

    public void SetInputEnabled(bool flag)
    {
        _inputEnabled = flag;
        if (!flag && _currentInput != Vector3.zero && enabled)
        {
            enabled = false;
            _thrustDisplay._thrusterArrowRoot.gameObject.SetActive(false);
            for (int i = 0; i < _displayRenderers.Length; i++)
            {
                _lastRendererValues[i] = _displayRenderers[i].material.GetFloat(_thrustDisplay._propID_BarPosition);
                _displayRenderers[i].material.SetFloat(_thrustDisplay._propID_BarPosition, 0f);
            }
        }
        else if (flag && _currentInput != Vector3.zero && !enabled)
        {
            enabled = true;
            _thrustDisplay._thrusterArrowRoot.gameObject.SetActive(true);
            for (int i = 0; i < _displayRenderers.Length; i++)
            {
                _displayRenderers[i].material.SetFloat(_thrustDisplay._propID_BarPosition, _lastRendererValues[i]);
                _lastRendererValues[i] = 0f;
            }
        }
    }

    public bool InputEnabled()
    {
        return _inputEnabled;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
    }
}
