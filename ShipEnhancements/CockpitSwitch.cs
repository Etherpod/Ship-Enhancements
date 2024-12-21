using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEnhancements;

public abstract class CockpitSwitch : ElectricalComponent
{
    [SerializeField]
    protected float _rotationOffset;
    [SerializeField]
    protected InteractReceiver _interactReceiver;
    [SerializeField]
    protected string _label;
    [SerializeField]
    protected OWAudioSource _audioSource;
    [SerializeField]
    protected AudioClip _onAudio;
    [SerializeField]
    protected AudioClip _offAudio;
    [SerializeField]
    protected Light _light;

    protected Quaternion _initialRotation;
    protected OWRenderer _renderer;
    protected CockpitButtonPanel _buttonPanel;
    protected bool _on = false;
    protected ElectricalSystem _electricalSystem;
    protected bool _wasDisrupted = false;
    protected float _baseLightIntensity;
    protected bool _enabledInShip;

    public override void Awake()
    {
        base.Awake();
        _interactReceiver.OnPressInteract += FlipSwitch;
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;

        _buttonPanel = GetComponentInParent<CockpitButtonPanel>();
        _renderer = GetComponent<OWRenderer>();

        if (ShipEnhancements.InMultiplayer && _enabledInShip)
        {
            ShipEnhancements.QSBCompat.AddActiveSwitch(this);
        }
    }

    protected virtual void Start()
    {
        if (!_enabledInShip)
        {
            enabled = false;
            return;
        }

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _interactReceiver.ChangePrompt("Turn on " + _label);
        transform.localRotation = Quaternion.Euler(_initialRotation.eulerAngles.x + _rotationOffset,
            _initialRotation.eulerAngles.y, _initialRotation.eulerAngles.z);
        _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 0f);
        _baseLightIntensity = _light.intensity;
        _light.intensity = 0f;

        _electricalSystem = SELocator.GetShipBody().transform
            .Find("Module_Cockpit/Systems_Cockpit/FlightControlsElectricalSystem")
            .GetComponent<ElectricalSystem>();
        List<ElectricalComponent> componentList = [.. _electricalSystem._connectedComponents];
        componentList.Add(this);
        _electricalSystem._connectedComponents = [.. componentList];
    }

    protected void Update()
    {
        if (_wasDisrupted != _electricalSystem.IsDisrupted())
        {
            _wasDisrupted = _electricalSystem.IsDisrupted();
            _interactReceiver.SetInteractionEnabled(!_wasDisrupted);
        }
    }

    private void FlipSwitch()
    {
        ChangeSwitchState(!_on);

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendSwitchState(id, (GetType().Name, _on));
            }
        }
    }

    public void ChangeSwitchState(bool state)
    {
        _on = state;
        if (_on)
        {
            transform.localRotation = Quaternion.Euler(_initialRotation.eulerAngles.x - _rotationOffset,
                _initialRotation.eulerAngles.y, _initialRotation.eulerAngles.z);
            _interactReceiver.ChangePrompt("Turn off " + _label);
            _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 1f);
            if (_onAudio)
            {
                PlaySwitchAudio(_onAudio);
            }
            _light.intensity = _baseLightIntensity;
        }
        else
        {
            transform.localRotation = Quaternion.Euler(_initialRotation.eulerAngles.x + _rotationOffset,
                _initialRotation.eulerAngles.y, _initialRotation.eulerAngles.z);
            _interactReceiver.ChangePrompt("Turn on " + _label);
            _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 0f);
            if (_offAudio)
            {
                PlaySwitchAudio(_offAudio);
            }
            _light.intensity = 0f;
        }

        OnFlipSwitch(_on);
    }

    private void PlaySwitchAudio(AudioClip clip)
    {
        _audioSource.pitch = Random.Range(0.9f, 1.1f);
        _audioSource.PlayOneShot(clip);
    }

    public override void SetPowered(bool powered)
    {
        if (!_electricalSystem.IsDisrupted())
        {
            base.SetPowered(powered);
            if (powered)
            {
                _interactReceiver.EnableInteraction();
                if (_on)
                {
                    _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 1f);
                    _light.intensity = _baseLightIntensity;
                }
            }
            else
            {
                if (_on)
                {
                    _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 0f);
                    _light.intensity = 0f;
                }
                _interactReceiver.DisableInteraction();
            }
        }
        else if (_on)
        {
            if (powered)
            {
                _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 1f);
                _light.intensity = _baseLightIntensity;
            }
            else
            {
                _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 0f);
                _light.intensity = 0f;
            }
        }
    }

    protected virtual void OnFlipSwitch(bool state) { }

    public bool IsOn()
    {
        return _on;
    }

    private void OnGainFocus()
    {
        SELocator.GetFlightConsoleInteractController().AddInteractible();
    }

    private void OnLoseFocus()
    {
        SELocator.GetFlightConsoleInteractController().RemoveInteractible();
    }

    private void OnShipSystemFailure()
    {
        enabled = false;
        _interactReceiver.DisableInteraction();
    }

    protected void OnDestroy()
    {
        _interactReceiver.OnPressInteract -= FlipSwitch;
        _interactReceiver.OnGainFocus -= OnGainFocus;
        _interactReceiver.OnLoseFocus -= OnLoseFocus;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);

        if (ShipEnhancements.InMultiplayer)
        {
            ShipEnhancements.QSBCompat.RemoveActiveSwitch(this);
        }
    }
}
