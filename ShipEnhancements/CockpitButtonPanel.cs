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

    /*public void SetThrustModulatorActive(bool active)
    {
        if (active)
        {
            _numButtons++;
            _thrustModulatorObject.SetActive(true);
            _thrustModulatorReplacement.SetActive(false);
        }
        else
        {
            _thrustModulatorObject.SetActive(false);
            _thrustModulatorReplacement.SetActive(true);
        }
    }

    public void SetGravityLandingGearActive(bool active)
    {
        if (active)
        {
            _numButtons++;
            _gravityGearObject.SetActive(true);
            _gravityGearReplacement.SetActive(false);
        }
        else
        {
            _gravityGearObject.SetActive(false);
            _gravityGearReplacement.SetActive(true);
        }
    }

    public void SetPersistentInputActive(bool active)
    {
        if (active)
        {
            _numButtons++;
            _numBottomButtons++;
            _persistentInputObject.SetActive(true);
            _persistentInputReplacement.SetActive(false);
        }
        else
        {
            _persistentInputObject.SetActive(false);
            _persistentInputReplacement.SetActive(true);
        }
    }

    public void SetEngineSwitchActive(bool active)
    {
        if (active)
        {
            _numButtons++;
            _numBottomButtons++;
            _engineSwitchObject.SetActive(true);
            _engineSwitchReplacement.SetActive(false);
        }
        else
        {
            _engineSwitchObject.SetActive(false);
            _engineSwitchReplacement.SetActive(true);
        }
    }*/

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
