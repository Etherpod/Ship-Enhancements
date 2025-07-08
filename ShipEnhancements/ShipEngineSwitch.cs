using static ShipEnhancements.ShipEnhancements.Settings;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace ShipEnhancements;

public class ShipEngineSwitch : CockpitInteractible
{
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

    private ShipThrusterController _thrusterController;
    private ShipAudioController _audioController;
    private MasterAlarm _alarm;
    private AudioClip _engineSputterClip;
    private bool _turnSwitch = false;
    private float _turnTime = 0.15f;
    private float _turningT;
    private Quaternion _baseRotation;
    private Quaternion _targetRotation;
    private bool _completedTurn = false;
    private float _ignitionTime;
    private float _ignitionDuration;
    private bool _completedIgnition = false;
    private bool _engineStalling = false;
    private Color _indicatorLightColor = new Color(1.3f, 0.55f, 0.55f);
    private bool _lastShipPowerState = false;
    private bool _lastShipThrusterState = false;
    private bool _reset = true;
    private float _baseIndicatorLightIntensity;
    private bool _shipDestroyed = false;

    private bool _controlledRemote = false;

    private readonly string _onPrompt = UITextLibrary.GetString(UITextType.HoldPrompt) + " Start Engine";
    private readonly string _offPrompt = "Turn Off Engine";

    public override void Awake()
    {
        base.Awake();

        _thrusterController = SELocator.GetShipBody().GetComponent<ShipThrusterController>();
        _audioController = SELocator.GetShipBody().GetComponentInChildren<ShipAudioController>();
        _alarm = SELocator.GetShipTransform().GetComponentInChildren<MasterAlarm>();
        _engineSputterClip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/ShipEngineSputter.ogg");

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

        _interactReceiver.ChangePrompt(_onPrompt);
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

            bool thrustersUsable = SELocator.GetShipResources().AreThrustersUsable();
            if (thrustersUsable != _lastShipThrusterState)
            {
                _lastShipThrusterState = thrustersUsable;
                _thrustersIndicator.SetEmissionColor(!thrustersUsable ? Color.black : _indicatorLightColor);
                _thrustersIndicatorLight.intensity = !thrustersUsable ? 0f : _baseIndicatorLightIntensity;
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

                    if (SELocator.GetShipTemperatureDetector() != null)
                    {
                        float ratio = SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio();
                        if (Random.value < Mathf.InverseLerp(-0.5f, -1.16f, ratio))
                        {
                            AudioClip sputterClip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/ShipEngineSputter.ogg");
                            ShipThrusterAudio thrusterAudio = _thrusterController.GetComponentInChildren<ShipThrusterAudio>();
                            thrusterAudio._ignitionSource.Stop();
                            thrusterAudio._isIgnitionPlaying = true;
                            thrusterAudio._ignitionSource.PlayOneShot(sputterClip);
                            _engineStalling = true;
                        }
                        else
                        {
                            _ignitionTime = Time.time;
                            GlobalMessenger.FireEvent("StartShipIgnition");
                        }
                    }
                    else
                    {
                        _ignitionTime = Time.time;
                        GlobalMessenger.FireEvent("StartShipIgnition");
                    }
                }
                if (!_completedIgnition && !_engineStalling && Time.time > _ignitionTime + _ignitionDuration)
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
                    //_thrusterController._isIgniting = false;
                    _interactReceiver.ChangePrompt(_offPrompt);
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

    protected override void OnLoseFocus()
    {
        base.OnLoseFocus();
        OnReleaseInteract();
    }

    protected override void OnPressInteract()
    {
        bool turnOff = _completedTurn;
        OnStartPress();

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendEngineSwitchState(id, true, turnOff);
            }
        }
    }

    private void OnStartPress()
    {
        if (_completedTurn)
        {
            TurnOffEngine();
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

    protected override void OnReleaseInteract()
    {
        if (_controlledRemote)
        {
            return;
        }

        OnStopPress();

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendEngineSwitchState(id, false, false);
            }
        }
    }
    
    public void TurnOffEngine()
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
        if (Locator.GetToolModeSwapper().IsInToolMode(ToolMode.Probe, ToolGroup.Ship)
            || Locator.GetToolModeSwapper().IsInToolMode(ToolMode.SignalScope, ToolGroup.Ship))
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
        _interactReceiver.ChangePrompt(_onPrompt);
        StopAllCoroutines();
        DeactivateIndicatorLights();
    }

    private void OnStopPress()
    {
        if (!_completedIgnition && _turnSwitch)
        {
            _turnSwitch = false;
            _completedTurn = false;
            //_thrusterController._isIgniting = false;
            _engineStalling = false;
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

    public void UpdateWasPressed(bool wasPressed, bool turnOff)
    {
        if (wasPressed)
        {
            if (_completedTurn || !turnOff)
            {
                OnStartPress();
            }
            _controlledRemote = true;
            _interactReceiver.DisableInteraction();
        }
        else
        {
            OnStopPress();
            _controlledRemote = false;
            if (!_shipDestroyed)
            {
                _interactReceiver.EnableInteraction();
            }
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
            _interactReceiver.ChangePrompt(_offPrompt);
        }
    }

    public bool IsEngineStalling()
    {
        return _engineStalling;
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
        DeactivateIndicatorLights();
        _shipDestroyed = true;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        ShipEnhancements.Instance.OnFuelDepleted -= OnFuelDepleted;
        ShipEnhancements.Instance.OnFuelRestored -= OnFuelRestored;

        if (ShipEnhancements.InMultiplayer)
        {
            ShipEnhancements.QSBCompat.RemoveEngineSwitch();
        }
    }
}
