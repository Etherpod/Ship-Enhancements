using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipTemperatureDetector : MonoBehaviour
{
    private List<TemperatureZone> _activeZones = [];
    private ShipHull[] _shipHulls;
    private ShipHull _lastDamagedHull;
    private ShipComponent[] _shipComponents;
    private float _currentTemperature;
    private float _highTempCutoff = 50f;
    private bool _highTemperature = false;
    private float _damageDelay = 1.5f;
    private float _randDamageDelay;
    private float _delayStartTime;
    private float _tempMeterChargeLength = 180f;
    private float _tempMeter;
    private bool _componentDamageNextTime = false;
    private bool _shipDestroyed = false;

    private void Start()
    {
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _shipHulls = SELocator.GetShipDamageController()._shipHulls;
        _shipComponents = SELocator.GetShipDamageController()._shipComponents;

        _currentTemperature = 0f;
        _delayStartTime = Time.time;
        _randDamageDelay = _damageDelay + UnityEngine.Random.Range(-1f, 1f);
        _tempMeterChargeLength *= (float)temperatureResistanceMultiplier.GetProperty();
    }

    private void Update()
    {
        if (_activeZones.Count > 0 && !_shipDestroyed)
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
                }
            }
            else
            {
                if (Mathf.Abs(_tempMeter) < _tempMeterChargeLength)
                {
                    _tempMeter += Time.deltaTime * 3f * Mathf.InverseLerp(_highTempCutoff, 100f, Mathf.Abs(_currentTemperature)) * Mathf.Sign(GetTemperatureRatio());
                }

                if ((GetShipTemperatureRatio() - 0.5f < 0) != (GetTemperatureRatio() < 0))
                {
                    return;
                }

                if (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost())
                {
                    if (Time.time > _delayStartTime + _randDamageDelay)
                    {
                        _delayStartTime = Time.time;
                        _randDamageDelay = _damageDelay + UnityEngine.Random.Range(-1f, 1f);

                        float timeMultiplier = Mathf.InverseLerp(0f, _tempMeterChargeLength, Mathf.Abs(_tempMeter));
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
        }

        if (_highTemperature && (_activeZones.Count == 0 || Mathf.Abs(_currentTemperature) < _highTempCutoff))
        {
            _highTemperature = false;
        }
        if (!_highTemperature)
        {
            if (Mathf.Abs(_tempMeter) / _tempMeterChargeLength < 0.01f)
            {
                _tempMeter = 0f;
            }
            else
            {
                float step = Time.deltaTime * Mathf.InverseLerp(_highTempCutoff, 0f, Mathf.Abs(_currentTemperature));
                if (_tempMeter > 0f)
                {
                    _tempMeter -= step;
                }
                else if (_tempMeter < 0f)
                {
                    _tempMeter += step;
                }
            }
        }
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
        ApplyHullTempDamage(targetHull, damage);

        if (ShipEnhancements.InMultiplayer && ShipEnhancements.QSBAPI.GetIsHost())
        {
            ShipEnhancements.QSBInteraction.SetHullDamaged(targetHull);
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

    public void ApplyComponentTempDamage(ShipComponent component)
    {
        component.SetDamaged(true);
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
        return Mathf.InverseLerp(-_tempMeterChargeLength, _tempMeterChargeLength, _tempMeter);
    }

    public bool IsHighTemperature()
    {
        return _highTemperature;
    }

    public float GetShipTempMeter()
    {
        return _tempMeter;
    }

    public void SetShipTempMeter(float time)
    {
        _tempMeter = Mathf.Clamp(time, -_tempMeterChargeLength, _tempMeterChargeLength);
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

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
    }
}