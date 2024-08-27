using System;
using UnityEngine;

namespace ShipEnhancements;

public class ShipEngineSwitch : MonoBehaviour
{
    [SerializeField]
    private InteractReceiver _interactReceiver;
    [SerializeField]
    private Transform _switchTransform;
    [SerializeField]
    private float _targetYRotation;

    private CockpitButtonPanel _buttonPanel;
    private bool _turnSwitch = false;
    private float _turnTime = 0.2f;
    private float _turningT;
    private Quaternion _baseRotation;
    private Quaternion _targetRotation;
    private bool _completedTurn = false;

    private void Awake()
    {
        _buttonPanel = GetComponentInParent<CockpitButtonPanel>();

        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;

        _baseRotation = _switchTransform.localRotation;
        _targetRotation = Quaternion.Euler(_switchTransform.localRotation.eulerAngles.x, _targetYRotation, 
            _switchTransform.localRotation.eulerAngles.z);
    }

    private void Start()
    {
        _interactReceiver.SetPromptText(UITextType.HoldPrompt);
        _interactReceiver.ChangePrompt("Start engine");
    }

    private void Update()
    {
        if (_completedTurn)
        {
            GlobalMessenger.FireEvent("StartShipIgnition");
        }
    }

    private void FixedUpdate()
    {
        if (_turnSwitch)
        {
            if (_turningT < 1)
            {
                _turningT += Time.deltaTime / _turnTime;
                float num = Mathf.InverseLerp(0f, 1f, _turningT);
                _switchTransform.localRotation = Quaternion.Lerp(_baseRotation, _targetRotation, num);
            }
            else if (!_completedTurn)
            {
                _completedTurn = true;
            }
        }
        else if (!_turnSwitch)
        {
            if (_turningT > 0)
            {
                _turningT -= Time.deltaTime / _turnTime;
                float num = Mathf.InverseLerp(0f, 1f, _turningT);
                _switchTransform.localRotation = Quaternion.Slerp(_baseRotation, _targetRotation, num);
            }
            else
            {
                _interactReceiver.ResetInteraction();
            }
        }
    }

    private void OnGainFocus()
    {
        _buttonPanel.UpdateFocusedButtons(true);
    }

    private void OnLoseFocus()
    {
        _buttonPanel.UpdateFocusedButtons(false);
    }

    private void OnPressInteract()
    {
        if (_completedTurn)
        {
            _completedTurn = false;
            _turnSwitch = false;
        }
        _turnSwitch = true;
    }

    private void OnReleaseInteract()
    {
        if (!_completedTurn)
        {
            _turnSwitch = false;
            GlobalMessenger.FireEvent("CancelShipIgnition");
        }
    }
}
