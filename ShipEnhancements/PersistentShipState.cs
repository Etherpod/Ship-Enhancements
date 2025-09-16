using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class PersistentShipState : MonoBehaviour
{
    private bool _everInitialized = false;

    private float _shipFuel;
    private float _shipOxygen;
    private float _shipWater;

    private Dictionary<string, float> _hullIntegrities = [];
    private Dictionary<string, float> _componentIntegrities = [];

    private bool _headlightsOn;

    private CockpitButtonPanel.ButtonStates _buttonStates;

    private void Start()
    {
        LoadManager.OnStartSceneLoad += (scene, loadScene) =>
        {
            if (scene == OWScene.SolarSystem)
            {
                SaveState();
            }
        };

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene == OWScene.SolarSystem)
            {
                LoadState();
            }
        };
    }

    private void SaveState()
    {
        _shipFuel = SELocator.GetShipResources()._currentFuel;
        _shipOxygen = SELocator.GetShipResources()._currentOxygen;
        _shipWater = SELocator.GetShipWaterResource()?.GetWater() ?? 0f;

        _hullIntegrities.Clear();
        _componentIntegrities.Clear();

        ShipHull[] hulls = SELocator.GetShipDamageController()._shipHulls;
        foreach (var hull in hulls)
        {
            _hullIntegrities.Add(hull.hullName.ToString(), hull.integrity);
        }

        ShipComponent[] components = SELocator.GetShipDamageController()._shipComponents;
        foreach (var comp in components)
        {
            _componentIntegrities.Add(comp._componentName.ToString(), comp._repairFraction);
        }

        _headlightsOn = FindObjectOfType<ShipCockpitController>()._externalLightsOn;
        _buttonStates = SELocator.GetButtonPanel()?.GetButtonStates() ?? new();
    }

    private void LoadState()
    {
        if (!_everInitialized)
        {
            _everInitialized = true;
            return;
        }

        if (!(bool)persistentShipState.GetProperty()
            || (ShipEnhancements.InMultiplayer && !ShipEnhancements.QSBAPI.GetIsHost()))
        {
            return;
        }

        ShipEnhancements.Instance.ModHelper.Events.Unity.RunWhen(() => Locator._shipBody != null, () =>
        {
            SELocator.GetShipResources()._currentFuel = _shipFuel;
            SELocator.GetShipResources()._currentOxygen = _shipOxygen;
            SELocator.GetShipWaterResource()?.SetWater(_shipWater);

            ShipHull[] hulls = SELocator.GetShipDamageController()._shipHulls;
            foreach (var hull in hulls)
            {
                if (_hullIntegrities.TryGetValue(hull.hullName.ToString(), out float integrity))
                {
                    SetHullIntegrity(hull, integrity);
                }
            }

            ShipComponent[] components = SELocator.GetShipDamageController()._shipComponents;
            foreach (var comp in components)
            {
                if (_componentIntegrities.TryGetValue(comp._componentName.ToString(), out float integrity))
                {
                    if (integrity < 1f && comp._repairReceiver.repairDistance > 0f)
                    {
                        comp.SetDamaged(true);
                        comp._repairFraction = integrity;
                        if (comp._damageEffect)
                        {
                            comp._damageEffect.SetEffectBlend(1f - comp._repairFraction);
                        }
                    }
                }
            }

            SELocator.GetShipBody().GetComponentInChildren<ShipCockpitController>().SetEnableShipLights(_headlightsOn, false);
            SELocator.GetButtonPanel()?.SetButtonStates(_buttonStates);
        });
    }

    private void SetHullIntegrity(ShipHull hull, float integrity)
    {
        if (integrity >= 1f) return;

        bool wasDamaged = hull._damaged;
        hull._damaged = true;
        hull._integrity = integrity;
        var eventDelegate1 = (MulticastDelegate)typeof(ShipHull).GetField("OnDamaged",
            BindingFlags.Instance | BindingFlags.NonPublic
            | BindingFlags.Public).GetValue(hull);
        if (eventDelegate1 != null)
        {
            foreach (var handler in eventDelegate1.GetInvocationList())
            {
                handler.Method.Invoke(handler.Target, [hull]);
            }
        }
        if (hull._damageEffect != null)
        {
            hull._damageEffect.SetEffectBlend(1f - hull._integrity);
        }

        if (ShipEnhancements.InMultiplayer)
        {
            ShipEnhancements.QSBInteraction.SetHullDamaged(hull, !wasDamaged);
        }
    }
}
