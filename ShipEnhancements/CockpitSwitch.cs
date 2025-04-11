using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEnhancements;

public abstract class CockpitSwitch : CockpitInteractible
{
    [SerializeField]
    protected Transform _switchTransform;
    [SerializeField]
    protected string _onLabel;
    [SerializeField]
    protected string _offLabel;
    [SerializeField]
    protected OWEmissiveRenderer _emissiveRenderer;
    [SerializeField]
    [ColorUsage(true, true)]
    protected Color _highlightColor = Color.white;
    [SerializeField]
    protected float _enabledEmissionScale = 1f;
    [SerializeField]
    protected float _disabledEmissionScale = 0f;
    [SerializeField]
    protected Light _switchLight;
    [SerializeField]
    protected Vector3 _rotationOffset;
    [SerializeField]
    protected Vector3 _positionOffset;
    [SerializeField]
    protected OWAudioSource _audioSource;
    [SerializeField]
    protected AudioClip _onAudio;
    [SerializeField]
    protected AudioClip _offAudio;

    protected bool _on = false;
    protected Vector3 _initialPosition;
    protected Quaternion _initialRotation;
    protected float _baseLightIntensity;

    protected virtual void Start()
    {
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _initialPosition = _switchTransform.localPosition;
        _initialRotation = _switchTransform.localRotation;
        _baseLightIntensity = _switchLight.intensity;

        _interactReceiver.ChangePrompt(_onLabel);
        _emissiveRenderer.SetEmissiveScale(0f);
        _switchLight.intensity = 0f;

        AddToElectricalSystem();
    }

    protected override void OnPressInteract()
    {
        SetState(!_on);

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendSwitchState(id, this, _on);
            }
        }
    }

    protected override void OnGainFocus()
    {
        base.OnGainFocus();
        if (!_electricalDisrupted && _powered)
        {
            _emissiveRenderer.SetEmissiveScale(0.5f);
        }
    }

    protected override void OnLoseFocus()
    {
        base.OnLoseFocus();
        if (!_electricalDisrupted && _powered)
        {
            _emissiveRenderer.SetEmissiveScale(_on ? _enabledEmissionScale : _disabledEmissionScale);
        }
    }

    public void SetState(bool state)
    {
        _on = state;

        if (!gameObject.activeInHierarchy) return;

        if (_on)
        {
            _switchTransform.localPosition = _initialPosition + _positionOffset;
            _switchTransform.localRotation = Quaternion.Euler(_initialRotation.eulerAngles + _rotationOffset);
            _interactReceiver.ChangePrompt(_offLabel);
            _emissiveRenderer.SetEmissiveScale(1f);
            _switchLight.intensity = _baseLightIntensity;
            if (_onAudio)
            {
                PlaySwitchAudio(_onAudio);
            }
        }
        else
        {
            _switchTransform.localPosition = _initialPosition;
            _switchTransform.localRotation = _initialRotation;
            _interactReceiver.ChangePrompt(_onLabel);
            _emissiveRenderer.SetEmissiveScale(0f);
            _switchLight.intensity = 0f;
            if (_offAudio)
            {
                PlaySwitchAudio(_offAudio);
            }
        }

        OnChangeState();
    }

    private void PlaySwitchAudio(AudioClip clip)
    {
        _audioSource.pitch = Random.Range(0.95f, 1.05f);
        _audioSource.PlayOneShot(clip, 0.5f);
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);

        if (powered)
        {
            _emissiveRenderer.SetEmissiveScale(_on ? 1f : 0f);
            _switchLight.intensity = _on ? _baseLightIntensity : 0f;
        }
        else
        {
            _emissiveRenderer.SetEmissiveScale(0f);
            _switchLight.intensity = 0f;
        }

        _interactReceiver.SetInteractionEnabled(_electricalDisrupted ? _lastPoweredState : powered);
    }

    protected virtual void OnChangeState() { }

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

        ShipEnhancements.WriteDebugMessage("destroy switch");
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
