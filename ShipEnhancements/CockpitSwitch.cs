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

    protected Quaternion _initialRotation;
    protected OWRenderer _renderer;
    protected bool _on = false;

    private void Start()
    {
        _renderer = GetComponent<OWRenderer>();

        _interactReceiver.OnPressInteract += FlipSwitch;

        _interactReceiver.ChangePrompt("Turn on " + _label);
        transform.localRotation = Quaternion.Euler(_initialRotation.eulerAngles.x + _rotationOffset,
            _initialRotation.eulerAngles.y, _initialRotation.eulerAngles.z);
        _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 0f);

        ElectricalSystem cockpitElectricalSystem = Locator.GetShipBody().transform
            .Find("Module_Cockpit/Systems_Cockpit/FlightControlsElectricalSystem")
            .GetComponent<ElectricalSystem>();
        List<ElectricalComponent> componentList = cockpitElectricalSystem._connectedComponents.ToList();
        componentList.Add(this);
        cockpitElectricalSystem._connectedComponents = componentList.ToArray();
    }

    private void FlipSwitch()
    {
        _on = !_on;
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
        base.SetPowered(powered);
        if (powered)
        {
            _interactReceiver.EnableInteraction();
            if (_on)
            {
                _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 1f);
            }
        }
        else
        {
            if (_on)
            {
                _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 0f);
            }
            _interactReceiver.DisableInteraction();
        }
    }

    protected virtual void OnFlipSwitch(bool state) { }

    protected void OnDestroy()
    {
        _interactReceiver.OnPressInteract -= FlipSwitch;
    }
}
