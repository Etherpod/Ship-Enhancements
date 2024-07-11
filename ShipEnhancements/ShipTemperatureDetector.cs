using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ShipEnhancements;

public class ShipTemperatureDetector : MonoBehaviour
{
    private List<TemperatureZone> _activeZones = [];
    private ShipHull[] _shipHulls;
    private ShipComponent[] _shipComponents;
    private float _currentTemperature;
    private float _highTempCutoff = 50f;
    private bool _highTemperature = false;
    private float _damageDelay = 1.5f;
    private float _randDamageDelay;
    private float _delayStartTime;
    private float _tempMeterChargeLength = 360f;
    private float _tempMeterStartTime;
    private float _cooldownStartTime;
    private float _cooldownTime;
    private bool _componentDamageNextTime = false;

    private void Start()
    {
        ShipDamageController damageController = Locator.GetShipBody().GetComponent<ShipDamageController>();
        _shipHulls = damageController._shipHulls;
        _shipComponents = damageController._shipComponents;

        _currentTemperature = 0f;
        _delayStartTime = Time.time;
        _randDamageDelay = _damageDelay + UnityEngine.Random.Range(-1f, 1f);
        _tempMeterChargeLength *= ShipEnhancements.Instance.TemperatureResistanceMultiplier;
        _tempMeterStartTime = Time.time - _tempMeterChargeLength;
    }

    private void Update()
    {
        if (_activeZones.Count > 0)
        {
            float totalTemperature = 0f;
            foreach (TemperatureZone zone in _activeZones)
            {
                float temp = zone.GetTemperature();
                totalTemperature += temp;
            }
            _currentTemperature = Mathf.Clamp(totalTemperature, -100f, 100f);

            if (!_highTemperature)
            {
                if (Mathf.Abs(_currentTemperature) > _highTempCutoff)
                {
                    _highTemperature = true;
                    float ratio = Mathf.InverseLerp(_cooldownStartTime, _cooldownStartTime + _cooldownTime, Time.time);
                    _tempMeterStartTime = Time.time - (_cooldownTime * (1 - ratio));
                }
            }
            else
            {
                if (Time.time > _delayStartTime + _randDamageDelay)
                {
                    _delayStartTime = Time.time;
                    _randDamageDelay = _damageDelay + UnityEngine.Random.Range(-1f, 1f);

                    float timeMultiplier = Mathf.InverseLerp(_tempMeterStartTime, _tempMeterStartTime + _tempMeterChargeLength * (1 - Mathf.Abs(GetTemperatureRatio()) / 2), Time.time);

                    float damageChance = 0.05f * Mathf.LerpUnclamped(0f, 1f + (Mathf.InverseLerp(_highTempCutoff, 100f, Mathf.Abs(_currentTemperature)) * 2f), timeMultiplier);
                    if (ShipEnhancements.Instance.ComponentTemperatureDamage && UnityEngine.Random.value 
                        < damageChance * ShipEnhancements.Instance.TemperatureDamageMultiplier / 8)
                    {
                        _componentDamageNextTime = true;
                    }
                    if (ShipEnhancements.Instance.HullTemperatureDamage && UnityEngine.Random.value < damageChance)
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

        if (_highTemperature && (_activeZones.Count == 0 || Mathf.Abs(_currentTemperature) < _highTempCutoff))
        {
            _highTemperature = false;
            _cooldownStartTime = Time.time;
            _cooldownTime = Mathf.Max(_tempMeterChargeLength - ((_tempMeterStartTime + _tempMeterChargeLength) - Time.time), 0f);
        }
    }

    private void HullTemperatureDamage()
    {
        ShipHull targetHull = _shipHulls[UnityEngine.Random.Range(0, _shipHulls.Length)];

        targetHull._damaged = true;
        float damage = UnityEngine.Random.Range(0.03f, 0.15f) * ShipEnhancements.Instance.TemperatureDamageMultiplier;
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
    }

    private void ComponentTemperatureDamage()
    {
        ShipComponent[] enabledComponents = _shipComponents.Where(component =>
        {
            return component.repairFraction == 1f && !component.isDamaged;
        }).ToArray();
        ShipComponent targetComponent = enabledComponents[UnityEngine.Random.Range(0, enabledComponents.Length)];
        targetComponent.SetDamaged(true);
    }

    public float GetTemperatureRatio()
    {
        if (_currentTemperature > 0)
        {
            return Mathf.InverseLerp(0f, 100f, _currentTemperature);
        }
        else
        {
            return -Mathf.InverseLerp(0f, -100f, _currentTemperature);
        }
    }

    public float GetShipTemperatureRatio()
    {
        float ratio;
        if (!_highTemperature)
        {
            ratio = Mathf.InverseLerp(_cooldownStartTime, _cooldownStartTime + _cooldownTime, Time.time);
        }
        ratio = Mathf.InverseLerp(_tempMeterStartTime, _tempMeterStartTime + _tempMeterChargeLength * (1 - Mathf.Abs(GetTemperatureRatio()) / 2), Time.time);
        if (_currentTemperature < 0)
        {
            ratio *= -1f;
        }
        return ratio;
    }

    public bool IsHighTemperature()
    {
        return _highTemperature;
    }

    public void AddZone(TemperatureZone zone)
    {
        if (!_activeZones.Contains(zone))
        {
            _activeZones.Add(zone);
        }
    }

    public void RemoveZone(TemperatureZone zone)
    {
        if (_activeZones.Contains(zone))
        {
            _activeZones.Remove(zone);
        }
    }
}