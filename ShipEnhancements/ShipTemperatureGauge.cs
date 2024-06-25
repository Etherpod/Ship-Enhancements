using UnityEngine;

namespace ShipEnhancements;

public class ShipTemperatureGauge : MonoBehaviour
{
    private Transform _needleTransform;
    //private float _needleAngleMin = -98f;
    private float _needleAngleMin = 0f;
    private float _needleAngleMax = 126f;
    private Quaternion _currentNeedleRotation;
    private ShipTemperatureDamage _tempDamageController;

    private void Awake()
    {
        _needleTransform = Locator.GetShipBody().transform.Find("Module_Cockpit/Geo_Cockpit/Cockpit_Tech/Cockpit_Tech_Interior/TemperaturePointerPivot/TemperaturePointer_Geo");
        _currentNeedleRotation = _needleTransform.localRotation;
        _tempDamageController = Locator.GetShipBody().GetComponent<ShipTemperatureDamage>();
    }

    private void Update()
    {
        Quaternion quaternion;
        if (_tempDamageController.IsHighTemperature())
        {
            quaternion = Quaternion.AngleAxis(Mathf.Lerp(_needleAngleMin, _needleAngleMax, _tempDamageController.GetTemperatureRatio()), Vector3.right);
        }
        else
        {
            quaternion = Quaternion.AngleAxis(Mathf.Lerp(_needleAngleMax, _needleAngleMin, _tempDamageController.GetTemperatureRatio()), Vector3.right);
        }

        if (Quaternion.Angle(_currentNeedleRotation, quaternion) >= 0.1f)
        {
            _needleTransform.localRotation = quaternion;
            _currentNeedleRotation = quaternion;
        }
    }
}
