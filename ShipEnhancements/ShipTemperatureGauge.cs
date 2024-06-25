using UnityEngine;

namespace ShipEnhancements;

public class ShipTemperatureGauge : MonoBehaviour
{
    private Transform _needleTransform;
    private float _needleAngleMin = -98f;
    private float _needleAngleMax = 126f;
    private float _lastNeedleAngle;
    private Quaternion _currentNeedleRotation;
    private ShipTemperatureDetector _temperatureDetector;

    private void Awake()
    {
        _needleTransform = Locator.GetShipBody().transform.Find("Module_Cockpit/Geo_Cockpit/Cockpit_Tech/Cockpit_Tech_Interior/TemperaturePointerPivot/TemperaturePointer_Geo");
        _currentNeedleRotation = _needleTransform.localRotation;
        _lastNeedleAngle = 0f;
        _temperatureDetector = Locator.GetShipDetector().GetComponent<ShipTemperatureDetector>();
    }

    private void Update()
    {
        Quaternion quaternion;
        float ratio = _temperatureDetector.GetTemperatureRatio();
        if (ratio > 0)
        {
            quaternion = Quaternion.AngleAxis(Mathf.Lerp(0f, _needleAngleMax, _temperatureDetector.GetTemperatureRatio()), Vector3.right);
        }
        else
        {
            quaternion = Quaternion.AngleAxis(Mathf.Lerp(0f, _needleAngleMin, -_temperatureDetector.GetTemperatureRatio()), Vector3.right);
        }

        _lastNeedleAngle = quaternion.eulerAngles.x;

        if (Quaternion.Angle(_currentNeedleRotation, quaternion) >= 0.1f)
        {
            _needleTransform.localRotation = quaternion;
            _currentNeedleRotation = quaternion;
        }
    }
}
