using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipWaterResource : MonoBehaviour
{
    private WaterCoolingLever _coolingLever;
    private float _maxWater = 5000f;
    private float _currentWater;
    private bool _coolingActive = false;

    private void Awake()
    {
        _currentWater = _maxWater;
    }

    private void Start()
    {
        if ((bool)addWaterCooling.GetProperty())
        {
            _coolingLever = SELocator.GetShipTransform().GetComponentInChildren<WaterCoolingLever>();
            _coolingLever.OnChangeActive += OnSetCoolingActive;
        }
    }

    private void Update()
    {
        if (_coolingActive && SELocator.GetShipTemperatureDetector())
        {
            float temp = SELocator.GetShipTemperatureDetector().GetTemperatureRatio();
            if (temp <= 0f) return;
            float amount = Mathf.Lerp(0.2f, 3f, temp);
            if (!SELocator.GetShipTemperatureDetector().IsHighTemperature())
            {
                amount /= 2f;
            }
            DrainWater(amount * Time.deltaTime);
        }
    }

    public float GetWater()
    {
        return _currentWater;
    }

    public float GetFractionalWater()
    {
        return _currentWater / _maxWater;
    }

    public bool IsCoolingActive()
    {
        return _coolingActive && _currentWater > 0f;
    }

    public void DrainWater(float amount)
    {
        _currentWater = Mathf.Max(_currentWater - (amount * (float)waterDrainMultiplier.GetProperty()), 0f);
    }

    public void AddWater(float amount)
    {
        _currentWater = Mathf.Min(_currentWater + (amount * (float)waterDrainMultiplier.GetProperty()), _maxWater);
    }

    private void OnSetCoolingActive(bool active)
    {
        _coolingActive = active;
    }

    private void OnDestroy()
    {
        if ((bool)addWaterCooling.GetProperty())
        {
            _coolingLever.OnChangeActive -= OnSetCoolingActive;
        }
    }
}
