using UnityEngine;

namespace ShipEnhancements;

public class ShipWaterMeter : MonoBehaviour
{
    [SerializeField]
    private Transform _needleTransform;
    [SerializeField]
    private OWRenderer _indicatorLight;
    [SerializeField]
    private float _needlePosMax;
    [SerializeField]
    private float _needlePosMin;

    private float _lastNeedlePos;
    private float _currentNeedlePos;
    private bool _lightActive;
    private Color _warningLightColor;
    private bool _interpolating;

    private void Awake()
    {
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _currentNeedlePos = _needleTransform.localPosition.x;
        _warningLightColor = new Color(1.3f, 0.55f, 0.55f);
    }

    private void Update()
    {
        float ratio = SELocator.GetShipWaterResource().GetFractionalWater();
        float targetPos = Mathf.Lerp(_needlePosMin, _needlePosMax, ratio);

        if (Mathf.Abs(targetPos - _currentNeedlePos) >= 0.0002f)
        {
            if (Mathf.Abs(targetPos - _currentNeedlePos) > 0.0004f)
            {
                _interpolating = true;
            }
            else
            {
                _interpolating = false;
                _needleTransform.SetLocalPositionX(targetPos);
                _currentNeedlePos = targetPos;
            }
        }

        if (_interpolating)
        {
            _needleTransform.SetLocalPositionX(Mathf.Lerp(_needleTransform.localPosition.x, targetPos, Time.deltaTime));
            _currentNeedlePos = _needleTransform.localPosition.x;
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
