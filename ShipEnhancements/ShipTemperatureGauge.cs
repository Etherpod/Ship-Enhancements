using UnityEngine;

namespace ShipEnhancements;

public class ShipTemperatureGauge : MonoBehaviour
{
    private Transform _needleTransform;
    private float _needleAngleMin = -98f;
    private float _needleAngleMax = 126f;
    private float _lastNeedleAngle;
    private Quaternion _currentNeedleRotation;
    private OWRenderer _indicatorLight;
    private bool _lightActive;
    private Color _warningLightColor;
    private bool _interpolating;

    private void Awake()
    {
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _needleTransform = SELocator.GetShipBody().transform.Find("Module_Cockpit/Geo_Cockpit/Cockpit_Tech/Cockpit_Tech_Interior/TemperaturePointerPivot/TemperaturePointer_Geo");
        _currentNeedleRotation = _needleTransform.localRotation;
        _warningLightColor = new Color(1.3f, 0.55f, 0.55f);
    }

    private void Start()
    {
        GameObject indicatorLight = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/IndicatorLight_TemperatureGauge.prefab");
        _indicatorLight = Instantiate(indicatorLight, SELocator.GetShipBody().transform).GetComponent<OWRenderer>();
    }

    private void Update()
    {
        Quaternion targetQuaternion;
        float ratio = SELocator.GetShipTemperatureDetector().GetTemperatureRatio();
        if (ratio > 0)
        {
            targetQuaternion = Quaternion.AngleAxis(Mathf.Lerp(0f, _needleAngleMax, SELocator.GetShipTemperatureDetector().GetTemperatureRatio()), Vector3.right);
        }
        else
        {
            targetQuaternion = Quaternion.AngleAxis(Mathf.Lerp(0f, _needleAngleMin, -SELocator.GetShipTemperatureDetector().GetTemperatureRatio()), Vector3.right);
        }

        if (Quaternion.Angle(_currentNeedleRotation, targetQuaternion) >= 0.1f)
        {
            if (Quaternion.Angle(_currentNeedleRotation, targetQuaternion) > 0.5f)
            {
                _interpolating = true;
            }
            else
            {
                _interpolating = false;
                _needleTransform.localRotation = targetQuaternion;
                _currentNeedleRotation = targetQuaternion;
            }
        }

        if (_interpolating)
        {
            _needleTransform.localRotation = Quaternion.Lerp(_needleTransform.localRotation, targetQuaternion, Time.deltaTime);
            _currentNeedleRotation = _needleTransform.localRotation;
        }

        if (SELocator.GetShipTemperatureDetector().IsHighTemperature())
        {
            _lightActive = true;
            bool flag = Time.timeSinceLevelLoad * 2f % 2f < 1.33f;
            _indicatorLight.SetEmissionColor(flag ? _warningLightColor : Color.black);
        }
        else if (_lightActive)
        {
            _lightActive = false;
            _indicatorLight.SetEmissionColor(Color.black);
        }
    }

    private void OnShipSystemFailure()
    {
        _indicatorLight.SetEmissionColor(Color.black);
        _lightActive = false;
        enabled = false;
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
