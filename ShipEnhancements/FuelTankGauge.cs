using UnityEngine;

namespace ShipEnhancements;

public class FuelTankGauge : MonoBehaviour
{
    [SerializeField] private FuelTankItem _fuelTank;
    [SerializeField] private Transform _needleTransform;
    [SerializeField] private float _needleAngleMin;
    [SerializeField] private float _needleAngleMax;
    private float _lastNeedleAngle;
    private Quaternion _currentNeedleRotation;
    private bool _interpolating;

    private void Awake()
    {
        _currentNeedleRotation = _needleTransform.localRotation;
    }

    private void Update()
    {
        Quaternion targetQuaternion;
        float ratio = _fuelTank.GetFuelRatio();
        targetQuaternion = Quaternion.AngleAxis(Mathf.Lerp(_needleAngleMax, _needleAngleMin, ratio), Vector3.up);

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
    }
}
