﻿using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipTemperatureDetector : TemperatureDetector
{
    private ShipHull[] _shipHulls;
    private ShipHull _lastDamagedHull;
    private ShipComponent[] _shipComponents;
    private ShockLayerController _shockLayerController;
    private float _damageDelay = 1.5f;
    private float _randDamageDelay;
    private float _delayStartTime;
    private bool _componentDamageNextTime = false;

    protected override bool UpdateTemperature => base.UpdateTemperature
        || (_shockLayerController.enabled && _shockLayerController._ruleset != null);

    protected override void Start()
    {
        base.Start();

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _shipHulls = SELocator.GetShipDamageController()._shipHulls;
        _shipComponents = SELocator.GetShipDamageController()._shipComponents;
        _shockLayerController = SELocator.GetShipTransform().GetComponentInChildren<ShockLayerController>();

        _delayStartTime = Time.time;
        _randDamageDelay = _damageDelay + UnityEngine.Random.Range(-1f, 1f);
        _internalTempMeterLength *= Mathf.Max(Mathf.Abs((float)temperatureResistanceMultiplier.GetProperty()), 1f);
    }

    private void UpdateTemperatureDamage()
    {
        if (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost())
        {
            if (Time.time > _delayStartTime + _randDamageDelay)
            {
                _delayStartTime = Time.time;
                _randDamageDelay = _damageDelay + UnityEngine.Random.Range(-1f, 1f);

                float timeMultiplier = Mathf.InverseLerp(0f, _internalTempMeterLength, Mathf.Abs(_internalTempMeter));
                float tempDamage = Mathf.Max((float)temperatureDamageMultiplier.GetProperty(), 0f);

                float damageChance = 0.05f * Mathf.Lerp(0f, 1f + (Mathf.InverseLerp(_highTempCutoff, 100f, Mathf.Abs(_currentTemperature)) * 2f), Mathf.Pow(timeMultiplier, 2));
                if ((bool)componentTemperatureDamage.GetProperty() && UnityEngine.Random.value
                    < damageChance * tempDamage / 8)
                {
                    _componentDamageNextTime = true;
                }
                if ((bool)hullTemperatureDamage.GetProperty() && UnityEngine.Random.value < damageChance)
                {
                    HullTemperatureDamage();
                }
            }
            else if (_componentDamageNextTime && Time.time > _delayStartTime + _randDamageDelay / 2)
            {
                _componentDamageNextTime = false;
                ComponentTemperatureDamage();
            }
        }
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
                    _internalTempMeter = Mathf.Clamp(_internalTempMeter + Time.deltaTime
                        * ((multiplier * scalar) + additiveMultiplier) * Mathf.Sign(resistance),
                        -_internalTempMeterLength, _internalTempMeterLength);
                }
            }
        }

        UpdateTemperatureDamage();
    }

    private void HullTemperatureDamage()
    {
        ShipHull[] validHulls;

        if (_lastDamagedHull != null)
        {
            validHulls = _shipHulls.Where(hull =>
            {
                return hull != _lastDamagedHull;
            }).ToArray();
        }
        else
        {
            validHulls = _shipHulls;
        }

        ShipHull targetHull = validHulls[UnityEngine.Random.Range(0, validHulls.Length)];
        float tempDamage = Mathf.Max((float)temperatureDamageMultiplier.GetProperty(), 0f);
        float damage = UnityEngine.Random.Range(0.03f, 0.15f) * tempDamage;
        bool wasDamaged = targetHull.isDamaged;
        ApplyHullTempDamage(targetHull, damage);

        if (ShipEnhancements.InMultiplayer && ShipEnhancements.QSBAPI.GetIsHost())
        {
            ShipEnhancements.QSBInteraction.SetHullDamaged(targetHull, !wasDamaged);
        }
    }

    public void ApplyHullTempDamage(ShipHull targetHull, float damage)
    {
        targetHull._damaged = true;
        targetHull._integrity = Mathf.Max(targetHull._integrity - damage, 0f);
        var eventDelegate1 = (MulticastDelegate)typeof(ShipHull).GetField("OnDamaged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(targetHull);
        if (eventDelegate1 != null)
        {
            foreach (var handler in eventDelegate1.GetInvocationList())
            {
                handler.Method.Invoke(handler.Target, [targetHull]);
            }
        }
        if (targetHull._damageEffect != null)
        {
            targetHull._damageEffect.SetEffectBlend(1f - targetHull._integrity);
        }
        _lastDamagedHull = targetHull;

        if (targetHull._integrity <= 0f && targetHull.shipModule is ShipDetachableModule
            && (!(bool)preventSystemFailure.GetProperty() || targetHull.section == ShipHull.Section.Front))
        {
            ErnestoDetectiveController.ItWasTemperatureDamage(_internalTempMeter >= 0f);
        }
    }

    private void ComponentTemperatureDamage()
    {
        ShipComponent[] enabledComponents = _shipComponents.Where(component =>
        {
            return component.repairFraction == 1f && !component.isDamaged;
        }).ToArray();
        ShipComponent targetComponent = enabledComponents[UnityEngine.Random.Range(0, enabledComponents.Length)];
        ApplyComponentTempDamage(targetComponent);
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

    protected override void UpdateHighTemperature()
    {
        if ((float)temperatureResistanceMultiplier.GetProperty() == 0)
        {
            SELocator.GetShipDamageController().Explode();
        }
    }

    protected override void UpdateInternalTemperature()
    {
        float cutoff = Mathf.Abs(_internalTempMeterLength * GetTemperatureRatio());

        bool sameSide = _internalTempMeter < 0 == _currentTemperature < 0;

        if ((float)temperatureResistanceMultiplier.GetProperty() < 0f)
        {
            sameSide = !sameSide;
        }

        if (sameSide && Mathf.Abs(_internalTempMeter) < cutoff)
        {
            _internalTempMeter += Time.deltaTime * 3f * Mathf.InverseLerp(_highTempCutoff, 100f, Mathf.Abs(_currentTemperature)) * Mathf.Sign(GetTemperatureRatio())
                * Mathf.Sign((float)temperatureResistanceMultiplier.GetProperty());
        }
        else if (!sameSide)
        {
            _internalTempMeter += Time.deltaTime * Mathf.Sign(GetTemperatureRatio()) * Mathf.Sign((float)temperatureResistanceMultiplier.GetProperty());
        }
    }

    protected override void UpdateCooldown()
    {
        if (!(bool)faultyHeatRegulators.GetProperty() && Mathf.Abs(_internalTempMeter) / _internalTempMeterLength < 0.01f)
        {
            _internalTempMeter = 0f;
        }
        else
        {
            float step = Time.deltaTime * Mathf.InverseLerp(_highTempCutoff, 0f, Mathf.Abs(_currentTemperature));
            if (_internalTempMeter > 0f)
            {
                _internalTempMeter -= step;
            }
            else if (_internalTempMeter < 0f)
            {
                _internalTempMeter += step;
            }
        }
    }

    public void ApplyComponentTempDamage(ShipComponent component)
    {
        if (component is ShipReactorComponent && !component.isDamaged)
        {
            ErnestoDetectiveController.SetReactorCause("temperature" + (_internalTempMeter >= 0f ? "_hot" : "_cold"));
        }
        component.SetDamaged(true);
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