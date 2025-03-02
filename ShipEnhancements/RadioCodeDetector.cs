using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class RadioCodeDetector : MonoBehaviour
{
    public delegate void RadioCodeDetectorEvent(RadioCodeZone zone);
    public event RadioCodeDetectorEvent OnChangeActiveZone;

    private List<RadioCodeZone> _activeZones = [];
    private AudioClip _activeCode;

    public void AddZone(RadioCodeZone zone)
    {
        _activeZones.SafeAdd(zone);
        _activeCode = zone.GetAudioCode();
        OnChangeActiveZone?.Invoke(zone);
    }

    public void RemoveZone(RadioCodeZone zone)
    {
        if (_activeZones.Contains(zone))
        {
            _activeZones.Remove(zone);
            if (_activeZones.Count > 0)
            {
                _activeCode = _activeZones[0].GetAudioCode();
                OnChangeActiveZone?.Invoke(_activeZones[0]);
            }
            else
            {
                _activeCode = null;
                OnChangeActiveZone?.Invoke(null);
            }
        }
    }

    public AudioClip GetActiveCode()
    {
        return _activeCode;
    }
}
