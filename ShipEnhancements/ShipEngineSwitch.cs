using static ShipEnhancements.ShipEnhancements.Settings;
using UnityEngine;

namespace ShipEnhancements;

public class ShipEngineSwitch : MonoBehaviour
{
    [SerializeField]
    private InteractReceiver _interactReceiver;
    [SerializeField]
    private Transform _switchTransform;
    [SerializeField]
    private float _targetYRotation;

    private CockpitButtonPanel _buttonPanel;
    private ShipThrusterController _thrusterController;
    private bool _turnSwitch = false;
    private float _turnTime = 0.15f;
    private float _turningT;
    private Quaternion _baseRotation;
    private Quaternion _targetRotation;
    private bool _completedTurn = false;
    private float _ignitionTime;
    private float _ignitionDuration;
    private bool _completedIgnition = false;


    private void Awake()
    {
        _buttonPanel = GetComponentInParent<CockpitButtonPanel>();
        _thrusterController = Locator.GetShipBody().GetComponent<ShipThrusterController>();

        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _baseRotation = _switchTransform.localRotation;
        _targetRotation = Quaternion.Euler(_switchTransform.localRotation.eulerAngles.x, _targetYRotation, 
            _switchTransform.localRotation.eulerAngles.z);
        _ignitionDuration = _thrusterController._ignitionDuration;
        _thrusterController._requireIgnition = false;
    }

    private void Start()
    {
        _interactReceiver.SetPromptText(UITextType.HoldPrompt);
        _interactReceiver.ChangePrompt("Start engine");
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
                    GlobalMessenger.FireEvent("CompleteShipIgnition");
                }
            }
        }
        else if (!_turnSwitch)
        {
            if (_turningT > 0)
            {
                _turningT -= Time.deltaTime / _turnTime;
                float num = Mathf.InverseLerp(0f, 1f, _turningT);
                _switchTransform.localRotation = Quaternion.Slerp(_baseRotation, _targetRotation, num);
            }
        }
    }

    private void OnGainFocus()
    {
        _buttonPanel.UpdateFocusedButtons(true);
    }

    private void OnLoseFocus()
    {
        _buttonPanel.UpdateFocusedButtons(false);
    }

    private void OnPressInteract()
    {
        if (_completedTurn)
        {
            _completedTurn = false;
            _turnSwitch = false;
            _completedIgnition = false;
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
        }
        else
        {
            _turnSwitch = true;
        }
    }

    private void OnReleaseInteract()
    {
        if (!_completedIgnition)
        {
            _turnSwitch = false;
            _completedTurn = false;
            GlobalMessenger.FireEvent("CancelShipIgnition");
        }
        _interactReceiver.ResetInteraction();
    }

    private void OnShipSystemFailure()
    {
        _interactReceiver.DisableInteraction();   
    }

    private void OnDestroy()
    {
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
