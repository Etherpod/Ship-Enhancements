using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ShipEnhancements;

public class ShipTemperatureDamage : MonoBehaviour
{
    private HazardDetector _hazardDetector;
    private ShipHull[] _shipHulls;
    private ShipComponent[] _shipComponents;
    private HazardVolume[] _heatVolumes;
    private bool _highTemperature = false;
    private float _damageDelay = 1.5f;
    private float _randDamageDelay;
    private float _delayStartTime;
    private float _tempMeterChargeLength = 360f;
    private float _tempMeterStartTime;

    private void Start()
    {
        _hazardDetector = Locator.GetShipDetector().GetComponent<HazardDetector>();
        ShipDamageController damageController = Locator.GetShipBody().GetComponent<ShipDamageController>();
        _shipHulls = damageController._shipHulls;
        _shipComponents = damageController._shipComponents;
        _delayStartTime = Time.time;
        _randDamageDelay = _damageDelay + UnityEngine.Random.Range(-1f, 1f);
        _tempMeterChargeLength /= ShipEnhancements.Instance.DamageSpeedMultiplier;
        _tempMeterStartTime = Time.time - _tempMeterChargeLength;
    }

    private void Update()
    {
        if ((Locator.GetAstroObject(AstroObject.Name.Sun).transform.position - transform.position).sqrMagnitude < 3000 * 3000)
        {
            if (!_highTemperature)
            {
                _highTemperature = true;
                _tempMeterStartTime = Time.time;
            }

            if (Time.time > _delayStartTime + _randDamageDelay)
            {
                _delayStartTime = Time.time;

                float num = Mathf.InverseLerp(_tempMeterStartTime, _tempMeterStartTime + _tempMeterChargeLength, Time.time);

                float damageChance = 0f;
                foreach (HazardVolume volume in _hazardDetector._activeVolumes)
                {
                    if (volume.GetHazardType() == HazardVolume.HazardType.HEAT)
                    {
                        damageChance += volume.GetDamagePerSecond(_hazardDetector) / 100f;
                    }
                }
                damageChance = 0.05f * Mathf.Lerp(1f, 5f, num);
                damageChance *= ShipEnhancements.Instance.DamageMultiplier;
                if (UnityEngine.Random.value < damageChance / 2)
                {
                    ComponentTemperatureDamage();
                }
                else if (UnityEngine.Random.value < damageChance)
                {
                    HullTemperatureDamage();
                }

                //if (_hazardDetector.InHazardType(HazardVolume.HazardType.HEAT))
                /*if ((Locator.GetAstroObject(AstroObject.Name.Sun).transform.position - transform.position).sqrMagnitude < 3000 * 3000)
                {
                    if (!_highTemperature)
                    {
                        _highTemperature = true;
                        _tempMeterStartTime = Time.time;
                    }

                    float num = Mathf.InverseLerp(_tempMeterStartTime, _tempMeterStartTime + _tempMeterChargeLength, Time.time);

                    float damageChance = 0f;
                    foreach (HazardVolume volume in _hazardDetector._activeVolumes)
                    {
                        if (volume.GetHazardType() == HazardVolume.HazardType.HEAT)
                        {
                            damageChance += volume.GetDamagePerSecond(_hazardDetector) / 100f;
                        }
                    }
                    damageChance = 0.05f * Mathf.Lerp(1f, 5f, num);
                    damageChance *= ShipEnhancements.Instance.DamageMultiplier;
                    if (UnityEngine.Random.value < damageChance / 2)
                    {
                        ComponentTemperatureDamage();
                    }
                    else if (UnityEngine.Random.value < damageChance)
                    {
                        HullTemperatureDamage();
                    }
                }*/
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
            return component._repairReceiver._repairDistance == 0 && !component.isDamaged;
        }).ToArray();
        ShipComponent targetComponent = enabledComponents[UnityEngine.Random.Range(0, enabledComponents.Length)];
        targetComponent.SetDamaged(true);
    }

    public float GetTemperatureRatio()
    {
        return Mathf.InverseLerp(_tempMeterStartTime, _tempMeterStartTime + _tempMeterChargeLength, Time.time);
    }

    public bool IsHighTemperature()
    {
        return _highTemperature;
    }
}