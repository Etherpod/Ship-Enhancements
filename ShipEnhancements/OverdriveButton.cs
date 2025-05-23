﻿using UnityEngine;

namespace ShipEnhancements;

public class OverdriveButton : CockpitInteractible
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

    private ShipOverdriveController _overdriveController;
    private bool _active;
    private bool _on;
    private float _depressionDistance = 0.00521f;
    private bool _pressed;
    private float _cooldownT;

    public override void Awake()
    {
        base.Awake();
        _overdriveController = GetComponentInParent<ShipOverdriveController>();
    }

    private void Start()
    {
        _interactReceiver.ChangePrompt(_isPrimeButton ? "Disable safeties" : "Activate Overdrive");
        _light.color = _inactiveColor;

        enabled = false;
    }

    private void Update()
    {
        if (_cooldownT > 0f)
        {
            _cooldownT -= Time.deltaTime;

            if (_cooldownT <= 0f)
            {
                enabled = false;
                _interactReceiver.SetInteractionEnabled(_powered);
                if (!_powered) return;
                if (_isPrimeButton)
                {
                    SetButtonOn(false);
                }
                else
                {
                    SetButtonActive(false);
                }
            }
        }
        else
        {
            enabled = false;
        }
    }

    protected override void OnPressInteract()
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

    protected override void OnReleaseInteract()
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

    protected override void OnGainFocus()
    {
        base.OnGainFocus();
        if (_active && _powered)
        {
            _emissiveRenderer.SetEmissionColor(_active ? (_on ? _onColor : _offColor) * 0.5f : _inactiveColor);
        }
    }

    protected override void OnLoseFocus()
    {
        base.OnLoseFocus();
        if (_pressed)
        {
            OnReleaseInteract();
        }
        if (_active && _powered)
        {
            _emissiveRenderer.SetEmissionColor(_active ? (_on ? _onColor : _offColor) : _inactiveColor);
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

    public void SetButtonActive(bool active)
    {
        _active = active;
        if (!_powered || _cooldownT > 0f) return;
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
        if (!_powered || _cooldownT > 0f) return;
        _emissiveRenderer.SetEmissionColor(_on ? _onColor : _offColor);
        _light.color = _on ? _onColor : _offColor;
        if (_pressed)
        {
            OnReleaseInteract();
        }
    }

    public void SetButtonPowered(bool state, bool disrupted)
    {
        _powered = disrupted ? _powered : state;

        if (!state && _cooldownT > 0f) return;

        if (state)
        {
            if (_active)
            {
                _emissiveRenderer.SetEmissionColor(_on ? _onColor : _offColor);
                _light.color = _on ? _onColor : _offColor;
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
            if (!_isPrimeButton && !disrupted)
            {
                _on = false;
            }
        }

        _interactReceiver.SetInteractionEnabled(_powered);
    }

    public void SetCooldown(float cooldown)
    {
        SetButtonActive(false);
        _interactReceiver.DisableInteraction();

        _cooldownT = cooldown;
        enabled = true;
    }
}
