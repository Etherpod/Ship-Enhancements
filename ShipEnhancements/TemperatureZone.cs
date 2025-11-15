using UnityEngine;

namespace ShipEnhancements;

public class TemperatureZone : MonoBehaviour
{
    [SerializeField]
    [Range(-100f, 100f)]
    protected float _temperature;
    [SerializeField]
    protected float _innerRadius;
    [SerializeField]
    protected bool _isShell;
    [SerializeField]
    protected float _shellCenterRadius;
    [SerializeField]
    protected float _shellCenterThickness;

    protected OWTriggerVolume _triggerVolume;
    protected SphereShape _shape;
    protected float _outerRadius;
    protected float _scale = 1f;
    protected bool _active = true;

    protected virtual void Awake()
    {
        _shape = GetComponent<SphereShape>();
        _triggerVolume = gameObject.GetAddComponent<OWTriggerVolume>();

        _triggerVolume.OnEntry += OnEffectVolumeEnter;
        _triggerVolume.OnExit += OnEffectVolumeExit;

        _outerRadius = _shape.radius;
    }

    protected virtual void OnEffectVolumeEnter(GameObject hitObj)
    {
        if (hitObj.TryGetComponent(out TemperatureDetector detector))
        {
            detector.AddZone(this);
        }
    }

    protected virtual void OnEffectVolumeExit(GameObject hitObj)
    {
        if (hitObj.TryGetComponent(out TemperatureDetector detector))
        {
            detector.RemoveZone(this);
        }
    }

    public virtual float GetTemperature(TemperatureDetector detector)
    {
        if (!_active)
        {
            return 0;
        }

        float distSqr = (detector.transform.position - (transform.position + _shape.center)).sqrMagnitude;
        float multiplier;
        if (_isShell)
        {
            float a = Mathf.InverseLerp(Mathf.Pow(_outerRadius * _scale, 2), Mathf.Pow((_shellCenterRadius + _shellCenterThickness) * _scale, 2), distSqr);
            float b = Mathf.InverseLerp(Mathf.Pow((_shellCenterRadius - _shellCenterThickness) * _scale, 2), Mathf.Pow(_innerRadius * _scale, 2), distSqr);
            multiplier = a - b;
        }
        else
        {
            multiplier = Mathf.InverseLerp(Mathf.Pow(_outerRadius * _scale, 2), Mathf.Pow(_innerRadius * _scale, 2), distSqr);
        }
        return _temperature * multiplier;
    }

    public virtual void SetScale(float scale)
    {
        _scale = scale;
        _shape.transform.localScale = Vector3.one * scale;
    }

    public virtual void SetProperties(float temperature, float outerRadius, float innerRadius, 
        bool isShell, float shellCenterRadius, float shellCenterThickness)
    {
        _temperature = temperature;
        _outerRadius = outerRadius;
        _shape.radius = outerRadius;
        _innerRadius = innerRadius;
        _isShell = isShell;
        _shellCenterRadius = shellCenterRadius;
        _shellCenterThickness = shellCenterThickness;
    }

    public virtual void SetProperties(float temperature, float outerRadius, float innerRadius,
        bool isShell, float shellCenterRadius, float shellCenterThickness, 
        float nightTemperature, float twilightAngle, string customSunName)
    {
        _temperature = temperature;
        _outerRadius = outerRadius;
        _shape.radius = outerRadius;
        _innerRadius = innerRadius;
        _isShell = isShell;
        _shellCenterRadius = shellCenterRadius;
        _shellCenterThickness = shellCenterThickness;
    }

    public virtual void SetVolumeActive(bool active)
    {
        _active = active;
    }

    protected virtual void OnDestroy()
    {
        if (_triggerVolume)
        {
            _triggerVolume.OnEntry -= OnEffectVolumeEnter;
            _triggerVolume.OnExit -= OnEffectVolumeExit;
        }
    }
}
