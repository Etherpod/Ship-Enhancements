using static ShipEnhancements.ShipEnhancements.Settings;
using UnityEngine;

namespace ShipEnhancements;

public class CockpitButtonPanel : MonoBehaviour
{
    [SerializeField]
    private Transform _panelTransform;
    [SerializeField]
    private GameObject _bottomPanel;
    [SerializeField]
    private GameObject _thrustModulatorObject;
    [SerializeField]
    private GameObject _gravityGearObject;
    [SerializeField]
    private GameObject _persistentInputObject;
    [SerializeField]
    private GameObject _engineSwitchObject;
    [SerializeField]
    private GameObject _alignSwitchObject;
    [SerializeField]
    private GameObject _thrustModulatorReplacement;
    [SerializeField]
    private GameObject _gravityGearReplacement;
    [SerializeField]
    private GameObject _persistentInputReplacement;
    [SerializeField]
    private GameObject _engineSwitchReplacement;
    [SerializeField]
    private GameObject _alignSwitchReplacement;
    [SerializeField]
    private Transform _retractedTransform;
    [SerializeField]
    private Transform _extendedTransform;
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private AudioClip _extendAudio;
    [SerializeField]
    private AudioClip _retractAudio;
    [SerializeField]
    private AudioClip _finishSlideAudio;

    private bool _extending = false;
    private bool _completedSlide = false;
    private float _buttonPanelT = 0f;
    private float _extensionTime = 0.4f;
    private int _focusedButtons = 0;

    public struct ButtonStates
    {
        public bool gravState;
        public bool invertPowerState;
        public bool alignState;
        public bool alignModeState;
        public int modulatorLevel;
        public string selectedAutopilot;
        public string selectedMatchVelocity;
        public bool engineSwitchState;

        public ButtonStates()
        {
            gravState = false;
            invertPowerState = false;
            alignState = false;
            alignModeState = false;
            modulatorLevel = 5;
            selectedAutopilot = "Activate Autopilot";
            selectedMatchVelocity = "Activate Match Velocity";
            engineSwitchState = false;
        }
    }

    private void Awake()
    {
        if (!(bool)enableGravityLandingGear.GetProperty()
            && !(bool)enableThrustModulator.GetProperty()
            && !(bool)enableEnhancedAutopilot.GetProperty()
            && !(bool)addEngineSwitch.GetProperty()
            && !(bool)enableAutoAlign.GetProperty())
        {
            gameObject.SetActive(false);
            return;
        }
        else if (!(bool)enableEnhancedAutopilot.GetProperty()
            && !(bool)addEngineSwitch.GetProperty())
        {
            _bottomPanel.SetActive(false);
        }

        _gravityGearObject.SetActive((bool)enableGravityLandingGear.GetProperty());
        _gravityGearReplacement.SetActive(!(bool)enableGravityLandingGear.GetProperty());
        _thrustModulatorObject.SetActive((bool)enableThrustModulator.GetProperty());
        _thrustModulatorReplacement.SetActive(!(bool)enableThrustModulator.GetProperty());
        _alignSwitchObject.SetActive((bool)enableAutoAlign.GetProperty());
        _alignSwitchReplacement.SetActive(!(bool)enableAutoAlign.GetProperty());
        _persistentInputObject.SetActive((bool)enableEnhancedAutopilot.GetProperty());
        _persistentInputReplacement.SetActive(!(bool)enableEnhancedAutopilot.GetProperty());
        _engineSwitchObject.SetActive((bool)addEngineSwitch.GetProperty());
        _engineSwitchReplacement.SetActive(!(bool)addEngineSwitch.GetProperty());

        GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
    }

