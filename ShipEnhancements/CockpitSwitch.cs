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

    protected Quaternion _initialRotation;
    protected OWRenderer _renderer;
    protected bool _on = false;

    private void Start()
    {
        _interactReceiver.OnPressInteract += FlipSwitch;

        _interactReceiver._screenPrompt._text = "Turn on " + _label;
        transform.localRotation = Quaternion.Euler(_initialRotation.eulerAngles.x + _rotationOffset,
            _initialRotation.eulerAngles.y, _initialRotation.eulerAngles.z);
        _renderer = GetComponent<OWRenderer>();
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
            _interactReceiver._screenPrompt._text = "Turn off " + _label;
            _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 1f);
        }
        else
        {
            transform.localRotation = Quaternion.Euler(_initialRotation.eulerAngles.x + _rotationOffset,
                _initialRotation.eulerAngles.y, _initialRotation.eulerAngles.z);
            _interactReceiver._screenPrompt._text = "Turn on " + _label;
            _renderer.SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 0f);
        }

        OnFlipSwitch(_on);
        Locator.GetPromptManager().UpdateText(_interactReceiver._screenPrompt, _interactReceiver._screenPrompt._text);
        _interactReceiver.ResetInteraction();
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
}
