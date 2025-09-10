using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class TemperatureDetector : MonoBehaviour
{
    protected List<TemperatureZone> _activeZones = [];
    protected float _currentTemperature;
    protected float _highTempCutoff;
    protected bool _highTemperature = false;
    protected float _maxInternalTemperature;
    protected float _currentInternalTemperature;
    protected bool _forceNextUpdate = false;

    protected virtual void Start()
    {
        _currentTemperature = 0f;
        _highTempCutoff = 50f;
        _maxInternalTemperature = 180f;
    }

    protected virtual void Update()
    {
        if (CanUpdateTemperature())
        {
            _currentTemperature = CalculateCurrentTemperature();

            if (!_highTemperature && Mathf.Abs(_currentTemperature) > _highTempCutoff)
            {
                _highTemperature = true;
            }
            else if (_highTemperature)
            {
                UpdateInternalTemperature();
                OnHighTemperature();
            }
            
            if (_forceNextUpdate)
            {
                _forceNextUpdate = false;
            }
        }

        if (_highTemperature && (_activeZones.Count == 0 || Mathf.Abs(_currentTemperature) < _highTempCutoff))
        {
            _highTemperature = false;
        }
        if (!_highTemperature)
        {
            UpdateCooldown();
        }
    }

    protected virtual void OnHighTemperature() { }

    protected virtual float CalculateCurrentTemperature()
    {
        float totalTemperature = 0f;
        foreach (TemperatureZone zone in _activeZones)
        {
            float temp = zone.GetTemperature(this);
            totalTemperature += temp;
        }
        return Mathf.Clamp(totalTemperature, -100f, 100f);
    }

    protected virtual void UpdateInternalTemperature()
    {
        if (Mathf.Abs(_currentInternalTemperature) < _maxInternalTemperature)
        {
            var highTempAbs = Mathf.InverseLerp(_highTempCutoff, 100f, Mathf.Abs(_currentTemperature));
            _currentInternalTemperature += Time.deltaTime * 3f * highTempAbs * Mathf.Sign(GetTemperatureRatio());
        }
    }

    protected virtual void UpdateCooldown()
    {
        if (Mathf.Abs(_currentInternalTemperature) / _maxInternalTemperature < 0.01f)
        {
            _currentInternalTemperature = 0f;
        }
        else
        {
            float step = Time.deltaTime * Mathf.InverseLerp(_highTempCutoff, 0f, Mathf.Abs(_currentTemperature));
            if (_currentInternalTemperature > 0f)
            {
                _currentInternalTemperature -= step;
            }
            else if (_currentInternalTemperature < 0f)
            {
                _currentInternalTemperature += step;
            }
        }
    }

    protected virtual bool CanUpdateTemperature()
    {
        return true;
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

    public float GetInternalTemperatureRatio()
    {
        if (_currentInternalTemperature > 0)
        {
            return Mathf.InverseLerp(0f, _maxInternalTemperature, _currentInternalTemperature);
        }
        else
        {
            return -Mathf.InverseLerp(0f, -_maxInternalTemperature, _currentInternalTemperature);
        }
    }

    public float GetHighTempCutoff()
    {
        return _highTempCutoff;
    }

    public bool IsHighTemperature()
    {
        return _highTemperature;
    }

    public float GetCurrentTemperature()
    {
        return _currentTemperature;
    }

    public float GetCurrentInternalTemperature()
    {
        return _currentInternalTemperature;
    }

    public void SetInternalTempRemote(float time)
    {
        _currentInternalTemperature = Mathf.Clamp(time, -_maxInternalTemperature, _maxInternalTemperature);
    }

    public virtual void AddZone(TemperatureZone zone)
    {
        if (!_activeZones.Contains(zone))
        {
            _activeZones.Add(zone);
        }
    }

    public virtual void RemoveZone(TemperatureZone zone)
    {
        if (_activeZones.Contains(zone))
        {
            _activeZones.Remove(zone);
            if (_activeZones.Count == 0)
            {
                _forceNextUpdate = true;
            }
        }
    }

    public virtual void RemoveAllZones()
    {
        _activeZones.Clear();
        _forceNextUpdate = true;
    }
}
