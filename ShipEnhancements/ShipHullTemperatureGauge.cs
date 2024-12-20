using UnityEngine;

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

    private void Awake()
    {
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _currentNeedleRotation = _needleTransform.localRotation;
    }

    private void Update()
    {
        Quaternion targetQuaternion;
        float ratio = SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio();
        targetQuaternion = Quaternion.AngleAxis(Mathf.Lerp(_needleAngleMin, _needleAngleMax, SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio()), Vector3.right);

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
