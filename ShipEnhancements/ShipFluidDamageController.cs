using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipFluidDamageController : MonoBehaviour
{
    private List<StaticFluidDetector> _moduleDetectors = [];
    private Dictionary<FluidVolume.Type, float> _damageFluids = [];
    private List<ShipModule> _trackedModules = [];
    private float _minDamageDelay = 0.3f;
    private float _maxDamageDelay = 1f;
    private float _damageDelay;
    private float _currentDamagePercent;

    private void Awake()
    {
        GameObject cabin = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/Detectors/CabinFluidDetector.prefab");
        GameObject cockpit = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/Detectors/CockpitFluidDetector.prefab");
        GameObject engine = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/Detectors/EngineFluidDetector.prefab");
        GameObject supplies = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/Detectors/SuppliesFluidDetector.prefab");
        GameObject landingGear = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/Detectors/LandingGearFluidDetector.prefab");
        _moduleDetectors.Add(Instantiate(cabin, transform.parent.Find("Module_Cabin")).GetComponent<StaticFluidDetector>());
        _moduleDetectors.Add(Instantiate(cockpit, transform.parent.Find("Module_Cockpit")).GetComponent<StaticFluidDetector>());
        _moduleDetectors.Add(Instantiate(engine, transform.parent.Find("Module_Engine")).GetComponent<StaticFluidDetector>());
        _moduleDetectors.Add(Instantiate(supplies, transform.parent.Find("Module_Supplies")).GetComponent<StaticFluidDetector>());
        _moduleDetectors.Add(Instantiate(landingGear, transform.parent.Find("Module_LandingGear")).GetComponent<StaticFluidDetector>());

        foreach (StaticFluidDetector detector in _moduleDetectors)
        {
            detector.OnEnterFluid += (vol) => OnEnterFluid(vol, detector);
            detector.OnExitFluid += (vol) => OnExitFluid(vol, detector);
        }

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        if ((float)waterDamage.GetProperty() > 0f)
        {
            _damageFluids.Add(FluidVolume.Type.WATER, (float)waterDamage.GetProperty());
            _damageFluids.Add(FluidVolume.Type.GEYSER, (float)waterDamage.GetProperty());
        }
        if ((float)sandDamage.GetProperty() > 0f)
        {
            _damageFluids.Add(FluidVolume.Type.SAND, (float)sandDamage.GetProperty());
        }

        _damageDelay = 1f;
    }

    private void Start()
    {
        if ((ShipEnhancements.InMultiplayer && !ShipEnhancements.QSBAPI.GetIsHost()) || (float)shipDamageMultiplier.GetProperty() <= 0f)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (_trackedModules.Count == 0) return;

        if (_damageDelay > 0f)
        {
            _damageDelay -= Time.deltaTime * (1 + _trackedModules.Count * 0.5f);
        }
        else
        {
            ShipModule module = _trackedModules[Random.Range(0, _trackedModules.Count)];
            ShipHull hull = module._hulls[Random.Range(0, module._hulls.Length)];
            RandomDamageToModule(hull, _currentDamagePercent);

            float mult = Mathf.Lerp(10f, 1f, _currentDamagePercent);
            _damageDelay = Random.Range(_minDamageDelay * mult, _maxDamageDelay * mult);
        }
    }

    private void RandomDamageToModule(ShipHull targetHull, float damagePercent)
    {
        if ((ShipEnhancements.InMultiplayer && !ShipEnhancements.QSBAPI.GetIsHost()) || (float)shipDamageMultiplier.GetProperty() <= 0f) return;

        ShipComponent[] components = targetHull._components
            .Where((component) => component.repairFraction == 1f && !component.isDamaged).ToArray();

        if (components.Length > 0 && Random.value < 0.1f + (damagePercent * 0.3f))
        {
            components[Random.Range(0, components.Length)].SetDamaged(true);
        }
        else
        {
            bool wasDamaged = targetHull._damaged;
            targetHull._damaged = true;
            float mult = Mathf.Lerp(3f, 1f, damagePercent);
            targetHull._integrity = Mathf.Max(0f, targetHull._integrity - Random.Range(0.05f * mult, 0.15f * mult) 
                * (float)shipDamageMultiplier.GetProperty());
            var eventDelegate1 = (System.MulticastDelegate)typeof(ShipHull).GetField("OnDamaged",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public).GetValue(targetHull);
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

            if (ShipEnhancements.InMultiplayer)
            {
                ShipEnhancements.QSBInteraction.SetHullDamaged(targetHull, !wasDamaged);
            }
        }
    }

    private void OnEnterFluid(FluidVolume vol, StaticFluidDetector detector)
    {
        if (!_damageFluids.Keys.Contains(vol.GetFluidType())) return;

        ShipModule module = detector.GetComponentInParent<ShipModule>();
        if (module != null && !_trackedModules.Contains(module))
        {
            _trackedModules.Add(module);
        }

        _currentDamagePercent = _damageFluids[vol.GetFluidType()];
        if (_currentDamagePercent >= 1f)
        {
            SELocator.GetShipDamageController().Explode();
        }
    }

    private void OnExitFluid(FluidVolume vol, StaticFluidDetector detector)
    {
        if (!_damageFluids.Keys.Contains(vol.GetFluidType())) return;

        ShipModule module = detector.GetComponentInParent<ShipModule>();
        if (module != null && _trackedModules.Contains(module))
        {
            _trackedModules.Remove(module);
        }
    }

    private void OnShipSystemFailure()
    {
        enabled = false;
    }

    private void OnDestroy()
    {
        foreach (StaticFluidDetector detector in _moduleDetectors)
        {
            detector.OnEnterFluid -= (vol) => OnEnterFluid(vol, detector);
            detector.OnExitFluid -= (vol) => OnExitFluid(vol, detector);
        }

        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
