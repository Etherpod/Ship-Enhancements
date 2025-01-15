using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class TemperatureDetector : MonoBehaviour
{
    protected List<TemperatureZone> _activeZones = [];
    protected float _currentTemperature;
    protected float _highTempCutoff;
    protected bool _highTemperature = false;
    protected float _internalTempMeterLength;
    protected float _internalTempMeter;
    protected bool _updateNextFrame = false;

    protected virtual void Start()
    {
        _currentTemperature = 0f;
        _highTempCutoff = 50f;
        _internalTempMeterLength = 180f;
    }

    protected virtual void Update()
    {
        if (_activeZones.Count > 0 || _updateNextFrame)
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
                if (Mathf.Abs(_internalTempMeter) < _internalTempMeterLength)
                {
                    _internalTempMeter += Time.deltaTime * 3f * Mathf.InverseLerp(_highTempCutoff, 100f, Mathf.Abs(_currentTemperature)) * Mathf.Sign(GetTemperatureRatio());
                }

                // Check if internal temp is on the same side as temp
                if ((GetInternalTemperatureRatio() - 0.5f < 0) == (GetTemperatureRatio() < 0))
                {
                    UpdateHighTemperature();
                }
            }
            
            if (_updateNextFrame)
            {
                _updateNextFrame = false;
            }
        }

        if (_highTemperature && (_activeZones.Count == 0 || Mathf.Abs(_currentTemperature) < _highTempCutoff))
        {
            _highTemperature = false;
        }
        if (!_highTemperature)
        {
            if (Mathf.Abs(_internalTempMeter) / _internalTempMeterLength < 0.01f)
            {
                _internalTempMeter = 0f;
            }
            else
            {
                float step = Time.deltaTime * Mathf.InverseLerp(_highTempCutoff, 0f, Mathf.Abs(_currentTemperature));
                if (_internalTempMeter > 0f)
                {
                    _internalTempMeter -= step;
                }
                else if (_internalTempMeter < 0f)
                {
                    _internalTempMeter += step;
                }
            }
        }
    }

    protected virtual void UpdateHighTemperature() { }

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
        return Mathf.InverseLerp(-_internalTempMeterLength, _internalTempMeterLength, _internalTempMeter);
    }

    public bool IsHighTemperature()
    {
        return _highTemperature;
    }

    public float GetShipTempMeter()
    {
        return _internalTempMeter;
    }

    public void SetShipTempMeter(float time)
    {
        _internalTempMeter = Mathf.Clamp(time, -_internalTempMeterLength, _internalTempMeterLength);
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
                _updateNextFrame = true;
            }
        }
    }

    public virtual void RemoveAllZones()
    {
        _activeZones.Clear();
        _updateNextFrame = true;
    }
}
