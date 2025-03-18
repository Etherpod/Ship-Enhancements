using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class MinimapFlagController : MonoBehaviour
{
    private Minimap _minimap;
    private GameObject _flagMarkerPrefab;
    private Dictionary<ExpeditionFlagItem, Transform> _activeFlags = [];
    private List<Renderer> _renderersToToggle = [];

    private void Awake()
    {
        _minimap = GetComponent<Minimap>();
        GameObject prefab = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/FlagMarkerPivot.prefab");
        AssetBundleUtilities.ReplaceShaders(prefab);
        _flagMarkerPrefab = prefab;
    }

    public void UpdateMarkers()
    {
        foreach (var flag in _activeFlags.Keys)
        {
            // check for ruleset

            _activeFlags[flag].localPosition = _minimap.GetLocalMapPosition(flag.transform);
            _activeFlags[flag].LookAt(_minimap._globeMeshTransform, _minimap._globeMeshTransform.up);
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
    }

    public void AddFlag(ExpeditionFlagItem flag)
    {
        if (!_activeFlags.ContainsKey(flag))
        {
            ShipEnhancements.WriteDebugMessage("Add flag");
            Transform markerTransform = Instantiate(_flagMarkerPrefab, transform).transform;
            _activeFlags.Add(flag, markerTransform);
            _renderersToToggle.AddRange(markerTransform.GetComponentsInChildren<Renderer>());
        }
    }

    public void RemoveFlag(ExpeditionFlagItem flag)
    {
        if (_activeFlags.ContainsKey(flag))
        {
            ShipEnhancements.WriteDebugMessage("Remove flag");
            foreach (var rend in _activeFlags[flag].GetComponentsInChildren<Renderer>())
            {
                _renderersToToggle.Remove(rend);
            }
            Destroy(_activeFlags[flag].gameObject);
            _activeFlags.Remove(flag);
        }
    }
}
