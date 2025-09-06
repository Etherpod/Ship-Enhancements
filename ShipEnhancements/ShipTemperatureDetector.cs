using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipTemperatureDetector : TemperatureDetector
{
    private ShockLayerController _shockLayerController;
    private ShipFluidDetector _fluidDetector;
    private ReactorHeatController _reactorHeat;
    private bool _waterCooling;

    protected override void Start()
    {
        base.Start();

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        _shockLayerController = SELocator.GetShipTransform().GetComponentInChildren<ShockLayerController>();
        _fluidDetector = SELocator.GetShipDetector().GetComponent<ShipFluidDetector>();
        _reactorHeat = SELocator.GetShipDamageController()._shipReactorComponent.GetComponent<ReactorHeatController>();
        _maxInternalTemperature *= Mathf.Max(Mathf.Abs((float)temperatureResistanceMultiplier.GetProperty()), 1f);
        _waterCooling = (bool)addWaterTank.GetProperty() && (bool)addWaterCooling.GetProperty();
    }

    protected override void Update()
    {
        base.Update();

        if ((string)passiveTemperatureGain.GetProperty() != "None")
        {
            bool heating = (string)passiveTemperatureGain.GetProperty() == "Hot";
            float resistance = (float)temperatureResistanceMultiplier.GetProperty();

            // Reduce effect in opposite temperature
            float multiplier = 0.25f + (0.75f * Mathf.InverseLerp(_highTempCutoff / 4f * (heating ? -1f : 1f), 0f, _currentTemperature));

            // Reactor increases heat and decreases cold
            float additiveMultiplier = 0f;
            if (SELocator.GetShipDamageController().IsReactorCritical())
            {
                if (heating)
                {
                    additiveMultiplier = 1.5f;
                }
                else
                {
                    additiveMultiplier = -0.5f;
                }
            }

            if (multiplier > 0 || additiveMultiplier > 0)
            {
                if (resistance == 0)
                {
                    ErnestoDetectiveController.ItWasExplosion(fromTemperature: true);
                    SELocator.GetShipDamageController().Explode();
                }
                else
                {
                    // Change faster when close to zero
                    float scalar = 1 + (1f * Mathf.InverseLerp(_highTempCutoff, 0f, Mathf.Abs(_currentTemperature)));

                    _currentInternalTemperature = Mathf.Clamp(_currentInternalTemperature + Time.deltaTime
                        * Mathf.Max((multiplier * scalar) + additiveMultiplier, 0f) * (heating ? 1f : -1f) * Mathf.Sign(resistance),
                        -_maxInternalTemperature, _maxInternalTemperature);
                }
            }
        }
    }

    protected override bool CanUpdateTemperature()
    {
        return base.CanUpdateTemperature() || (_shockLayerController.enabled 
            && _shockLayerController._ruleset != null) || _fluidDetector.InFluidType(FluidVolume.Type.WATER);
    }

    protected override float CalculateCurrentTemperature()
    {
        float totalTemperature = 0f;
        foreach (TemperatureZone zone in _activeZones)
        {
            float temp = zone.GetTemperature(this);
            totalTemperature += temp;
        }

        if (_shockLayerController.enabled && _shockLayerController._ruleset != null)
        {
            float shockSpeedPercent = 0f;
            if (_shockLayerController._ruleset.GetShockLayerType() == ShockLayerRuleset.ShockType.Atmospheric)
            {
                Vector3 toCenter = _shockLayerController._ruleset.GetRadialCenter().position - _shockLayerController._owRigidbody.GetPosition();
                float centerDist = toCenter.magnitude;
                float radiusMultiplier = 1f - Mathf.InverseLerp(_shockLayerController._ruleset.GetInnerRadius(),
                    _shockLayerController._ruleset.GetOuterRadius(), centerDist);

                Vector3 relativeFluidVelocity = _shockLayerController._fluidDetector.GetRelativeFluidVelocity();
                float velocityMagnitude = relativeFluidVelocity.magnitude;
                float minSpeed = _shockLayerController._ruleset.GetMinShockSpeed();
                float maxSpeed = _shockLayerController._ruleset.GetMaxShockSpeed();
                shockSpeedPercent = Mathf.InverseLerp(minSpeed + ((maxSpeed - minSpeed) / 2), maxSpeed, velocityMagnitude);
                shockSpeedPercent *= radiusMultiplier;
            }

            totalTemperature += Mathf.Lerp(0f, 65f, shockSpeedPercent);
        }

        if (_fluidDetector.InFluidType(FluidVolume.Type.WATER)
            || _fluidDetector.InFluidType(FluidVolume.Type.GEYSER))
        {
            totalTemperature -= 15f;
        }

        return Mathf.Clamp(totalTemperature, -100f, 100f);
    }

    protected override void OnHighTemperature()
    {
        if ((float)temperatureResistanceMultiplier.GetProperty() == 0)
        {
            SELocator.GetShipDamageController().Explode();
        }
    }

    protected override void UpdateInternalTemperature()
    {
        bool ignoreSameSide = false;

        if ((IsHeatingLocked() && _currentInternalTemperature > 0f)
            || (IsCoolingLocked() && _currentInternalTemperature < 0f))
        {
            _currentInternalTemperature = Mathf.MoveTowards(_currentInternalTemperature, 0f, Time.deltaTime * 2f);
            ignoreSameSide = true;
        }

        float cutoff = Mathf.Abs(_maxInternalTemperature * GetTemperatureRatio());
        bool sameSide = _currentInternalTemperature < 0 == _currentTemperature < 0;

        if ((float)temperatureResistanceMultiplier.GetProperty() < 0f)
        {
            sameSide = !sameSide;
        }

        if (sameSide && !ignoreSameSide && Mathf.Abs(_currentInternalTemperature) < cutoff)
        {
            _currentInternalTemperature += Time.deltaTime * 3f * Mathf.InverseLerp(_highTempCutoff, 100f, Mathf.Abs(_currentTemperature)) * Mathf.Sign(GetTemperatureRatio())
                * Mathf.Sign((float)temperatureResistanceMultiplier.GetProperty());
        }
        else if (!sameSide)
        {
            _currentInternalTemperature += Time.deltaTime * Mathf.InverseLerp(0f, _highTempCutoff, Mathf.Abs(_currentTemperature)) 
                * Mathf.Sign(GetTemperatureRatio()) * Mathf.Sign((float)temperatureResistanceMultiplier.GetProperty());        
        }
    }

    protected override void UpdateCooldown()
    {
        if ((string)passiveTemperatureGain.GetProperty() == "None" && Mathf.Abs(_currentInternalTemperature) / _maxInternalTemperature < 0.01f)
        {
            _currentInternalTemperature = 0f;
        }
        else
        {
            float step = Time.deltaTime * Mathf.InverseLerp(_highTempCutoff, 0f, Mathf.Abs(_currentTemperature));
            if (_currentInternalTemperature > 0f)
            {
                _currentInternalTemperature -= step;
            }
            else if (_currentInternalTemperature < 0f)
            {
                _currentInternalTemperature += step;
            }
        }
    }

    private bool IsHeatingLocked()
    {
        return _waterCooling && SELocator.GetShipWaterResource().IsCoolingActive();
    }

    private bool IsCoolingLocked()
    {
        return _reactorHeat != null && _reactorHeat.IsOverloaded();
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