using UnityEngine;

namespace ShipEnhancements;

public class ReactorOverloader : MonoBehaviour
{
    private InteractReceiver _interactReceiver;
    private ReactorHeatController _reactorHeat;
    private ShipTemperatureDetector _temperatureDetector;
    private bool _focused = false;
    private bool _overloaded = false;

    private void Start()
    {
        _reactorHeat = SELocator.GetShipDamageController()._shipReactorComponent.GetComponent<ReactorHeatController>();
        _temperatureDetector = SELocator.GetShipTemperatureDetector();
        _interactReceiver = GetComponent<InteractReceiver>();
        _interactReceiver.ChangePrompt(UITextLibrary.GetString(UITextType.HoldPrompt) + " Overload Reactor");
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
    }

    private void Update()
    {
        if (_focused)
        {
            if (!_overloaded && OWInput.IsPressed(InputLibrary.interact, InputMode.Character, 1f))
            {
                _interactReceiver.ChangePrompt("Reset Reactor");
                _interactReceiver.ResetInteraction();
                _overloaded = true;
            }
            else if (_overloaded && OWInput.IsNewlyPressed(InputLibrary.interact, InputMode.Character))
            {
                _interactReceiver.ChangePrompt(UITextLibrary.GetString(UITextType.HoldPrompt) + " Overload Reactor");
                _interactReceiver.ResetInteraction();
                _reactorHeat.SetOverloadHeat(0f);
                _overloaded = false;
            }
        }

        if (_overloaded)
        {
            float tempLerp = _temperatureDetector.GetTemperatureRatio() * -1f;
            float diffLerp = (float)ShipEnhancements.Settings.temperatureDifficulty.GetProperty();
            float heat = Mathf.Lerp(0f, 0.8f, tempLerp * diffLerp);
            _reactorHeat.SetOverloadHeat(heat);
        }
    }

    private void OnGainFocus()
    {
        _focused = true;
    }

    private void OnLoseFocus()
    {
        _focused = false;
    }

    private void OnShipSystemFailure()
    {
        enabled = false;
    }

    private void OnDestroy()
    {
        _interactReceiver.OnGainFocus -= OnGainFocus;
        _interactReceiver.OnLoseFocus -= OnLoseFocus;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
