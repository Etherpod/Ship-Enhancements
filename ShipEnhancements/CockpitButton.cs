using UnityEngine;

namespace ShipEnhancements;

public class CockpitButton : CockpitInteractible
{
    [SerializeField]
    protected Transform _buttonTransform;
    [SerializeField]
    protected string _onLabel;
    [SerializeField]
    protected string _offLabel;
    [SerializeField]
    protected OWEmissiveRenderer _emissiveRenderer;
    [SerializeField]
    protected Light _buttonLight;
    [SerializeField]
    protected Vector3 _rotationOffset;
    [SerializeField]
    protected Vector3 _positionOffset;
    [SerializeField]
    protected OWAudioSource _audioSource;
    [SerializeField]
    protected AudioClip _pressAudio;
    [SerializeField]
    protected AudioClip _releaseAudio;

    public delegate void CockpitButtonEvent(bool value);
    public event CockpitButtonEvent OnChangeState;

    protected bool _pressed;
    protected bool _on;
    protected Vector3 _initialButtonPosition;
    protected Quaternion _initialButtonRotation;
    protected float _baseLightIntensity;
    protected float _disabledEmissionLevel = 0.2f;
    protected float _enabledEmissionLevel = 1f;

    protected virtual void Start()
    {
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _initialButtonPosition = _buttonTransform.localPosition;
        _initialButtonRotation = _buttonTransform.localRotation;
        _baseLightIntensity = _buttonLight.intensity;

        _interactReceiver.ChangePrompt(_onLabel);
        _emissiveRenderer.SetEmissiveScale(0f);
        _buttonLight.intensity = 0f;

        AddToElectricalSystem();
    }

    protected override void OnPressInteract()
    {
        _pressed = true;
        _buttonTransform.localPosition = _initialButtonPosition + _positionOffset;
        _buttonTransform.localRotation = Quaternion.Euler(_initialButtonRotation.eulerAngles + _rotationOffset);
        if (_pressAudio)
        {
            _audioSource.PlayOneShot(_pressAudio, 0.5f);
        }

        SetState(!_on);
        RaiseChangeStateEvent();
        OnChangeStateEvent();
    }

    protected override void OnReleaseInteract()
    {
        _pressed = false;
        _buttonTransform.localPosition = _initialButtonPosition;
        _buttonTransform.localRotation = _initialButtonRotation;
        _interactReceiver.ChangePrompt(_on ? _offLabel : _onLabel);
        if (_releaseAudio)
        {
            _audioSource.PlayOneShot(_releaseAudio, 0.5f);
        }
    }

    protected override void OnLoseFocus()
    {
        base.OnLoseFocus();
        if (_pressed)
        {
            OnReleaseInteract();
        }
    }

    public virtual void SetState(bool state)
    {
        _on = state;

        if (_on)
        {
            _emissiveRenderer.SetEmissiveScale(_enabledEmissionLevel);
            _buttonLight.intensity = _baseLightIntensity;
        }
        else
        {
            _emissiveRenderer.SetEmissiveScale(_disabledEmissionLevel);
            _buttonLight.intensity = 0f;
        }

        if (!_pressed)
        {
            _interactReceiver.ChangePrompt(_on ? _offLabel : _onLabel);
        }
    }

    public virtual void OnChangeStateEvent() { }

    public void RaiseChangeStateEvent()
    {
        OnChangeState?.Invoke(_on);
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);

        if (powered)
        {
            _emissiveRenderer.SetEmissiveScale(_on ? _enabledEmissionLevel : _disabledEmissionLevel);
            _buttonLight.intensity = _on ? _baseLightIntensity : 0f;
        }
        else
        {
            _emissiveRenderer.SetEmissiveScale(0f);
            _buttonLight.intensity = 0f;
        }

        _interactReceiver.SetInteractionEnabled(_electricalDisrupted ? _lastPoweredState : powered);
    }

    public virtual bool IsOn()
    {
        return _on;
    }

    private void OnShipSystemFailure()
    {
        enabled = false;
        _interactReceiver.DisableInteraction();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
