using UnityEngine;
using System.Collections.Generic;
using static ShipEnhancements.ShipEnhancements.Settings;
using System.Linq;

namespace ShipEnhancements;

public class GravityGearInvertSwitch : CockpitInteractible
{
    [SerializeField]
    private Transform _switchTransform;
    [SerializeField]
    private Transform _switchOffPos;
    [SerializeField]
    private Transform _switchOnPos;
    [SerializeField]
    private OWEmissiveRenderer _emissiveRenderer;
    [SerializeField]
    private Light _light;
    [SerializeField]
    private AudioSource _audioSource;
    [SerializeField]
    private AudioClip _onAudio;
    [SerializeField]
    private AudioClip _offAudio;

    private ElectricalSystem _electricalSystem;
    private bool _on = false;
    private bool _wasDisrupted;
    private float _baseLightIntensity;

    private string _enablePrompt = "Invert Power";
    private string _disablePrompt = "Reset Power";

    private void Start()
    {
        if (!(bool)enableGravityLandingGear.GetProperty())
        {
            enabled = false;
            return;
        }

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _interactReceiver.ChangePrompt(_enablePrompt);
        _switchTransform.localPosition = _switchOffPos.localPosition;
        _emissiveRenderer.SetEmissiveScale(0f);
        _baseLightIntensity = _light.intensity;
        _light.intensity = 0f;

        _electricalSystem = SELocator.GetShipBody().transform
            .Find("Module_Cockpit/Systems_Cockpit/FlightControlsElectricalSystem")
            .GetComponent<ElectricalSystem>();
        List<ElectricalComponent> componentList = [.. _electricalSystem._connectedComponents];
        componentList.Add(this);
        _electricalSystem._connectedComponents = [.. componentList];

        ShipEnhancements.WriteDebugMessage(_electricalSystem._connectedComponents.Contains(this));
    }

    private void Update()
    {
        if (_wasDisrupted != _electricalSystem.IsDisrupted())
        {
            _wasDisrupted = _electricalSystem.IsDisrupted();
            _interactReceiver.SetInteractionEnabled(!_wasDisrupted);
        }
    }

    protected override void OnPressInteract()
    {
        ChangeSwitchState(!_on);

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                //ShipEnhancements.QSBCompat.SendSwitchState(id, (GetType().Name, _on));
            }
        }
    }

    public void ChangeSwitchState(bool state)
    {
        _on = state;
        if (_on)
        {
            _switchTransform.localPosition = _switchOnPos.localPosition;
            _interactReceiver.ChangePrompt(_disablePrompt);
            _emissiveRenderer.SetEmissiveScale(1f);
            if (_onAudio)
            {
                _audioSource.PlayOneShot(_onAudio, 0.5f);
            }
            _light.intensity = _baseLightIntensity;
        }
        else
        {
            _switchTransform.localPosition = _switchOffPos.localPosition;
            _interactReceiver.ChangePrompt(_enablePrompt);
            _emissiveRenderer.SetEmissiveScale(0f);
            if (_offAudio)
            {
                _audioSource.PlayOneShot(_offAudio, 0.5f);
            }
            _light.intensity = 0f;
        }

        ShipEnhancements.Instance.SetGravityLandingGearInverted(_on);
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
                    _emissiveRenderer.SetEmissiveScale(1f);
                    _light.intensity = _baseLightIntensity;
                }
            }
            else
            {
                if (_on)
                {
                    _emissiveRenderer.SetEmissiveScale(0f);
                    _light.intensity = 0f;
                }
                _interactReceiver.DisableInteraction();
            }
        }
        else if (_on)
        {
            if (powered)
            {
                _emissiveRenderer.SetEmissiveScale(1f);
                _light.intensity = _baseLightIntensity;
            }
            else
            {
                _emissiveRenderer.SetEmissiveScale(0f);
                _light.intensity = 0f;
            }
        }
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
}
