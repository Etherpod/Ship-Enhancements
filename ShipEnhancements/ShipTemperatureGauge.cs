﻿using UnityEngine;

namespace ShipEnhancements;

public class ShipTemperatureGauge : MonoBehaviour
{
    private Transform _needleTransform;
    private float _needleAngleMin = -98f;
    private float _needleAngleMax = 126f;
    private float _lastNeedleAngle;
    private Quaternion _currentNeedleRotation;
    private ShipTemperatureDetector _temperatureDetector;
    private OWRenderer _indicatorLight;
    private bool _lightActive;
    private Color _warningLightColor;
    private bool _shipDestroyed = false;
    private bool _interpolating;

    private void Awake()
    {
        _needleTransform = Locator.GetShipBody().transform.Find("Module_Cockpit/Geo_Cockpit/Cockpit_Tech/Cockpit_Tech_Interior/TemperaturePointerPivot/TemperaturePointer_Geo");
        _currentNeedleRotation = _needleTransform.localRotation;
        _temperatureDetector = Locator.GetShipDetector().GetComponent<ShipTemperatureDetector>();
        _warningLightColor = new Color(1.3f, 0.55f, 0.55f);
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
    }

    private void Start()
    {
        GameObject indicatorLight = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/IndicatorLight_TemperatureGauge.prefab");
        AssetBundleUtilities.ReplaceShaders(indicatorLight);
        _indicatorLight = Instantiate(indicatorLight, Locator.GetShipBody().transform).GetComponent<OWRenderer>();
    }

    private void Update()
    {
        Quaternion targetQuaternion;
        float ratio = _temperatureDetector.GetTemperatureRatio();
        if (ratio > 0)
        {
            targetQuaternion = Quaternion.AngleAxis(Mathf.Lerp(0f, _needleAngleMax, _temperatureDetector.GetTemperatureRatio()), Vector3.right);
        }
        else
        {
            targetQuaternion = Quaternion.AngleAxis(Mathf.Lerp(0f, _needleAngleMin, -_temperatureDetector.GetTemperatureRatio()), Vector3.right);
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

        if (_temperatureDetector.IsHighTemperature())
        {
            _lightActive = true;
            bool flag = Time.timeSinceLevelLoad * 2f % 2f < 1.33f;
            _indicatorLight.SetEmissionColor(flag ? _warningLightColor : Color.black);
        }
        else
        {
            _lightActive = false;
            _indicatorLight.SetEmissionColor(Color.black);
        }
    }

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
        _indicatorLight.SetEmissionColor(Color.black);
        _lightActive = false;
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}