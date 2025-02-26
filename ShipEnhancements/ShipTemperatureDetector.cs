using System;
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

    protected override void Start()
    {
        base.Start();

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _shipHulls = SELocator.GetShipDamageController()._shipHulls;
        _shipComponents = SELocator.GetShipDamageController()._shipComponents;
        _shockLayerController = SELocator.GetShipTransform().GetComponentInChildren<ShockLayerController>();

        _delayStartTime = Time.time;
        _randDamageDelay = _damageDelay + UnityEngine.Random.Range(-1f, 1f);
        _internalTempMeterLength *= (float)temperatureResistanceMultiplier.GetProperty();
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
            float multiplier = Mathf.InverseLerp(-_highTempCutoff / 4f, 0f, _currentTemperature);
            float scalar = 1 + (1f * Mathf.InverseLerp(_highTempCutoff, 0f, Mathf.Abs(_currentTemperature)));
            _internalTempMeter = Mathf.Clamp(_internalTempMeter + (Time.deltaTime * multiplier * scalar), -_internalTempMeterLength, _internalTempMeterLength);
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
            float shockSpeedPercent;
            if (_shockLayerController._ruleset.GetShockLayerType() == ShockLayerRuleset.ShockType.Atmospheric)
            {
                Vector3 toCenter = _shockLayerController._ruleset.GetRadialCenter().position - _shockLayerController._owRigidbody.GetPosition();
                float centerDist = toCenter.magnitude;
                float radiusMultiplier = 1f - Mathf.InverseLerp(_shockLayerController._ruleset.GetInnerRadius(),
                    _shockLayerController._ruleset.GetOuterRadius(), centerDist);

                Vector3 relativeFluidVelocity = _shockLayerController._fluidDetector.GetRelativeFluidVelocity();
                float velocityMagnitude = relativeFluidVelocity.magnitude;
                shockSpeedPercent = Mathf.InverseLerp(_shockLayerController._ruleset.GetMinShockSpeed(),
                    _shockLayerController._ruleset.GetMaxShockSpeed(), velocityMagnitude);
                shockSpeedPercent *= radiusMultiplier;
            }
            else
            {
                Vector3 toCenter = _shockLayerController._ruleset.GetRadialCenter().position - _shockLayerController._owRigidbody.GetPosition();
                float centerDist = toCenter.magnitude;
                float radiusMultiplier = 1f - Mathf.InverseLerp(_shockLayerController._ruleset.GetInnerRadius(),
                    _shockLayerController._ruleset.GetOuterRadius(), centerDist);
                shockSpeedPercent = radiusMultiplier;
            }

            totalTemperature += Mathf.Lerp(0f, 65f, shockSpeedPercent);
        }

        return Mathf.Clamp(totalTemperature, -100f, 100f);
    }

    protected override bool RoundInternalTemperature()
    {
        return (bool)faultyHeatRegulators.GetProperty();
    }

    public void ApplyComponentTempDamage(ShipComponent component)
    {
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