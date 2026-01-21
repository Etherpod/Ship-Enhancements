using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ReactorOverloader : MonoBehaviour
{
    [SerializeField]
    private OWAudioSource _startupSource;
    [SerializeField]
    private OWAudioSource _disableSource;
    [SerializeField]
    private OWAudioSource _loopSource;

    private readonly float _startupLength = 1.2f;

    private InteractReceiver _interactReceiver;
    private ShipReactorComponent _reactor;
    private ReactorHeatController _reactorHeat;
    private ShipTemperatureDetector _temperatureDetector;
    private bool _focused = false;
    private bool _overloaded = false;

    private void Start()
    {
        _reactor = SELocator.GetShipDamageController()._shipReactorComponent;
        _reactorHeat = _reactor.GetComponent<ReactorHeatController>();
        _temperatureDetector = SELocator.GetShipTemperatureDetector();
        _interactReceiver = GetComponent<InteractReceiver>();
        _interactReceiver.ChangePrompt(UITextLibrary.GetString(UITextType.HoldPrompt) + " Overload Reactor");
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        _reactor.OnDamaged += OnReactorDamaged;
        _reactor.OnRepaired += OnReactorRepaired;
    }

    private void Update()
    {
        if (_focused && !_reactor.isDamaged)
        {
            bool was = _overloaded;
            if (!_overloaded && OWInput.IsPressed(InputLibrary.interact, InputMode.Character, _startupLength))
            {
                _interactReceiver.ChangePrompt("Reset Reactor");
                _interactReceiver.ResetInteraction();
                _loopSource.FadeIn(1f);
                _overloaded = true;
            }
            else if (_overloaded && OWInput.IsNewlyPressed(InputLibrary.interact, InputMode.Character))
            {
                _interactReceiver.ChangePrompt(UITextLibrary.GetString(UITextType.HoldPrompt) + " Overload Reactor");
                _interactReceiver.ResetInteraction();
                _reactorHeat.SetOverloadHeat(0f);

                _loopSource.FadeOut(1f);
                _disableSource.time = 0f;
                _disableSource.SetLocalVolume(1f);
                _disableSource.Play();

                _overloaded = false;
            }

            if (was != _overloaded && ShipEnhancements.InMultiplayer)
            {
                foreach (uint id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendReactorOverload(id, _overloaded);
                }
            }
        }

        if (_overloaded)
        {
            float tempLerp = _temperatureDetector.GetTemperatureRatio() * -1f;
            float diffLerp = (float)temperatureDifficulty.GetProperty();
            float passiveAdditive = 0f;
            if ((string)passiveTemperatureGain.GetProperty() == "Cold")
            {
                passiveAdditive = 0.3f;
            }
            float heat = Mathf.Lerp(0f, 0.8f, (tempLerp + passiveAdditive) * diffLerp);
            _reactorHeat.SetOverloadHeat(heat);
        }
    }

    public void SetOverloadedRemote(bool overloaded)
    {
        _overloaded = overloaded;
        if (_overloaded)
        {
            _interactReceiver.ChangePrompt("Reset Reactor");
            _interactReceiver.ResetInteraction();
            _loopSource.FadeIn(0.5f);
        }
        else
        {
            _interactReceiver.ChangePrompt(UITextLibrary.GetString(UITextType.HoldPrompt) + " Overload Reactor");
            _interactReceiver.ResetInteraction();
            _reactorHeat.SetOverloadHeat(0f);

            _loopSource.FadeOut(1f);
            _disableSource.time = 0f;
            _disableSource.SetLocalVolume(1f);
            _disableSource.Play();
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

    private void OnPressInteract()
    {
        if (!_overloaded)
        {
            _disableSource.FadeOut(1f);
            _startupSource.time = 0f;
            _startupSource.SetLocalVolume(1f);
            _startupSource.Play();
        }
    }

    private void OnReleaseInteract()
    {
        if (!_overloaded)
        {
            _startupSource.Stop();
        }
    }

    private void OnReactorDamaged(ShipComponent component)
    {
        _interactReceiver.DisableInteraction();
    }

    private void OnReactorRepaired(ShipComponent component)
    {
        _interactReceiver.EnableInteraction();
    }

    private void OnShipSystemFailure()
    {
        _loopSource.FadeOut(0.5f);
        _disableSource.FadeOut(0.5f);
        _startupSource.FadeOut(0.5f);
        enabled = false;
    }

    private void OnDestroy()
    {
        _interactReceiver.OnGainFocus -= OnGainFocus;
        _interactReceiver.OnLoseFocus -= OnLoseFocus;
        _interactReceiver.OnPressInteract -= OnPressInteract;
        _interactReceiver.OnReleaseInteract -= OnReleaseInteract;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        _reactor.OnDamaged -= OnReactorDamaged;
        _reactor.OnRepaired -= OnReactorRepaired;
    }
}
