using System.Linq;
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
    private GameObject _thrustModulatorReplacement;
    [SerializeField]
    private GameObject _gravityGearReplacement;
    [SerializeField]
    private GameObject _persistentInputReplacement;
    [SerializeField]
    private GameObject _engineSwitchReplacement;
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

    private int _numButtons = 0;
    private int _numBottomButtons = 0;
    private bool _extending = false;
    private bool _completedSlide = false;
    private float _buttonPanelT = 0f;
    private float _extensionTime = 0.4f;
    private int _focusedButtons;
    private InteractZone _cockpitInteractVolume;

    private void Start()
    {
        if (_numButtons == 0)
        {
            gameObject.SetActive(false);
            return;
        }
        else if (_numBottomButtons == 0)
        {
            _bottomPanel.SetActive(false);
        }
        _cockpitInteractVolume = (InteractZone)SELocator.GetShipBody().GetComponentInChildren<ShipCockpitController>()._interactVolume;
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
            _audioSource.Stop();
            _audioSource.clip = _extendAudio;
            _audioSource.Play();
        }
        else
        {
            _extending = false;
            _completedSlide = false;
            _audioSource.Stop();
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

    public void SetThrustModulatorActive(bool active)
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
    }

    public void UpdateFocusedButtons(bool add)
    {
        _focusedButtons = Mathf.Max(_focusedButtons + (add ? 1 : -1), 0);
        if (_focusedButtons > 0)
        {
            _cockpitInteractVolume.DisableInteraction();
        }
        else if (!PlayerState.AtFlightConsole())
        {
            if (ShipEnhancements.QSBInteraction == null || !ShipEnhancements.QSBInteraction.FlightConsoleOccupied())
            {
                ShipEnhancements.WriteDebugMessage("Enable");
                _cockpitInteractVolume.EnableInteraction();
            }
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
