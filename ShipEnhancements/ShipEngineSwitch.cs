using static ShipEnhancements.ShipEnhancements.Settings;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace ShipEnhancements;

public class ShipEngineSwitch : MonoBehaviour
{
    [SerializeField]
    private InteractReceiver _interactReceiver;
    [SerializeField]
    private Transform _switchTransform;
    [SerializeField]
    private float _targetYRotation;
    [SerializeField]
    private OWRenderer _thrustersIndicator;
    [SerializeField]
    private Light _thrustersIndicatorLight;
    [SerializeField]
    private OWRenderer _powerIndicator;
    [SerializeField]
    private Light _powerIndicatorLight;
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private AudioClip _turnAudio;
    [SerializeField]
    private AudioClip _releaseAudio;

    private CockpitButtonPanel _buttonPanel;
    private ShipThrusterController _thrusterController;
    private ShipAudioController _audioController;
    private MasterAlarm _alarm;
    private bool _turnSwitch = false;
    private float _turnTime = 0.15f;
    private float _turningT;
    private Quaternion _baseRotation;
    private Quaternion _targetRotation;
    private bool _completedTurn = false;
    private float _ignitionTime;
    private float _ignitionDuration;
    private bool _completedIgnition = false;
    private Color _indicatorLightColor = new Color(1.3f, 0.55f, 0.55f);
    private bool _lastShipPowerState = false;
    private bool _reset = true;
    private float _baseIndicatorLightIntensity;

    private bool _controlledRemote = false;

    private void Awake()
    {
        _buttonPanel = GetComponentInParent<CockpitButtonPanel>();

        if ((bool)addEngineSwitch.GetProperty())
        {
            _buttonPanel.SetEngineSwitchActive(true);
        }
        else
        {
            _buttonPanel.SetEngineSwitchActive(false);
            enabled = false;
            return;
        }

        _thrusterController = Locator.GetShipBody().GetComponent<ShipThrusterController>();
        _audioController = Locator.GetShipBody().GetComponentInChildren<ShipAudioController>();
        _alarm = Locator.GetShipTransform().GetComponentInChildren<MasterAlarm>();

        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        ShipEnhancements.Instance.OnFuelDepleted += OnFuelDepleted;
        ShipEnhancements.Instance.OnFuelRestored += OnFuelRestored;

        _baseRotation = _switchTransform.localRotation;
        _targetRotation = Quaternion.Euler(_switchTransform.localRotation.eulerAngles.x, _targetYRotation, 
            _switchTransform.localRotation.eulerAngles.z);
        _ignitionDuration = _thrusterController._ignitionDuration;
        _thrusterController._requireIgnition = false;

        if (ShipEnhancements.InMultiplayer)
        {
            ShipEnhancements.QSBCompat.SetEngineSwitch(this);
        }
    }

    private void Start()
    {
        if (!(bool)addEngineSwitch.GetProperty()) return;

        _interactReceiver.SetPromptText(UITextType.HoldPrompt);
        _interactReceiver.ChangePrompt("Start engine");
        _baseIndicatorLightIntensity = _powerIndicatorLight.intensity;
        _thrustersIndicatorLight.intensity = 0f;
        _powerIndicatorLight.intensity = 0f;
    }
    
    private void Update()
    {
        if (_completedIgnition)
        {
            if ((float)idleFuelConsumptionMultiplier.GetProperty() > 0f)
            {
                float fuelDrain = 0.5f * (float)idleFuelConsumptionMultiplier.GetProperty() * Time.deltaTime;
                SELocator.GetShipResources().DrainFuel(fuelDrain);
            }

            bool electricalFailed = SELocator.GetShipDamageController().IsElectricalFailed();
            if (electricalFailed != _lastShipPowerState)
            {
                _lastShipPowerState = electricalFailed;
                _powerIndicator.SetEmissionColor(electricalFailed ? Color.black : _indicatorLightColor);
                _powerIndicatorLight.intensity = electricalFailed ? 0f : _baseIndicatorLightIntensity;
            }
        }
    }

