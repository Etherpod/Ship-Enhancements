﻿using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipPersistentInput : ThrusterController
{
    private RulesetDetector _rulesetDetector;
    private ShipThrusterController _thrustController;
    private Vector3 _currentInput;

    public override void Awake()
    {
        base.Awake();
        _rulesetDetector = SELocator.GetShipDetector().GetComponent<RulesetDetector>();
        _thrustController = GetComponent<ShipThrusterController>();
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

        float num = Mathf.Min(_rulesetDetector.GetThrustLimit(), _thrustController._thrusterModel.GetMaxTranslationalThrust()) 
            / _thrustController._thrusterModel.GetMaxTranslationalThrust();
        return _currentInput * ((bool)enableThrustModulator.GetProperty() ? ShipEnhancements.Instance.ThrustModulatorFactor : 1f) * num;
    }

    public void ResetInput()
    {
        _currentInput = Vector3.zero;
        SetInputEnabled(false);
    }

    public void ReadNextInput()
    {
        if (!SELocator.GetAutopilotPanelController().IsAutopilotActive())
        {
            _currentInput = _thrustController._lastTranslationalInput;
            SetInputEnabled(true);

            if (ShipEnhancements.InMultiplayer)
            {
                foreach (uint id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendPersistentInput(id, _currentInput);
                }
            }
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
        _currentInput = newInput;
        SetInputEnabled(SELocator.GetAutopilotPanelController().IsHoldInputSelected()
            && !SELocator.GetShipDamageController().IsElectricalFailed()
            && SELocator.GetShipResources().AreThrustersUsable()
            && !SELocator.GetAutopilotPanelController().IsAutopilotDamaged());
    }
}
