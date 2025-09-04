using UnityEngine;

namespace ShipEnhancements;

public class DayNightTemperatureZone : TemperatureZone
{
    [SerializeField]
    [Range(-100f, 100f)]
    private float _nightTemperature;
    [SerializeField]
    private float _twilightAngle = 15f;
    [SerializeField]
    private bool _useCustomSun = false;
    [SerializeField]
    private string _customSunName;

    private Transform _sunTransform;
    private OWRigidbody _planetBody;

    private void Start()
    {
        _planetBody = gameObject.GetAttachedOWRigidbody();
        if (_useCustomSun && ShipEnhancements.NHAPI != null)
        {
            var sun = ShipEnhancements.NHAPI.GetPlanet(_customSunName);
            if (sun != null)
            {
                _sunTransform = sun.transform;
            }
        }
        else
        {
            _sunTransform = GameObject.Find("Sun_Body")?.transform;
        }

        var setting = (string)ShipEnhancements.Settings.temperatureZonesAmount.GetProperty();
        if (setting == "Hot")
        {
            _temperature = Mathf.Max(_temperature, 0f);
            _nightTemperature = Mathf.Max(_nightTemperature, 0f);
        }
        else if (setting == "Cold")
        {
            _temperature = Mathf.Min(_temperature, 0f);
            _nightTemperature = Mathf.Min(_nightTemperature, 0f);
        }
    }

    public override float GetTemperature(TemperatureDetector detector)
    {
        if (!_active || _sunTransform == null)
        {
            return 0;
        }

        var toSun = (_sunTransform.position - _planetBody.transform.position).normalized;
        var toDetector = (detector.transform.position - _planetBody.transform.position).normalized;
        var dayAmount = Vector3.Dot(toSun, toDetector);
        float twilightOffset = 1 - Mathf.Sin(_twilightAngle * Mathf.Deg2Rad * 0.5f);
        float nightLerp = Mathf.InverseLerp(twilightOffset, -twilightOffset, dayAmount);

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
        float temp = Mathf.SmoothStep(_temperature, _nightTemperature, nightLerp);
        return temp * multiplier;
    }
}
