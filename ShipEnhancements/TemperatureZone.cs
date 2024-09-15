using UnityEngine;

namespace ShipEnhancements;

public class TemperatureZone : MonoBehaviour
{
    [SerializeField]
    [Range(-100f, 100f)]
    private float _temperature;
    [SerializeField]
    private float _innerRadius;
    [SerializeField]
    private bool _isShell;
    [SerializeField]
    private float _shellCenterRadius;
    [SerializeField]
    private float _shellCenterThickness;

    private OWTriggerVolume _triggerVolume;
    private SphereShape _shape;
    private float _outerRadius;
    private float _scale = 1f;

    private void Awake()
    {
        _shape = GetComponent<SphereShape>();
        _triggerVolume = GetComponent<OWTriggerVolume>();

        _triggerVolume.OnEntry += OnEffectVolumeEnter;
        _triggerVolume.OnExit += OnEffectVolumeExit;

        _outerRadius = _shape.radius;
    }

    private void OnEffectVolumeEnter(GameObject hitObj)
    {
        if (hitObj.TryGetComponent(out ShipTemperatureDetector detector))
        {
            detector.AddZone(this);
        }
    }

    private void OnEffectVolumeExit(GameObject hitObj)
    {
        if (hitObj.TryGetComponent(out ShipTemperatureDetector detector))
        {
            detector.RemoveZone(this);
        }
    }

    public float GetTemperature()
    {
        float distSqr = (Locator.GetShipDetector().transform.position - (transform.position + _shape.center)).sqrMagnitude;
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

    public void SetScale(float scale)
    {
        _scale = scale;
        _shape.transform.localScale = Vector3.one * scale;
    }

    public void SetProperties(float temperature, float outerRadius, float innerRadius, 
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

    private void OnDestroy()
    {
        _triggerVolume.OnEntry -= OnEffectVolumeEnter;
        _triggerVolume.OnExit -= OnEffectVolumeExit;
    }
}
