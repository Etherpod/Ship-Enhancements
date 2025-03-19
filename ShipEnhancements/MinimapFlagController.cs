using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class MinimapFlagController : MonoBehaviour
{
    private Minimap _minimap;
    private GameObject _flagMarkerPrefab;
    private Dictionary<ExpeditionFlagItem, Transform> _activeFlags = [];
    private List<Renderer> _renderersToToggle = [];
    private List<ElectricalComponent> _componentsToToggle = [];

    private void Awake()
    {
        _minimap = GetComponent<Minimap>();
        GameObject prefab;
        if (_minimap._minimapMode == Minimap.MinimapMode.Player)
        {
            prefab = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/PlayerFlagMarkerPivot.prefab");
        }
        else
        {
            prefab = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ShipFlagMarkerPivot.prefab");
        }
        AssetBundleUtilities.ReplaceShaders(prefab);
        _flagMarkerPrefab = prefab;
    }

    public void UpdateMarkers()
    {
        foreach (var flag in _activeFlags.Keys)
        {
            if (flag.GetRulesetDetector() != null
                && flag.GetRulesetDetector().GetPlanetoidRuleset() == _minimap._playerRulesetDetector.GetPlanetoidRuleset())
            {
                _activeFlags[flag].localPosition = _minimap.GetLocalMapPosition(flag.transform);
                _activeFlags[flag].LookAt(_minimap._globeMeshTransform, _minimap._globeMeshTransform.up);
            }
            else
            {
                _activeFlags[flag].localPosition = Vector3.zero;
                _activeFlags[flag].localRotation = Quaternion.identity;
            }
        }
    }

    public void SetComponentsEnabled(bool value)
    {
        for (int i = 0; i < _renderersToToggle.Count; i++)
        {
            if (_renderersToToggle[i] == null)
            {
                _renderersToToggle.RemoveAt(i);
                i--;
            }
            else
            {
                _renderersToToggle[i].enabled = value;
            }
        }
        for (int i = 0; i < _componentsToToggle.Count; i++)
        {
            if (_componentsToToggle[i] == null)
            {
                _componentsToToggle.RemoveAt(i);
                i--;
            }
            else
            {
                _componentsToToggle[i].SetPowered(value);
            }
        }
    }

    public void SetComponentsOn(bool value)
    {
        for (int i = 0; i < _componentsToToggle.Count; i++)
        {
            if (_componentsToToggle[i] == null)
            {
                _componentsToToggle.RemoveAt(i);
                i--;
            }
            else if (_componentsToToggle[i] is ShipLight)
            {
                (_componentsToToggle[i] as ShipLight).SetOn(value);
            }
        }
    }

    public void AddFlag(ExpeditionFlagItem flag)
    {
        if (!_activeFlags.ContainsKey(flag))
        {
            ShipCockpitUI cockpitUI = SELocator.GetShipTransform().GetComponentInChildren<ShipCockpitUI>();
            Transform markerTransform = Instantiate(_flagMarkerPrefab, transform).transform;
            _activeFlags.Add(flag, markerTransform);
            foreach (var rend in markerTransform.GetComponentsInChildren<Renderer>())
            {
                rend.enabled = _minimap._updateMinimap;
                _renderersToToggle.Add(rend);
            }
            foreach (var comp in markerTransform.GetComponentsInChildren<ElectricalComponent>())
            {
                comp.SetPowered(_minimap._updateMinimap);
                if (comp is ShipLight)
                {
                    (comp as ShipLight).SetOn(cockpitUI._landingCamScreenLight.IsOn());
                }
                _componentsToToggle.Add(comp);
            }
        }
    }

    public void RemoveFlag(ExpeditionFlagItem flag)
    {
        if (_activeFlags.ContainsKey(flag))
        {
            foreach (var rend in _activeFlags[flag].GetComponentsInChildren<Renderer>())
            {
                _renderersToToggle.Remove(rend);
            }
            foreach (var comp in _activeFlags[flag].GetComponentsInChildren<ElectricalComponent>())
            {
                _componentsToToggle.Remove(comp);
            }
            Destroy(_activeFlags[flag].gameObject);
            _activeFlags.Remove(flag);
        }
    }
}