    private void Update()
    {
        if (!PlayerState.AtFlightConsole()) return;

        if (!_extending && OWInput.IsNewlyPressed(InputLibrary.freeLook, InputMode.ShipCockpit))
        {
            UpdateExtended(true);
            if (ShipEnhancements.InMultiplayer)
            {
                foreach (uint id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendPanelExtended(id, true);
                }
            }
        }
        else if (_extending && OWInput.IsNewlyReleased(InputLibrary.freeLook, InputMode.ShipCockpit))
        {
            UpdateExtended(false);
            if (ShipEnhancements.InMultiplayer)
            {
                foreach (uint id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendPanelExtended(id, false);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (_extending)
        {
            if (_buttonPanelT < 1f)
            {
                _buttonPanelT = Mathf.Clamp01(_buttonPanelT + Time.deltaTime / _extensionTime);
                _panelTransform.position = Vector3.Lerp(_retractedTransform.position, _extendedTransform.position, Mathf.SmoothStep(0f, 1f, _buttonPanelT));
            }
            else if (!_completedSlide)
            {
                _completedSlide = true;
                _audioSource.PlayOneShot(_finishSlideAudio, 0.8f);
            }
        }
        else if (!_extending)
        {
            if (_buttonPanelT > 0f)
            {
                _buttonPanelT = Mathf.Clamp01(_buttonPanelT - Time.deltaTime / _extensionTime);
                _panelTransform.position = Vector3.Lerp(_retractedTransform.position, _extendedTransform.position, Mathf.SmoothStep(0f, 1f, _buttonPanelT));
            }
            else if (!_completedSlide)
            {
                _completedSlide = true;
                _audioSource.PlayOneShot(_finishSlideAudio, 0.8f);
            }
        }
    }

    public void UpdateExtended(bool extended)
    {
        if (extended)
        {
            _extending = true;
            _completedSlide = false;
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
            _audioSource.clip = _extendAudio;
            _audioSource.Play();
        }
        else
        {
            _extending = false;
            _completedSlide = false;
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
            _audioSource.clip = _retractAudio;
            _audioSource.Play();
        }
    }

    private void OnExitFlightConsole()
    {
        if (_extending)
        {
            UpdateExtended(false);
            if (ShipEnhancements.InMultiplayer)
            {
                foreach (uint id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendPanelExtended(id, false);
                }
            }
        }
    }

    public ButtonStates GetButtonStates()
    {
        ButtonStates state = new();
        if (_gravityGearObject.activeInHierarchy)
        {
            state.gravState = _gravityGearObject.GetComponentInChildren<GravityLandingGearSwitch>().IsOn();
            state.invertPowerState = _gravityGearObject.GetComponentInChildren<GravityGearInvertSwitch>().IsOn();
        }
        if (_alignSwitchObject.activeInHierarchy)
        {
            state.alignState = _alignSwitchObject.GetComponentInChildren<AutoAlignButton>().IsOn();
            state.alignModeState = _alignSwitchObject.GetComponentInChildren<AutoAlignDirectionButton>().IsOn();
        }
        if (_thrustModulatorObject.activeInHierarchy)
        {
            state.modulatorLevel = ShipEnhancements.Instance.ThrustModulatorLevel;
        }
        if (_persistentInputObject.activeInHierarchy)
        {
            AutopilotPanelController autopilotPanel = _persistentInputObject.GetComponentInChildren<AutopilotPanelController>();
            state.selectedAutopilot = autopilotPanel.GetActiveAutopilot().GetOnLabel();
            state.selectedMatchVelocity = autopilotPanel.GetActiveMatchVelocity().GetOnLabel();
        }
        if (_engineSwitchObject.activeInHierarchy)
        {
            state.engineSwitchState = ShipEnhancements.Instance.engineOn;
        }
        return state;
    }

    public void SetButtonStates(ButtonStates data)
    {
        if (_gravityGearObject.activeInHierarchy)
        {
            _gravityGearObject.GetComponentInChildren<GravityLandingGearSwitch>().SetState(data.gravState);
            _gravityGearObject.GetComponentInChildren<GravityGearInvertSwitch>().SetState(data.invertPowerState);
        }
        if (_alignSwitchObject.activeInHierarchy)
        {
            _alignSwitchObject.GetComponentInChildren<AutoAlignButton>().SetState(data.alignState);
            _alignSwitchObject.GetComponentInChildren<AutoAlignDirectionButton>().SetState(data.alignModeState);
        }
        if (_thrustModulatorObject.activeInHierarchy)
        {
            ShipEnhancements.Instance.SetThrustModulatorLevel(data.modulatorLevel);
            _thrustModulatorObject.GetComponentInChildren<ThrustModulatorController>().UpdateModulatorDisplay(data.modulatorLevel);
        }
        if (_persistentInputObject.activeInHierarchy)
        {
            AutopilotPanelController autopilotPanel = _persistentInputObject.GetComponentInChildren<AutopilotPanelController>(true);
            foreach (var bs in _persistentInputObject.GetComponentsInChildren<CockpitButtonSwitch>(true))
            {
                if ((bs.GetOnLabel() == data.selectedAutopilot
                    || bs.GetOnLabel() == data.selectedMatchVelocity)
                    && !bs.IsOn())
                {
                    bs.SetState(true);
                    bs.RaiseChangeStateEvent();
                    bs.OnChangeStateEvent();
                }
            }
        }
        if (_engineSwitchObject.activeInHierarchy)
        {
            _engineSwitchObject.GetComponentInChildren<ShipEngineSwitch>(true).InitializeEngineSwitch(data.engineSwitchState);
        }
    }

    private void OnShipSystemFailure()
    {
        enabled = false;
        _audioSource.Stop();
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
