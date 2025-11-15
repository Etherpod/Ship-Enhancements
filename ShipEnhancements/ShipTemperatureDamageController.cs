using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipTemperatureDamageController : MonoBehaviour
{
    private ShipTemperatureDetector _detector;
    private ReactorHeatController _reactorHeat;
    private ShipHull[] _shipHulls;
    private ShipHull _lastDamagedHull;
    private ShipComponent[] _shipComponents;
    private float _damageDelay = 1.5f;
    private float _randDamageDelay;
    private float _delayStartTime;
    private bool _componentDamageNextTime = false;

    private float _initialReactorArrowAngle;
    private float _maxReactorArrowAngle;
    private (float min, float max) _initialCountdownRange;
    private (float min, float max) _maxCountdownRange;

    private float _damageSpeedMultiplier = 1f;
    private float _maxDamageSpeedMultiplier = 0.4f;
    private float _damageMultiplier = 1f;
    private float _maxDamageMultiplier = 0.75f;

    private void Start()
    {
        _detector = SELocator.GetShipDetector().GetComponent<ShipTemperatureDetector>();
        _reactorHeat = SELocator.GetShipDamageController()._shipReactorComponent.GetComponent<ReactorHeatController>();
        _shipHulls = SELocator.GetShipDamageController()._shipHulls;
        _shipComponents = SELocator.GetShipDamageController()._shipComponents;

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _delayStartTime = Time.time;
        _randDamageDelay = _damageDelay + UnityEngine.Random.Range(-1f, 1f);

        /*_initialReactorArrowAngle = _reactor._startArrowRotation;
        _initialCountdownRange = (_reactor._minCountdown, _reactor._maxCountdown);
        float diff = _reactor._endArrowRotation - _reactor._startArrowRotation;
        float minReactorLength = Mathf.Lerp(1f, 4f, (float)temperatureDifficulty.GetProperty());
        _maxReactorArrowAngle = _reactor._endArrowRotation - diff / minReactorLength;
        _maxCountdownRange = (_reactor._minCountdown / minReactorLength, _reactor._maxCountdown / minReactorLength);*/

        _maxDamageSpeedMultiplier = Mathf.Lerp(0.8f, 0.4f, (float)temperatureDifficulty.GetProperty());
        _maxDamageMultiplier = Mathf.Lerp(0.6f, 1f, (float)temperatureDifficulty.GetProperty());
    }

    private void Update()
    {
        if ((float)temperatureDifficulty.GetProperty() <= 0f) return;

        if (_detector.GetInternalTemperatureRatio() > 0f)
        {
            UpdateReactor();
        }

        UpdateTemperatureDamage();
    }

    private void UpdateReactor()
    {
        float tempLerp = _detector.GetInternalTemperatureRatio();
        float multiplier = Mathf.Lerp(0.25f, 0.75f, (float)temperatureDifficulty.GetProperty());
        _reactorHeat.SetAdditiveHeat(tempLerp * multiplier);
        /*_reactor._startArrowRotation = Mathf.LerpAngle(_initialReactorArrowAngle, _maxReactorArrowAngle, tempLerp);
        _reactor._minCountdown = Mathf.Lerp(_initialCountdownRange.min, _maxCountdownRange.min, tempLerp);
        _reactor._maxCountdown = Mathf.Lerp(_initialCountdownRange.max, _maxCountdownRange.max, tempLerp);

        if (!_reactor.enabled)
        {
            _reactor._timerArrow.localEulerAngles = new Vector3(_reactor._startArrowRotation, 0f, 0f);
        }*/
    }

    private void UpdateTemperatureDamage()
    {
        float internalTemp = _detector.GetInternalTemperatureRatio();

        if (internalTemp > 0f)
        {
            if (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost())
            {
                if (Time.time > _delayStartTime + _randDamageDelay)
                {
                    _delayStartTime = Time.time;
                    _randDamageDelay = _damageDelay + UnityEngine.Random.Range(-1f, 1f);

                    float timeMultiplier = Mathf.Abs(internalTemp);
                    float tempLerp = Mathf.InverseLerp(_detector.GetHighTempCutoff(), 100f, Mathf.Abs(_detector.GetCurrentTemperature()));
                    //float tempDamage = Mathf.Max((float)temperatureDamageMultiplier.GetProperty(), 0f);
                    float tempDamage = (float)temperatureDifficulty.GetProperty() * 2f;

                    float damageChance = 0.05f * Mathf.Lerp(0f, 1f + (tempLerp * 2f), Mathf.Pow(timeMultiplier, 2));
                    if (UnityEngine.Random.value < damageChance * tempDamage / 8)
                    {
                        _componentDamageNextTime = true;
                    }
                    if (UnityEngine.Random.value < damageChance)
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
        else if (internalTemp < 0f)
        {
            float tempLerp = Mathf.Abs(internalTemp);
            _damageMultiplier = Mathf.Lerp(1f, _maxDamageMultiplier, tempLerp);
            _damageSpeedMultiplier = Mathf.Lerp(1f, _maxDamageSpeedMultiplier, tempLerp);
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
        //float tempDamage = Mathf.Max((float)temperatureDamageMultiplier.GetProperty(), 0f);
        float tempDamage = (float)temperatureDifficulty.GetProperty() * 2f;
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
            ErnestoDetectiveController.ItWasTemperatureDamage(_detector.GetInternalTemperatureRatio() >= 0f);
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

    public void ApplyComponentTempDamage(ShipComponent component)
    {
        if (component is ShipReactorComponent && !component.isDamaged)
        {
            ErnestoDetectiveController.SetReactorCause("temperature" + (_detector.GetInternalTemperatureRatio() >= 0f ? "_hot" : "_cold"));
        }
        component.SetDamaged(true);
    }

    public (float damageMultiplier, float speedMultiplier) GetDamageMultipliers()
    {
        return (_damageMultiplier, _damageSpeedMultiplier);
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
