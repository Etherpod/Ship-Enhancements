using UnityEngine;

namespace ShipEnhancements;

public class WaterCoolingLever : MonoBehaviour
{
    public delegate void ChangeActiveEvent(bool active);
    public event ChangeActiveEvent OnChangeActive;

    [SerializeField]
    private InteractReceiver _interactReceiver;
    [SerializeField]
    private float _disabledAngle;
    [SerializeField]
    private float _enabledAngle;
    [SerializeField]
    private OWAudioSource _leverSource;
    [SerializeField]
    private OWAudioSource _loopSource;
    [SerializeField]
    private AudioClip _onAudio;
    [SerializeField]
    private AudioClip _offAudio;

    private bool _initialized = false;
    private bool _active = false;
    private float _startAngle;
    private float _targetAngle;
    private float _moveStartTime;
    private float _moveTime = 0.3f;

    private void Start()
    {
        transform.localRotation = Quaternion.Euler(new Vector3(_disabledAngle, 0f, 0f));
        _interactReceiver.ChangePrompt("Enable Water Cooling");
        _interactReceiver.OnPressInteract += OnPressInteract;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        GlobalMessenger.AddListener("ExitShip", OnExitShip);
        GlobalMessenger.AddListener("EnterShip", OnEnterShip);
        _initialized = true;
        enabled = false;
    }

    private void Update()
    {
        if (!_initialized)
        {
            enabled = false;
            return;
        }

        float timeLerp = Mathf.InverseLerp(_moveStartTime, _moveStartTime + _moveTime, Time.time);
        float angle = Mathf.SmoothStep(_startAngle, _targetAngle, timeLerp);
        transform.localRotation = Quaternion.Euler(new Vector3(angle, 0f, 0f));
        if (timeLerp >= 1f)
        {
            _interactReceiver.ChangePrompt($"{(_active ? "Disable" : "Enable")} Water Cooling");
            _interactReceiver.EnableInteraction();
            OnChangeActive?.Invoke(_active);
            enabled = false;
        }
    }

    private void OnPressInteract()
    {
        _active = !_active;
        _startAngle = transform.localEulerAngles.x;
        _targetAngle = _active ? _enabledAngle : _disabledAngle;
        _moveStartTime = Time.time;
        _interactReceiver.DisableInteraction();
        enabled = true;

        if (_active)
        {
            _leverSource.PlayOneShot(_onAudio);
            _loopSource.FadeIn(2f);
        }
        else
        {
            _leverSource.PlayOneShot(_offAudio);
            _loopSource.FadeOut(1f);
        }
    }

    private void OnShipSystemFailure()
    {
        _loopSource.FadeOut(0.5f);
        _interactReceiver.DisableInteraction();
        enabled = false;
    }

    private void OnExitShip()
    {
        _loopSource.FadeOut(0.5f);
    }

    private void OnEnterShip()
    {
        if (_active)
        {
            _loopSource.FadeIn(0.5f);
        }
    }

    private void OnDestroy()
    {
        _interactReceiver.OnPressInteract -= OnPressInteract;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
        GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
    }
}
