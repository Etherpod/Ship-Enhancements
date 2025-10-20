using UnityEngine;

namespace ShipEnhancements;

public class ShipWaterGauge : MonoBehaviour
{
    [SerializeField]
    private Transform _needleTransform;
    [SerializeField]
    private float _needleAngleMin;
    [SerializeField]
    private float _needleAngleMax;
    [SerializeField]
    private OWRenderer _indicatorLight;

    private float _lastNeedleAngle;
    private Quaternion _currentNeedleRotation;
    private bool _lightActive;
    private Color _warningLightColor;
    private bool _interpolating;

    private void Awake()
    {
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _currentNeedleRotation = _needleTransform.localRotation;
        _warningLightColor = new Color(1.3f, 0.55f, 0.55f);
    }

    private void Update()
    {
        Quaternion targetQuaternion;
        float ratio = SELocator.GetShipWaterResource().GetFractionalWater();
        targetQuaternion = Quaternion.AngleAxis(Mathf.Lerp(_needleAngleMin, _needleAngleMax, ratio), Vector3.up);

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

        if (ratio <= 0f)
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
