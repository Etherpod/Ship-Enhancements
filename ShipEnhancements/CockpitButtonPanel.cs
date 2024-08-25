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
    private GameObject _thrustModulatorReplacement;
    [SerializeField]
    private GameObject _gravityGearReplacement;
    [SerializeField]
    private Transform _retractedTransform;
    [SerializeField]
    private Transform _extendedTransform;

    private int _numButtons = 0;
    private bool _extending = false;
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
        _cockpitInteractVolume = (InteractZone)Locator.GetShipBody().GetComponentInChildren<ShipCockpitController>()._interactVolume;
        GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
    }
    
    private void Update()
    {
        if (SELocator.GetShipDamageController().IsSystemFailed()) return;

        if (!_extending && OWInput.IsNewlyPressed(InputLibrary.freeLook, InputMode.ShipCockpit))
        {
            _extending = true;
        }
        else if (_extending && OWInput.IsNewlyReleased(InputLibrary.freeLook, InputMode.ShipCockpit))
        {
            _extending = false;
        }
    }

    private void FixedUpdate()
    {
        if (SELocator.GetShipDamageController().IsSystemFailed()) return;

        if (_extending && _buttonPanelT < 1f)
        {
            _buttonPanelT = Mathf.Clamp01(_buttonPanelT + Time.deltaTime / _extensionTime);
            _panelTransform.position = Vector3.Lerp(_retractedTransform.position, _extendedTransform.position, Mathf.SmoothStep(0f, 1f, _buttonPanelT));
        }
        else if (!_extending && _buttonPanelT > 0f)
        {
            _buttonPanelT = Mathf.Clamp01(_buttonPanelT - Time.deltaTime / _extensionTime);
            _panelTransform.position = Vector3.Lerp(_retractedTransform.position, _extendedTransform.position, Mathf.SmoothStep(0f, 1f, _buttonPanelT));
        }
    }

    private void OnExitFlightConsole()
    {
        if (_extending)
        {
            _extending = false;
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
            _persistentInputObject.SetActive(true);
            _bottomPanel.SetActive(true);
        }
        else
        {
            _persistentInputObject.SetActive(false);
            _bottomPanel.SetActive(false);
        }
    }

    public void UpdateFocusedButtons(bool add)
    {
        _focusedButtons = Mathf.Max(_focusedButtons + (add ? 1 : -1), 0);
        if (_focusedButtons > 0)
        {
            _cockpitInteractVolume.DisableInteraction();
        }
        else
        {
            _cockpitInteractVolume.EnableInteraction();
        }
    }
}
