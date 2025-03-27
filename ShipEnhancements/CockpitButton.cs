using UnityEngine;

namespace ShipEnhancements;

public class CockpitButton : CockpitInteractible
{
    [SerializeField]
    private Transform _buttonTransform;
    [SerializeField]
    private string _onLabel;
    [SerializeField]
    private string _offLabel;
    [SerializeField]
    private OWEmissiveRenderer _emissiveRenderer;
    [SerializeField]
    private Light _buttonLight;
    [SerializeField]
    private Vector3 _pressOffset;
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private AudioClip _pressAudio;
    [SerializeField]
    private AudioClip _releaseAudio;

    protected bool _pressed;
    protected bool _on;
    protected Vector3 _initialButtonPosition;
    protected float _baseLightIntensity;

    protected virtual void Start()
    {
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _interactReceiver.ChangePrompt(_onLabel);
        _initialButtonPosition = _buttonTransform.localPosition;
        _emissiveRenderer.SetEmissiveScale(0f);
        _baseLightIntensity = _buttonLight.intensity;
        _buttonLight.intensity = 0f;

        AddToElectricalSystem();
    }

    protected override void OnPressInteract()
    {
        _pressed = true;
        _buttonTransform.localPosition = _initialButtonPosition + _pressOffset;
        if (_pressAudio)
        {
            _audioSource.PlayOneShot(_pressAudio, 0.5f);
        }

        SetState(!_on);
    }

    protected override void OnReleaseInteract()
    {
        _pressed = false;
        _buttonTransform.localPosition = _initialButtonPosition;
        if (_releaseAudio)
        {
            _audioSource.PlayOneShot(_releaseAudio, 0.5f);
        }
    }

    protected virtual void SetState(bool state)
    {
        _on = state;

        if (_on)
        {
            _interactReceiver.ChangePrompt(_offLabel);
            _emissiveRenderer.SetEmissiveScale(1f);
            _buttonLight.intensity = _baseLightIntensity;
        }
        else
        {
            _interactReceiver.ChangePrompt(_onLabel);
            _emissiveRenderer.SetEmissiveScale(0f);
            _buttonLight.intensity = 0f;
        }
    }

    protected virtual void OnChangeState() { }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);

        if (powered)
        {
            _emissiveRenderer.SetEmissiveScale(_on ? 1f : 0f);
            _buttonLight.intensity = _on ? _baseLightIntensity : 0f;
        }
        else
        {
            _emissiveRenderer.SetEmissiveScale(0f);
            _buttonLight.intensity = 0f;
        }

        _interactReceiver.SetInteractionEnabled(_electricalDisrupted ? _lastPoweredState : powered);
    }

    public bool IsOn()
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
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
