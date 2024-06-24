using System;
using UnityEngine;

namespace ShipEnhancements;

public class CockpitSwitch : MonoBehaviour
{
    [SerializeField]
    private float _rotationOffset;
    [SerializeField]
    private InteractReceiver _interactReceiver;
    [SerializeField]
    private string _label;

    private Quaternion _initialRotation;
    private bool _on = false;

    protected virtual void Awake() { }

    private void Start()
    {
        _interactReceiver.OnPressInteract += FlipSwitch;

        _interactReceiver._screenPrompt._text = "Turn on " + _label;
        transform.localRotation = Quaternion.Euler(_initialRotation.eulerAngles.x + _rotationOffset,
            _initialRotation.eulerAngles.y, _initialRotation.eulerAngles.z);
        GetComponent<OWRenderer>().SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 0f);
    }

    private void FlipSwitch()
    {
        _on = !_on;
        if (_on)
        {
            transform.localRotation = Quaternion.Euler(_initialRotation.eulerAngles.x - _rotationOffset,
                _initialRotation.eulerAngles.y, _initialRotation.eulerAngles.z);
            _interactReceiver._screenPrompt._text = "Turn off " + _label;
            GetComponent<OWRenderer>().SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 1f);
        }
        else
        {
            transform.localRotation = Quaternion.Euler(_initialRotation.eulerAngles.x + _rotationOffset,
                _initialRotation.eulerAngles.y, _initialRotation.eulerAngles.z);
            _interactReceiver._screenPrompt._text = "Turn on " + _label;
            GetComponent<OWRenderer>().SetMaterialProperty(Shader.PropertyToID("_LightIntensity"), 0f);
        }

        OnFlipSwitch(_on);
        Locator.GetPromptManager().UpdateText(_interactReceiver._screenPrompt, _interactReceiver._screenPrompt._text);
        _interactReceiver.ResetInteraction();
    }

    protected virtual void OnFlipSwitch(bool state) { }
}
