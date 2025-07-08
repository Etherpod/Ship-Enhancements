using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipTemperatureDetector : TemperatureDetector
{
    private ShockLayerController _shockLayerController;

    protected override void Start()
    {
        base.Start();

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        _shockLayerController = SELocator.GetShipTransform().GetComponentInChildren<ShockLayerController>();
        _maxInternalTemperature *= Mathf.Max(Mathf.Abs((float)temperatureResistanceMultiplier.GetProperty()), 1f);
    }

    protected override void Update()
    {
        base.Update();

        if ((bool)faultyHeatRegulators.GetProperty())
        {
            float resistance = (float)temperatureResistanceMultiplier.GetProperty();
            float multiplier = Mathf.InverseLerp(-_highTempCutoff / 4f, 0f, _currentTemperature);
            float additiveMultiplier = 0f;
            if (SELocator.GetShipDamageController().IsReactorCritical())
            {
                additiveMultiplier = 1.5f;
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
                    float scalar = 1 + (1f * Mathf.InverseLerp(_highTempCutoff, 0f, Mathf.Abs(_currentTemperature)));
                    _currentInternalTemperature = Mathf.Clamp(_currentInternalTemperature + Time.deltaTime
                        * ((multiplier * scalar) + additiveMultiplier) * Mathf.Sign(resistance),
                        -_maxInternalTemperature, _maxInternalTemperature);
                }
            }
        }
    }

    protected override bool CanUpdateTemperature()
    {
        return base.CanUpdateTemperature() || (_shockLayerController.enabled 
            && _shockLayerController._ruleset != null);
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
        float cutoff = Mathf.Abs(_maxInternalTemperature * GetTemperatureRatio());

        bool sameSide = _currentInternalTemperature < 0 == _currentTemperature < 0;

        if ((float)temperatureResistanceMultiplier.GetProperty() < 0f)
        {
            sameSide = !sameSide;
        }

        if (sameSide && Mathf.Abs(_currentInternalTemperature) < cutoff)
        {
            _currentInternalTemperature += Time.deltaTime * 3f * Mathf.InverseLerp(_highTempCutoff, 100f, Mathf.Abs(_currentTemperature)) * Mathf.Sign(GetTemperatureRatio())
                * Mathf.Sign((float)temperatureResistanceMultiplier.GetProperty());
        }
        else if (!sameSide)
        {
            _currentInternalTemperature += Time.deltaTime * Mathf.Sign(GetTemperatureRatio()) * Mathf.Sign((float)temperatureResistanceMultiplier.GetProperty());
        }
    }

    protected override void UpdateCooldown()
    {
        if (!(bool)faultyHeatRegulators.GetProperty() && Mathf.Abs(_currentInternalTemperature) / _maxInternalTemperature < 0.01f)
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

    private void OnShipSystemFailure()
    {
        enabled = false;
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}