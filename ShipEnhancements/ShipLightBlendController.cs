﻿using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipLightBlendController : ColorBlendController
{
    protected override string CurrentBlend => (string)shipLightColorBlend.GetProperty();
    protected override int NumberOfOptions => int.Parse((string)shipLightColorOptions.GetProperty());
    protected override string OptionStem => "shipLightColor";

    private ShipLight _shipLight;
    private PulsingLight _pulsingLight;

    protected override void Awake()
    {
        _shipLight = GetComponent<ShipLight>();
        _pulsingLight = GetComponent<PulsingLight>();
        _defaultTheme = [(_shipLight ? _shipLight._light.color : _pulsingLight._light.color) * 255f];
        base.Awake();
    }

    protected override void SetBlendTheme(int i, string themeName)
    {
        if (themeName == "Default")
        {
            _blendThemes[i] = _defaultTheme;
            return;
        }

        LightTheme theme = ShipEnhancements.ThemeManager.GetLightTheme(themeName);
        _blendThemes[i] = [theme.LightColor];
    }

    protected override void UpdateLerp(List<object> start, List<object> end, float lerp)
    {
        SetColor(GetLerp(start, end, lerp));
    }

    protected override List<object> GetLerp(List<object> start, List<object> end, float lerp)
    {
        var newColor = Color.Lerp((Color)start[0], (Color)end[0], lerp);
        return [newColor];
    }

    protected override void UpdateRainbowTheme(int index, Color color)
    {
        _blendThemes[index] = [color];
    }

    protected override void SetColor(Color color)
    {
        SetColor([color]);
    }

    protected override void SetColor(List<object> theme)
    {
        Color color = (Color)theme[0] / 255f;
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

    protected override void ResetColor()
    {
        Color baseEmission;
        Color baseColor = (Color)_defaultTheme[0] / 255f;
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

        base.ResetColor();
    }
}
