﻿using UnityEngine;

namespace ShipEnhancements;

public class ShipHullTemperatureGauge : MonoBehaviour
{
    [SerializeField]
    private Transform _needleTransform;
    [SerializeField]
    private float _needleAngleMin;
    [SerializeField]
    private float _needleAngleMax;
    private float _lastNeedleAngle;
    private Quaternion _currentNeedleRotation;
    private ShipTemperatureDetector _temperatureDetector;

    private void Awake()
    {
        _temperatureDetector = Locator.GetShipDetector().GetComponent<ShipTemperatureDetector>();

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _currentNeedleRotation = _needleTransform.localRotation;
    }

    private void Update()
    {
        Quaternion targetQuaternion;
        float ratio = _temperatureDetector.GetShipTemperatureRatio();
        targetQuaternion = Quaternion.AngleAxis(Mathf.Lerp(_needleAngleMin, _needleAngleMax, _temperatureDetector.GetShipTemperatureRatio()), Vector3.right);

        if (Quaternion.Angle(_currentNeedleRotation, targetQuaternion) >= 0.1f)
        {
            _needleTransform.localRotation = targetQuaternion;
            _currentNeedleRotation = targetQuaternion;
        }
    }

    private void OnShipSystemFailure()
    {
        enabled = false;
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
