using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoMod.Utils;
using Newtonsoft.Json;
using ShipEnhancements.Models.Json;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class PersistentShipState : MonoBehaviour
{
    public bool PreserveSettings => ((bool)persistentShipState.GetProperty() || 
        ShipEnhancements.ExperimentalSettings.UltraPersistentShip) && !_skipNextLoad;

    private bool _everInitialized;
    private bool _skipNextLoad;
    private bool _processItems;

    private float _shipFuel;
    private float _shipOxygen;
    private float _shipWater;

    private readonly Dictionary<string, float> _hullIntegrities = [];
    private readonly Dictionary<string, float> _componentIntegrities = [];
    private readonly List<string> _detachedHullPaths = [];

    private readonly string[] _hullPaths =
    [
        "Module_Cockpit",
        "Module_Supplies",
        "Module_Engine",
        "Module_LandingGear/LandingGear_Front",
        "Module_LandingGear/LandingGear_Left",
        "Module_LandingGear/LandingGear_Right",
    ];

    private bool _headlightsOn;
    private bool _shipVanished;

    private CockpitButtonPanel.ButtonStates _buttonStates;
    private List<string> _emptySocketPaths = [];
    private Dictionary<string, object> _activeSettings = [];

    private void Start()
    {
        GlobalMessenger.AddListener("TriggerFlashback", OnTriggerFlashback);
        ShipEnhancements.Instance.PostShipInitialize.AddListener(OnShipInitialized);
        
        ShipStateJson state = ShipEnhancements.Instance.ModHelper.Storage
            .Load<ShipStateJson>("ShipStateSave.json");
        if (state == null) return;
        LoadJson(state);
    }

    public void OnStartSceneLoad(OWScene scene, OWScene loadScene)
    {
        if (scene == OWScene.SolarSystem && !ShipEnhancements.Instance.IsWarpingBackToEye)
        {
            SaveState();
        }
        else if (loadScene != OWScene.SolarSystem || ShipEnhancements.Instance.IsWarpingBackToEye || 
            (scene != OWScene.SolarSystem && !ShipEnhancements.ExperimentalSettings.UltraPersistentShip))
        {
            _skipNextLoad = true;
        }
    }

    public void OnCompleteSceneLoad(OWScene scene, OWScene loadScene)
    {
        if (loadScene == OWScene.SolarSystem && !ShipEnhancements.Instance.IsWarpingBackToEye)
        {
            if (_skipNextLoad)
            {
                _skipNextLoad = false;
                return;
            }
            LoadState();
        }
    }

    private void SaveState()
    {
        _processItems = false;
        
        _shipFuel = SELocator.GetShipResources()._currentFuel;
        _shipOxygen = SELocator.GetShipResources()._currentOxygen;
        _shipWater = SELocator.GetShipWaterResource()?.GetWater() ?? 0f;

        _hullIntegrities.Clear();
        _componentIntegrities.Clear();
        _detachedHullPaths.Clear();

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
        
        foreach (var path in _hullPaths)
        {
            if (!SELocator.GetShipTransform().Find(path))
            {
                _detachedHullPaths.Add(path);
            }
        }

        _activeSettings.Clear();
        
        var settings = Enum.GetValues(typeof(ShipEnhancements.Settings)) as ShipEnhancements.Settings[];
        foreach (var s in settings)
        {
            var def = SettingExtensions.ConvertJValue(ShipEnhancements.Instance.ModHelper
                .DefaultConfig.GetSettingsValue<object>(s.ToString()));
            if (!s.GetProperty().Equals(def))
            {
                _activeSettings.Add(s.ToString(), s.GetProperty());
            }
        }
        
        _headlightsOn = Locator.GetPlayerCameraController()._shipController._externalLightsOn;
        _buttonStates = SELocator.GetButtonPanel()?.GetButtonStates() ?? new();
        
        FindEmptySockets();
        CreateJson();
    }

    private void LoadState()
    {
        if (!_everInitialized)
        {
            _everInitialized = true;
            if (ShipEnhancements.ExperimentalSettings.UltraPersistentShip)
            {
                ShipStateJson state = ShipEnhancements.Instance.ModHelper.Storage
                    .Load<ShipStateJson>("ShipStateSave.json");
                if (state == null) return;
                LoadJson(state);
                _processItems = true;
            }
            else
            {
                return;
            }
        }

        if ((!(bool)persistentShipState.GetProperty() && 
                !ShipEnhancements.ExperimentalSettings.UltraPersistentShip) || 
            (ShipEnhancements.InMultiplayer && !ShipEnhancements.QSBAPI.GetIsHost()) ||
            LoadManager.GetCurrentScene() != OWScene.SolarSystem)
        {
            return;
        }

        ShipEnhancements.Instance.ModHelper.Events.Unity.RunWhen(() => ShipEnhancements.Instance.shipLoaded, () =>
        {
            if (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost())
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
                
                foreach (var path in _detachedHullPaths)
                {
                    var obj = SELocator.GetShipTransform().Find(path);
                    if (!obj) continue;
                    if (obj.TryGetComponent(out ShipDetachableModule mod))
                    {
                        mod.Detach();
                        mod.gameObject.SetActive(false);
                    }
                    else if (obj.TryGetComponent(out ShipDetachableLeg leg))
                    {
                        leg.Detach();
                        leg.gameObject.SetActive(false);
                    }
                }
            }

            SELocator.GetShipCockpitController().SetEnableShipLights(_headlightsOn, false);
            SELocator.GetButtonPanel()?.SetButtonStates(_buttonStates);

            if (_shipVanished)
            {
                ShipEnhancements.Instance.ModHelper.Events.Unity.RunWhen(
                    () => !LateInitializerManager.s_paused, () =>
                {
                    SELocator.GetShipBody().gameObject.SetActive(false);
                });
                _shipVanished = false;
            }
        });

        _processItems = true;
    }

    private void CreateJson()
    {
        ShipStateJson state = new ShipStateJson(_shipFuel, _shipOxygen, _shipWater, _hullIntegrities,
            _componentIntegrities, _detachedHullPaths, _headlightsOn, _shipVanished, _buttonStates,
            _emptySocketPaths, _activeSettings);
        ShipEnhancements.Instance.ModHelper.Storage.Save(state, "ShipStateSave.json");
    }

    private void LoadJson(ShipStateJson state)
    {
        _shipFuel = state.ShipFuel;
        _shipOxygen = state.ShipOxygen;
        _shipWater = state.ShipWater;
        _hullIntegrities.Clear();
        _hullIntegrities.AddRange(state.HullIntegrities);
        _componentIntegrities.Clear();
        _componentIntegrities.AddRange(state.ComponentIntegrities);
        _detachedHullPaths.AddRange(state.DetachedHullPaths);
        _headlightsOn = state.HeadlightsOn;
        _shipVanished = state.ShipVanished;
        _buttonStates = state.ButtonStates;
        _emptySocketPaths = state.EmptySocketPaths;
        _activeSettings = state.ActiveSettings;
    }

    private void SetHullIntegrity(ShipHull hull, float integrity)
    {
        if (integrity >= 1f) return;

        bool wasDamaged = hull._damaged;
        hull._damaged = true;
        hull._integrity = integrity;
        var eventDelegate1 = (MulticastDelegate)typeof(ShipHull).GetField("OnDamaged",
            BindingFlags.Instance | BindingFlags.NonPublic
            | BindingFlags.Public)?.GetValue(hull);
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

    private void FindEmptySockets()
    {
        _emptySocketPaths.Clear();
        var items = SELocator.GetShipTransform().GetComponentsInChildren<OWItem>().ToList();
        
        foreach (var socket in SELocator.GetShipTransform().GetComponentsInChildren<SEItemSocket>())
        {
            if (socket.GetSocketedItem() == null)
            {
                bool empty = true;
                for (int i = 0; i < items.Count; i++)
                {
                    if (!items[i].GetComponentInParent<SEItemSocket>() && 
                        socket.AcceptsItem(items[i]))
                    {
                        items.RemoveAt(i);
                        empty = false;
                        break;
                    }
                }

                if (empty)
                {
                    var obj = socket.gameObject;
                    string path = obj.name;
                    while (obj.transform.parent != null && 
                        obj.transform.parent != SELocator.GetShipTransform())
                    {
                        obj = obj.transform.parent.gameObject;
                        path = obj.name + "/" + path;
                    }

                    _emptySocketPaths.Add(path);
                }
            }
            else
            {
                items.Remove(socket.GetSocketedItem());
            }
        }
    }

    private void OnShipInitialized()
    {
        if (!_processItems || (!(bool)persistentShipState.GetProperty() &&
            !ShipEnhancements.ExperimentalSettings.UltraPersistentShip)) return;
        
        foreach (var path in _emptySocketPaths)
        {
            ShipEnhancements.WriteDebugMessage("checking " + path);
            var socket = SELocator.GetShipTransform().Find(path);
            if (socket)
            {
                socket.GetComponent<SEItemSocket>().SkipItemCreation();
            }
        }
    }

    private void OnTriggerFlashback()
    {
        if (ShipEnhancements.ExperimentalSettings.UltraPersistentShip)
        {
            ShipEnhancements.Instance.UpdateExperimentalSettings();
        }

        _skipNextLoad = _skipNextLoad || !ShipEnhancements.ExperimentalSettings.UltraPersistentShip;
    }

    public void OnShipVanished()
    {
        _shipVanished = true;
    }

    public IDictionary<string, object> GetActiveSettings()
    {
        return _activeSettings;
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("TriggerFlashback", OnTriggerFlashback);
    }
}