    private void FixedUpdate()
    {
        if (_turnSwitch)
        {
            if (_turningT < 1)
            {
                _turningT += Time.deltaTime / _turnTime;
                float num = Mathf.InverseLerp(0f, 1f, _turningT);
                _switchTransform.localRotation = Quaternion.Lerp(_baseRotation, _targetRotation, num);
            }
            else
            {
                if (!_completedTurn)
                {
                    _completedTurn = true;
                    _ignitionTime = Time.time;
                    _thrusterController._isIgniting = true;
                    GlobalMessenger.FireEvent("StartShipIgnition");
                }
                if (!_completedIgnition && Time.time > _ignitionTime + _ignitionDuration)
                {
                    _completedIgnition = true;
                    ShipEnhancements.Instance.SetEngineOn(true);
                    ShipElectricalComponent electricalComponent = SELocator.GetShipDamageController()._shipElectricalComponent;
                    if (!electricalComponent.isDamaged && !electricalComponent._electricalSystem.IsPowered())
                    {
                        electricalComponent._electricalSystem.SetPowered(true);
                        AudioClip clip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/PowerRestored.mp3");
                        electricalComponent._audioSource.PlayOneShot(clip, 0.8f);
                    }
                    _alarm.UpdateAlarmState();
                    _audioController.PlayShipAmbient();
                    StartCoroutine(ActivateIndicatorLights(electricalComponent._electricalSystem._systemDelay));
                    _thrusterController._isIgniting = false;
                    _interactReceiver.ChangePrompt("Turn off engine");
                    _interactReceiver.EnableInteraction();
                    GlobalMessenger.FireEvent("CompleteShipIgnition");
                }
            }
        }
        else if (!_turnSwitch)
        {
            if (_turningT > 0)
            {
                _turningT -= Time.deltaTime / _turnTime * 2f;
                float num = Mathf.InverseLerp(0f, 1f, _turningT);
                _switchTransform.localRotation = Quaternion.Lerp(_baseRotation, _targetRotation, num);
            }
            else if (!_reset)
            {
                _audioSource.Stop();
                _reset = true;
                _interactReceiver.EnableInteraction();
            }
        }
    }

    private IEnumerator ActivateIndicatorLights(float delay)
    {
        if (SELocator.GetShipResources().AreThrustersUsable())
        {
            _thrustersIndicator.SetEmissionColor(_indicatorLightColor);
            _thrustersIndicatorLight.intensity = _baseIndicatorLightIntensity;
        }
        yield return new WaitForSeconds(delay);
        if (!SELocator.GetShipDamageController().IsElectricalFailed())
        {
            _powerIndicator.SetEmissionColor(_indicatorLightColor);
            _powerIndicatorLight.intensity = _baseIndicatorLightIntensity;
        }
    }

    private void DeactivateIndicatorLights()
    {
        _thrustersIndicator.SetEmissionColor(Color.black);
        _thrustersIndicatorLight.intensity = 0f;
        _powerIndicator.SetEmissionColor(Color.black);
        _powerIndicatorLight.intensity = 0f;
    }

    private void OnGainFocus()
    {
        _buttonPanel.UpdateFocusedButtons(true);
    }

    private void OnLoseFocus()
    {
        _buttonPanel.UpdateFocusedButtons(false);
        OnReleaseInteract();
    }

