using UnityEngine;

namespace ShipEnhancements;

public class FuelTankTemperatureDetector : TemperatureDetector
{
    [SerializeField]
    private FuelTankItem _fuelTank;

    private float _nextGroanTime;
    private float _groanIntervalMin = 6f;
    private float _groanIntervalMax = 12f;

    protected override void Start()
    {
        _currentTemperature = 0f;
        _highTempCutoff = 50f;
        _internalTempMeterLength = 75f;
    }

    protected override void Update()
    {
        base.Update();

        _fuelTank.SetEmissiveScale(Mathf.InverseLerp(0f, _internalTempMeterLength, _internalTempMeter));
    }

    protected override void UpdateHighTemperature()
    {
        if (_fuelTank.GetFuelRatio() <= 0f || (_currentTemperature < 0 != _internalTempMeter < 0))
        {
            return;
        }

        if (Time.time > _nextGroanTime && _currentTemperature > 0f)
        {
            _nextGroanTime = Time.time + Random.Range(_groanIntervalMin, _groanIntervalMax);
            _fuelTank.PlayGroan();
        }

        if (GetInternalTemperatureRatio() == 1f)
        {
            enabled = false;

            if (_fuelTank.GetComponentInParent<ShipBody>() && (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost()))
            {
                ErnestoDetectiveController.ItWasFuelTank(temperature: true);
            }

            _fuelTank.Explode();
        }
    }
}
