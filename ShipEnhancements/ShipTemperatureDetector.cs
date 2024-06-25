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
    private HazardVolume[] _heatVolumes;
    private float _currentTemperature;
    private bool _highTemperature = false;
    private float _damageDelay = 1.5f;
    private float _randDamageDelay;
    private float _delayStartTime;
    private float _tempMeterChargeLength = 360f;
    private float _tempMeterStartTime;

    private void Start()
    {
        ShipDamageController damageController = Locator.GetShipBody().GetComponent<ShipDamageController>();
        _currentTemperature = 0f;
        _shipHulls = damageController._shipHulls;
        _shipComponents = damageController._shipComponents;
        _delayStartTime = Time.time;
        _randDamageDelay = _damageDelay + UnityEngine.Random.Range(-1f, 1f);
        _tempMeterChargeLength /= ShipEnhancements.Instance.DamageSpeedMultiplier;
        _tempMeterStartTime = Time.time - _tempMeterChargeLength;
    }

    private void Update()
    {
        if (_activeZones.Count > 0)
        {
            if (!_highTemperature)
            {
                _highTemperature = true;
                _tempMeterStartTime = Time.time;
            }

            float totalTemperature = 0f;
            foreach (TemperatureZone zone in _activeZones)
            {
                totalTemperature += zone.GetTemperature();
            }
            _currentTemperature = totalTemperature /= _activeZones.Count;

            if (Time.time > _delayStartTime + _randDamageDelay)
            {
                _delayStartTime = Time.time;

                float timeMultiplier = Mathf.InverseLerp(_tempMeterStartTime, _tempMeterStartTime + _tempMeterChargeLength, Time.time) * Mathf.Abs(GetTemperatureRatio());

                float damageChance = 0f;
                damageChance = 0.05f * Mathf.Lerp(1f, 5f, timeMultiplier);
                if (UnityEngine.Random.value < damageChance / 2)
                {
                    ComponentTemperatureDamage();
                }
                else if (UnityEngine.Random.value < damageChance)
                {
                    HullTemperatureDamage();
                }
            }
        }
        else if (_highTemperature)
        {
            _highTemperature = false;
            _tempMeterStartTime = Time.time;
        }
    }

    private void HullTemperatureDamage()
    {
        ShipHull targetHull = _shipHulls[UnityEngine.Random.Range(0, _shipHulls.Length)];
        float damage = UnityEngine.Random.Range(0.03f, 0.15f) * ShipEnhancements.Instance.DamageMultiplier;
        targetHull._integrity = Mathf.Max(targetHull._integrity - damage, 0f);
        targetHull._damaged = true;
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
        ShipComponent[] enabledComponents = _shipComponents.Where(component => {
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