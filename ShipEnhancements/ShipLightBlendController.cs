using System;
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
    private bool _rainbow = false;

    private void Awake()
    {
        _shipLight = GetComponent<ShipLight>();
        _pulsingLight = GetComponent<PulsingLight>();
        _red = 1f;
        _green = 0f;
        _blue = 0f;

        _blendColors = new Color[int.Parse((string)shipLightColorOptions.GetProperty())];
        for (int i = 0; i < _blendColors.Length; i++)
        {
            var setting = (string)("shipLightColor" + (i + 1))
                .AsEnum<ShipEnhancements.Settings>().GetProperty();
            if (setting == "Rainbow")
            {
                _rainbow = true;
            }

            if (setting == "Default")
            {
                _blendColors[i] = (_shipLight ? _shipLight._light.color : _pulsingLight._light.color)
                    * 255f;
            }
            else
            {
                _blendColors[i] = ShipEnhancements.ThemeManager.GetLightTheme(setting).LightColor;
            }
        }
        _blendMode = (string)shipLightBlend.GetProperty();
    }

    private void SetColor(Color color)
    {
        color /= 255f;
        if (_shipLight)
        {
            _shipLight._baseEmission = color;
            if (_shipLight._light.intensity == _shipLight._baseIntensity)
            {
                _shipLight._matPropBlock.SetColor(_shipLight._propID_EmissionColor, color);
                _shipLight._emissiveRenderer.SetPropertyBlock(_shipLight._matPropBlock);
            }
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
        switch (_blendMode)
        {
            case "Temperature":
                if (SELocator.GetShipTemperatureDetector() != null)
                {
                    float temp = (SELocator.GetShipTemperatureDetector().GetTemperatureRatio() + 1f) / 2f;
                    if (_blendColors.Length == 1)
                    {
                        SetColor(_blendColors[0]);
                    }
                    else if (_blendColors.Length == 2)
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
                break;
        }
    }


    private void FixedUpdate()
    {
        if (!_rainbow || (_shipLight && !_shipLight.IsOn()) || (_pulsingLight && !_pulsingLight.enabled))
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

        _lastDelta = num;
    }
}
