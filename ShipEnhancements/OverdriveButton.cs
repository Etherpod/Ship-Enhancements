﻿using UnityEngine;

namespace ShipEnhancements;

public class OverdriveButton : MonoBehaviour
{
    [SerializeField]
    [ColorUsage(true, true)]
    private Color _inactiveColor = Color.black;
    [SerializeField]
    [ColorUsage(true, true)]
    private Color _offColor = Color.red;
    [SerializeField]
    [ColorUsage(true, true)]
    private Color _onColor = Color.green;
    [SerializeField]
    private bool _isPrimeButton;
    [SerializeField]
    private OWEmissiveRenderer _emissiveRenderer;
    [SerializeField]
    private Transform _buttonTransform;
    [SerializeField]
    private AudioClip _buttonPressedAudio;
    [SerializeField]
    private AudioClip _buttonReleasedAudio;
    [SerializeField]
    private Light _light;

    private InteractReceiver _interactReceiver;
    private ShipOverdriveController _overdriveController;
    private bool _active;
    private bool _on;
    private bool _powered;
    private float _depressionDistance = 0.00521f;
    private bool _pressed;

    private void Awake()
    {
        _interactReceiver = GetComponent<InteractReceiver>();
        _overdriveController = GetComponentInParent<ShipOverdriveController>();
    }

    private void Start()
    {
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;

        _interactReceiver.ChangePrompt(_isPrimeButton ? "Disable safeties" : "Activate Overdrive");
        _light.color = _inactiveColor;
    }

    private void OnPressInteract()
    {
        PressButton();

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendOverdriveButtonState(id, _isPrimeButton, true);
            }
        }
    }

    public void PressButton()
    {
        _pressed = true;
        if (!_active || !_powered) return;

        _on = !_on;
        _emissiveRenderer.SetEmissionColor(_on ? _onColor : _offColor);
        _light.color = _on ? _onColor : _offColor;
        if (_buttonPressedAudio)
        {
            _overdriveController.PlayButtonAudio(_buttonPressedAudio, 0.3f);
        }
        _buttonTransform.localPosition -= new Vector3(0f, _depressionDistance, 0f);
        _overdriveController.OnPressInteract(_isPrimeButton, _on);
    }

    private void OnReleaseInteract()
    {
        ReleaseButton();

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendOverdriveButtonState(id, _isPrimeButton, false);
            }
        }
    }

    public void ReleaseButton()
    {
        _pressed = false;
        _buttonTransform.localPosition += new Vector3(0f, _depressionDistance, 0f);
        if (!_isPrimeButton)
        {
            _interactReceiver.DisableInteraction();
        }
        else
        {
            _interactReceiver.ChangePrompt(_on ? "Enable safeties" : "Disable safeties");
        }
        if (_buttonReleasedAudio)
        {
            _overdriveController.PlayButtonAudio(_buttonReleasedAudio, 0.3f);
        }
    }

    private void OnGainFocus()
    {
        _overdriveController.UpdateFocusedButtons(true);
    }

    private void OnLoseFocus()
    {
        _overdriveController.UpdateFocusedButtons(false);
        if (_pressed)
        {
            OnReleaseInteract();
        }
    }

    public bool IsOn()
    {
        return _active && _on;
    }

    public bool IsActive()
    {
        return _active;
    }

    public void OnDisruptedEvent(bool disrupted)
    {
        if (disrupted)
        {
            _interactReceiver.DisableInteraction();
        }
        else
        {
            if (_active && _powered && (_isPrimeButton || !_on))
            {
                _interactReceiver.EnableInteraction();
            }
        }
    }

    public void SetButtonActive(bool active)
    {
        _active = active;
        if (!_powered) return;
        if (active)
        {
            _emissiveRenderer.SetEmissionColor(_on ? _onColor : _offColor);
            _light.color = _on ? _onColor : _offColor;
            _interactReceiver.EnableInteraction();
        }
        else
        {
            _emissiveRenderer.SetEmissionColor(_inactiveColor);
            _light.color = _inactiveColor;
            if (_pressed)
            {
                OnReleaseInteract();
            }
            _interactReceiver.DisableInteraction();
            _on = false;
            if (_isPrimeButton)
            {
                _interactReceiver.ChangePrompt("Disable safeties");
            }
        }
    }

    public void SetButtonOn(bool on)
    {
        _active = true;
        _on = on;
        if (_isPrimeButton)
        {
            _interactReceiver.ChangePrompt(_on ? "Enable safeties" : "Disable safeties");
        }
        if (!_powered) return;
        _emissiveRenderer.SetEmissionColor(_on ? _onColor : _offColor);
        _light.color = _on ? _onColor : _offColor;
        if (_pressed)
        {
            OnReleaseInteract();
        }
    }

    public void SetPowered(bool powered, bool disrupted)
    {
        _powered = disrupted ? _powered : powered;
        if (powered)
        {
            if (_active)
            {
                _emissiveRenderer.SetEmissionColor(_on ? _onColor : _offColor);
                _light.color = _on ? _onColor : _offColor;
                if ((_isPrimeButton || !_on) && !disrupted)
                {
                    _interactReceiver.EnableInteraction();
                }
            }
            else
            {
                _emissiveRenderer.SetEmissionColor(_inactiveColor);
                _light.color = _inactiveColor;
            }
        }
        else
        {
            _emissiveRenderer.SetEmissionColor(_inactiveColor);
            _light.color = _inactiveColor;
            if (!disrupted)
            {
                if (_pressed)
                {
                    OnReleaseInteract();
                }
                if (!_isPrimeButton)
                {
                    _on = false;
                }
                _interactReceiver.DisableInteraction();
            }
        }
    }

    private void OnDestroy()
    {
        _interactReceiver.OnPressInteract -= OnPressInteract;
        _interactReceiver.OnReleaseInteract -= OnReleaseInteract;
        _interactReceiver.OnGainFocus -= OnGainFocus;
        _interactReceiver.OnLoseFocus -= OnLoseFocus;
    }
}
