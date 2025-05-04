using System;
using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipLightBlendController : MonoBehaviour
{
    private ShipLight _shipLight;
    private PulsingLight _pulsingLight;
    private float _colorTransitionTime = 6f;
    private float _red;
    private float _green;
    private float _blue;
    private int _index;
    private float _lastDelta;

    private Color[] _blendColors;
    private string _blendMode;
    private List<int> _rainbowIndexes = [];
    private Color _defaultColor;

    private void Awake()
    {
        _shipLight = GetComponent<ShipLight>();
        _pulsingLight = GetComponent<PulsingLight>();
        _red = 1f;
        _green = 0f;
        _blue = 0f;
        _defaultColor = (_shipLight ? _shipLight._light.color : _pulsingLight._light.color) * 255f;

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

    private void Update()
    {
        if (_blendColors == null) return;

        if (_blendMode == "Temperature" && SELocator.GetShipTemperatureDetector() != null)
        {
            float temp = (SELocator.GetShipTemperatureDetector().GetTemperatureRatio() + 1f) / 2f;

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[1], _blendColors[0], temp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                if (temp < 0.5f)
                {
                    color = Color.Lerp(_blendColors[2], _blendColors[1], temp * 2f);
                }
                else
                {
                    color = Color.Lerp(_blendColors[1], _blendColors[0], (temp - 0.5f) * 2f);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Ship Temperature" && SELocator.GetShipTemperatureDetector() != null)
        {
            float shipTemp = SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio();
            
            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[1], _blendColors[0], shipTemp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                if (shipTemp < 0.5f)
                {
                    color = Color.Lerp(_blendColors[2], _blendColors[1], shipTemp * 2f);
                }
                else
                {
                    color = Color.Lerp(_blendColors[1], _blendColors[0], (shipTemp - 0.5f) * 2f);
                }
                SetColor(color);
            }
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

        float num = Mathf.InverseLerp(0f, _colorTransitionTime, Time.time % _colorTransitionTime);

        if (_lastDelta > num)
        {
            _index++;
            if (_index > 5) _index = 0;
        }

        if (_index == 0)
        {
            _green = Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 1)
        {
            _red = 1 - Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 2)
        {
            _blue = Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 3)
        {
            _green = 1 - num;
        }
        else if (_index == 4)
        {
            _red = Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 5)
        {
            _blue = 1 - Mathf.Lerp(0f, 1f, num);
        }

        if (_blendColors != null)
        {
            foreach (int i in _rainbowIndexes)
            {
                _blendColors[i] = new Color(_red, _green, _blue) * 255f;
            }
        }
        else
        {
            SetColor(new Color(_red, _green, _blue) * 255f);
        }

        _lastDelta = num;
    }
}
