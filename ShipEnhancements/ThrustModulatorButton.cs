﻿using UnityEngine;

namespace ShipEnhancements;

public class ThrustModulatorButton : CockpitInteractible
{
    [SerializeField]
    private int _modulatorLevel;
    [SerializeField]
    private AudioClip _buttonPressedAudio;
    [SerializeField]
    private AudioClip _buttonReleasedAudio;
    [SerializeField]
    private Light _light;

    private ThrustModulatorController _modulatorController;
    private OWEmissiveRenderer _emissiveRenderer;
    private bool _active;
    private float _fadeTime = 0.1f;
    private float _fadeStartTime;
    private bool _fading = false;
    private float _lastEmissiveScale = 0f;
    private float _depressionDistance = 0.0054f;
    private bool _pressed = false;
    private float _baseLightIntensity;
    private Color _baseLightColor;
    private bool _resetWhenOff = false;

    public override void Awake()
    {
        base.Awake();
        _modulatorController = GetComponentInParent<ThrustModulatorController>();
        _emissiveRenderer = GetComponent<OWEmissiveRenderer>();
    }

    private void Start()
    {
        _interactReceiver._screenPrompt._text = "Set Modulator to level " + _modulatorLevel;
        _baseLightIntensity = _light.intensity;
        _baseLightColor = _light.color;

        SetButtonLight(true);
    }

    private void Update()
    {
        if (_fading)
        {
            float num = Mathf.InverseLerp(_fadeStartTime, _fadeStartTime + _fadeTime, Time.time);
            if (_active)
            {
                _emissiveRenderer.SetEmissiveScale(Mathf.Lerp(_lastEmissiveScale, 1f, num));
                _light.intensity = Mathf.Lerp(_lastEmissiveScale, 1f, num);
            }
            else
            {
                _emissiveRenderer.SetEmissiveScale(Mathf.Lerp(_lastEmissiveScale, 0f, num));
                _light.intensity = Mathf.Lerp(_lastEmissiveScale, 0f, num);
            }

            if (num >= 1)
            {
                _fading = false;
                if (_resetWhenOff && !_active)
                {
                    _resetWhenOff = false;
                    ResetButtonColor();
                }
            }
        }
    }

    protected override void OnPressInteract()
    {
        PressButton();

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendModulatorButtonState(id, _modulatorLevel, true);
            }
        }
    }

    public void PressButton()
    {
        _pressed = true;
        if (_buttonPressedAudio)
        {
            _modulatorController.PlayButtonSound(_buttonPressedAudio, 0.3f, _modulatorLevel);
        }
        ShipEnhancements.Instance.SetThrustModulatorLevel(_modulatorLevel);
        _modulatorController.UpdateModulatorDisplay(_modulatorLevel, false);
        transform.localPosition -= new Vector3(0f, _depressionDistance, 0f);
    }

    protected override void OnReleaseInteract()
    {
        ReleaseButton();

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendModulatorButtonState(id, _modulatorLevel, false);
            }
        }
    }

    public void ReleaseButton()
    {
        _pressed = false;
        if (_buttonReleasedAudio)
        {
            _modulatorController.PlayButtonSound(_buttonReleasedAudio, 0.3f, _modulatorLevel);
        }
        transform.localPosition += new Vector3(0f, _depressionDistance, 0f);
        _interactReceiver.DisableInteraction();
    }

    protected override void OnGainFocus()
    {
        base.OnGainFocus();
        _emissiveRenderer.SetEmissiveScale(0.5f);
    }

    protected override void OnLoseFocus()
    {
        base.OnLoseFocus();
        if (_pressed)
        {
            OnReleaseInteract();
        }
        _emissiveRenderer.SetEmissiveScale(_active ? 1f : 0f);
    }

    public int GetModulatorLevel()
    {
        return _modulatorLevel;
    }

    public void SetButtonLight(bool state, bool instant = false)
    {
        _active = state;
        if (instant)
        {
            _emissiveRenderer.SetEmissiveScale(state ? 1f : 0f);
            _light.intensity = state ? _baseLightIntensity : 0f;
        }
        else
        {
            _fadeStartTime = Time.time;
            _lastEmissiveScale = _emissiveRenderer.GetEmissiveScale();
            _fading = true;
        }
    }

    public void TurnOffAndReset()
    {
        SetButtonLight(false);
        _resetWhenOff = true;
    }

    public void SetInteractable(bool interactable)
    {
        _interactReceiver.SetInteractionEnabled(interactable);
    }

    public void SetButtonColor(Color color)
    {
        _emissiveRenderer.SetEmissionColor(color);
        _light.color = color;
    }

    public void ResetButtonColor()
    {
        _emissiveRenderer.SetEmissionColor(_emissiveRenderer.GetOriginalEmissionColor());
        _emissiveRenderer.SetEmissiveScale(_active ? 1f : 0f);
        _light.color = _baseLightColor;
    }
}