    private void OnPressInteract()
    {
        OnStartPress();

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.QSBAPI.GetPlayerIDs().Where(id => id != ShipEnhancements.QSBAPI.GetLocalPlayerID()))
            {
                ShipEnhancements.QSBCompat.SendEngineSwitchState(id, true);
            }
        }
    }

    private void OnStartPress()
    {
        if (_completedTurn)
        {
            _completedTurn = false;
            _turnSwitch = false;
            _completedIgnition = false;
            _reset = false;
            ShipEnhancements.Instance.SetEngineOn(false);
            ShipElectricalComponent electricalComponent = SELocator.GetShipDamageController()._shipElectricalComponent;
            if (!electricalComponent.isDamaged && electricalComponent._electricalSystem.IsPowered())
            {
                electricalComponent._electricalSystem.SetPowered(false);
                electricalComponent._audioSource.PlayOneShot(AudioType.ShipDamageElectricalFailure, 0.5f);
            }
            if ((bool)enablePersistentInput.GetProperty())
            {
                Locator.GetShipBody().GetComponent<ShipPersistentInput>().OnDisableEngine();
            }
            if (OWInput.IsInputMode(InputMode.ShipCockpit) && Locator.GetToolModeSwapper().IsInToolMode(ToolMode.Probe)
                || Locator.GetToolModeSwapper().IsInToolMode(ToolMode.SignalScope))
            {
                Locator.GetToolModeSwapper().UnequipTool();
            }
            if (_releaseAudio)
            {
                _audioSource.clip = _releaseAudio;
                _audioSource.pitch = Random.Range(0.9f, 1.1f);
                _audioSource.Play();
            }
            if (_alarm._isAlarmOn)
            {
                _alarm.TurnOffAlarm();
            }
            _audioController.StopShipAmbient();
            _interactReceiver.ChangePrompt("Start engine");
            StopAllCoroutines();
            DeactivateIndicatorLights();
        }
        else
        {
            _turnSwitch = true;
            _reset = false;
            if (_turnAudio)
            {
                _audioSource.clip = _turnAudio;
                _audioSource.pitch = Random.Range(0.9f, 1.1f);
                _audioSource.Play();
            }
        }
    }

    private void OnReleaseInteract()
    {
        if (_controlledRemote)
        {
            return;
        }

        OnStopPress();

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.QSBAPI.GetPlayerIDs().Where(id => id != ShipEnhancements.QSBAPI.GetLocalPlayerID()))
            {
                ShipEnhancements.QSBCompat.SendEngineSwitchState(id, false);
            }
        }
    }

    private void OnStopPress()
    {
        if (!_completedIgnition && _turnSwitch)
        {
            _turnSwitch = false;
            _completedTurn = false;
            _thrusterController._isIgniting = false;
            GlobalMessenger.FireEvent("CancelShipIgnition");
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
            if (_releaseAudio)
            {
                _audioSource.clip = _releaseAudio;
                _audioSource.pitch = Random.Range(0.9f, 1.1f);
                _audioSource.Play();
            }
        }
        _interactReceiver.ResetInteraction();
    }

    public void UpdateWasPressed(bool wasPressed)
    {
        if (wasPressed)
        {
            OnStartPress();
            _controlledRemote = true;
            _interactReceiver.DisableInteraction();
        }
        else
        {
            OnStopPress();
            _controlledRemote = false;
            _interactReceiver.EnableInteraction();
        }
    }
    
    public void InitializeEngineSwitch(bool completedIgnition)
    {
        if (completedIgnition)
        {
            _switchTransform.localRotation = _targetRotation;
            _completedTurn = true;
            _completedIgnition = true;
            ShipEnhancements.Instance.SetEngineOn(true);
            /*ShipElectricalComponent electricalComponent = SELocator.GetShipDamageController()._shipElectricalComponent;
            if (!electricalComponent.isDamaged && !electricalComponent._electricalSystem.IsPowered())
            {
                electricalComponent._electricalSystem.SetPowered(true);
            }*/
            //_alarm.UpdateAlarmState();
            //_audioController.PlayShipAmbient();
            StartCoroutine(ActivateIndicatorLights(0f));
            _interactReceiver.ChangePrompt("Turn off engine");
        }
    }

    private void OnFuelDepleted()
    {
        _thrustersIndicator.SetEmissionColor(Color.black);
        _thrustersIndicatorLight.intensity = 0f;
    }

    private void OnFuelRestored()
    {
        if (_completedIgnition)
        {
            _thrustersIndicator.SetEmissionColor(_indicatorLightColor);
            _thrustersIndicatorLight.intensity = _baseIndicatorLightIntensity;
        }
    }

    private void OnShipSystemFailure()
    {
        if (!_completedTurn)
        {
            OnReleaseInteract();
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }
        _interactReceiver.DisableInteraction();
    }

    private void OnDestroy()
    {
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        ShipEnhancements.Instance.OnFuelDepleted -= OnFuelDepleted;
        ShipEnhancements.Instance.OnFuelRestored -= OnFuelRestored;

        if (ShipEnhancements.InMultiplayer)
        {
            ShipEnhancements.QSBCompat.RemoveEngineSwitch();
        }
    }
}
