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
    }

    private void OnDestroy()
    {
        _interactReceiver.OnPressInteract -= OnPressInteract;
    }
}
