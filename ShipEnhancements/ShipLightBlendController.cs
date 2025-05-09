using System;
using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipLightBlendController : MonoBehaviour
{
    private ShipLight _shipLight;
    private PulsingLight _pulsingLight;
    private float _rainbowCycleLength = 15f;
    private float _currentLerp;
    private float _targetLerp;
    private float _resetFadeStartTime;
    private bool _reset = false;

    private Color[] _blendColors;
    private string _blendMode;
    private List<int> _rainbowIndexes = [];
    private Color _defaultColor;
    private Color _cachedColor;

    private readonly float _maxLerpStep = 0.005f;
    private readonly float _resetFadeTime = 2f;

    private void Awake()
    {
        _shipLight = GetComponent<ShipLight>();
        _pulsingLight = GetComponent<PulsingLight>();
        _defaultColor = (_shipLight ? _shipLight._light.color : _pulsingLight._light.color) * 255f;

        if ((string)shipLightBlend.GetProperty() == "Ship Damage %")
        {
            SELocator.GetShipDamageController().OnDamageUpdated += OnDamageUpdated;
        }

        if ((bool)enableColorBlending.GetProperty() && int.Parse((string)shipLightColorOptions.GetProperty()) > 1)
        {
            _blendColors = new Color[int.Parse((string)shipLightColorOptions.GetProperty())];
            for (int i = 0; i < _blendColors.Length; i++)
            {
                var setting = (string)("shipLightColor" + (i + 1))
                    .AsEnum<ShipEnhancements.Settings>().GetProperty();
                if (setting == "Rainbow")
                {
                    _rainbowIndexes.Add(i);
                    _blendColors[i] = Color.white;
                }
                else if (setting == "Default")
                {
                    _blendColors[i] = _defaultColor;
                }
                else
                {
                    _blendColors[i] = ShipEnhancements.ThemeManager.GetLightTheme(setting).LightColor;
                }
            }
            _blendMode = (string)shipLightBlend.GetProperty();
        }
    }

    private void SetColor(Color color)
    {
        color /= 255f;
        if (_shipLight)
        {
            _shipLight._baseEmission = color;
            _shipLight._matPropBlock.SetColor(_shipLight._propID_EmissionColor,
                    color * _shipLight._light.intensity / _shipLight._baseIntensity);
            _shipLight._emissiveRenderer.SetPropertyBlock(_shipLight._matPropBlock);
            _shipLight._light.color = color;
        }
        else if (_pulsingLight)
        {
            _pulsingLight._initEmissionColor = color;
            _pulsingLight._light.color = color;
        }
    }
    
    private void SetColor(ColorHSV color)
    {
        SetColor(color.AsRGB());
    }

    private void Update()
    {
        if (_blendColors == null) return;

        if (_blendMode == "Temperature" && SELocator.GetShipTemperatureDetector() != null)
        {
            _targetLerp = (SELocator.GetShipTemperatureDetector().GetTemperatureRatio() + 1f) / 2f;
            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[1], _blendColors[0], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                if (_currentLerp < 0.5f)
                {
                    color = Color.Lerp(_blendColors[2], _blendColors[1], _currentLerp * 2f);
                }
                else
                {
                    color = Color.Lerp(_blendColors[1], _blendColors[0], (_currentLerp - 0.5f) * 2f);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Ship Temperature" && SELocator.GetShipTemperatureDetector() != null)
        {
            _targetLerp = SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio();
            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[1], _blendColors[0], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                if (_currentLerp < 0.5f)
                {
                    color = Color.Lerp(_blendColors[2], _blendColors[1], _currentLerp * 2f);
                }
                else
                {
                    color = Color.Lerp(_blendColors[1], _blendColors[0], (_currentLerp - 0.5f) * 2f);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Reactor State")
        {
            ShipReactorComponent reactor = SELocator.GetShipDamageController()._shipReactorComponent;
            bool enabled = reactor._damaged && SELocator.GetShipTransform().Find("Module_Engine") != null;
            _targetLerp = enabled
                ? 1f - reactor._criticalTimer / reactor._criticalCountdown
                : 0f;

            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                _cachedColor = Color.Lerp(_blendColors[1], _blendColors[2], _currentLerp);

                if (!SELocator.GetShipDamageController().IsReactorCritical())
                {
                    if (!_reset)
                    {
                        _reset = true;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    color = Color.Lerp(_cachedColor, _blendColors[0], timeLerp);
                }
                else
                {
                    if (_reset)
                    {
                        _reset = false;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    color = Color.Lerp(_blendColors[0], _cachedColor, timeLerp);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Ship Damage %")
        {
            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                _cachedColor = Color.Lerp(_blendColors[1], _blendColors[2], _currentLerp);
                if (_targetLerp == 0)
                {
                    if (!_reset)
                    {
                        _reset = true;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    color = Color.Lerp(_cachedColor, _blendColors[0], timeLerp);
                }
                else
                {
                    if (_reset)
                    {
                        _reset = false;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    color = Color.Lerp(_blendColors[0], _cachedColor, timeLerp);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Fuel")
        {
            _targetLerp = 1 - SELocator.GetShipResources().GetFractionalFuel();

            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                if (_currentLerp < 0.5f)
                {
                    color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp * 2f);
                }
                else
                {
                    color = Color.Lerp(_blendColors[1], _blendColors[2], (_currentLerp - 0.5f) * 2f);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Oxygen")
        {
            _targetLerp = 1 - SELocator.GetShipResources().GetFractionalOxygen();

            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                if (_currentLerp < 0.5f)
                {
                    color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp * 2f);
                }
                else
                {
                    color = Color.Lerp(_blendColors[1], _blendColors[2], (_currentLerp - 0.5f) * 2f);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Time")
        {
            var t = Time.time / 4f / _blendColors.Length % _blendColors.Length;
            var a = (int)t;
            var b = (a + 1) % _blendColors.Length;
            var color = Color.Lerp(_blendColors[a], _blendColors[b], 
                Mathf.SmoothStep(0, 1, (t - a) * 2f - 0.5f));
            SetColor(color);
        }
        else
        {
            ResetLight();
        }
    }

    private void ResetLight()
    {
        Color baseEmission;
        Color baseColor = _defaultColor / 255f;
        if (_shipLight)
        {
            baseEmission = _shipLight._emissiveRenderer.sharedMaterials[_shipLight._materialIndex]
                .GetColor(_shipLight._propID_EmissionColor);

            _shipLight._baseEmission = baseColor;
            _shipLight._matPropBlock.SetColor(_shipLight._propID_EmissionColor,
                    baseEmission * _shipLight._light.intensity / _shipLight._baseIntensity);
            _shipLight._emissiveRenderer.SetPropertyBlock(_shipLight._matPropBlock);
            _shipLight._light.color = baseColor;
        }
        else if (_pulsingLight)
        {
            baseEmission = _pulsingLight._emissiveRenderer.sharedMaterials[_pulsingLight._materialIndex]
                .GetColor(PulsingLight.s_propID_EmissionColor);

            _pulsingLight._initEmissionColor = baseEmission;
            _pulsingLight._light.color = baseColor;
        }

        _blendColors = null;
        enabled = false;
    }

    private void FixedUpdate()
    {
        if ((_blendColors != null && _rainbowIndexes.Count == 0) 
            || (_shipLight && !_shipLight.IsOn()) 
            || (_pulsingLight && !_pulsingLight.enabled))
        {
            return;
        }

        float num = Time.time % _rainbowCycleLength / _rainbowCycleLength;
        ColorHSV color = new ColorHSV(num, 1f, 255f);

        if (_blendColors != null)
        {
            foreach (int i in _rainbowIndexes)
            {
                _blendColors[i] = color.AsRGB();
            }
        }
        else
        {
            SetColor(color);
        }
    }

    private void OnDamageUpdated()
    {
        int numParts = 0;
        int numDamaged = 0;

        foreach (var hull in SELocator.GetShipDamageController()._shipHulls)
        {
            numParts++;
            if (hull.isDamaged)
            {
                numDamaged++;
            }
        }
        foreach (var comp in SELocator.GetShipDamageController()._shipComponents)
        {
            numParts++;
            if (comp.isDamaged)
            {
                numDamaged++;
            }
        }

        _targetLerp = (float)numDamaged / numParts;
    }

    private void OnDestroy()
    {
        if ((string)shipLightBlend.GetProperty() == "Ship Damage %")
        {
            SELocator.GetShipDamageController().OnDamageUpdated -= OnDamageUpdated;
        }
    }
}
