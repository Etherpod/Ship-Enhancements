﻿using UnityEngine;

namespace ShipEnhancements;

public static class ThrustIndicatorManager
{
    private static ThrustAndAttitudeIndicator _indicator;
    private static Light[] _barLights;
    private static MeshRenderer[] _barRenderers;
    private static Color _currentColor;
    private static Color _currentLightColor;
    private static Color _layerColor;
    private static float _layerAmount;

    public static void Initialize()
    {
        _indicator = SELocator.GetShipTransform().GetComponentInChildren<ThrustAndAttitudeIndicator>(true);

        _barLights = _indicator.GetComponentsInChildren<Light>();
        _barRenderers = new MeshRenderer[6];

        _barRenderers[0] = _indicator._rendererBack;
        _barRenderers[1] = _indicator._rendererForward;
        _barRenderers[2] = _indicator._rendererLeft;
        _barRenderers[3] = _indicator._rendererRight;
        _barRenderers[4] = _indicator._rendererUp;
        _barRenderers[5] = _indicator._rendererDown;

        _currentColor = _barRenderers[0].material.GetColor("_BarColor");
        _currentLightColor = _barLights[0].color;
    }

    public static void SetColor(Color color, Color lightColor, float intensity = 1)
    {
        color *= Mathf.Pow(2, intensity);
        _currentColor = color;
        _currentLightColor = lightColor;
        UpdateColor();
    }

    public static void ApplyTheme(ThrusterTheme theme)
    {
        Color hdrColor = theme.IndicatorColor / 255f * Mathf.Pow(2, theme.IndicatorIntensity);
        _currentColor = hdrColor;
        _currentLightColor = theme.IndicatorLight / 255f;
        UpdateColor();
    }

    public static void LayerColor(Color color, float amount)
    {
        _layerColor = color;
        _layerAmount = amount;
        UpdateColor();
    }

    private static void UpdateColor()
    {
        foreach (Light light in _barLights)
        {
            light.color = Color.Lerp(_currentLightColor, _layerColor, _layerAmount);
        }
        foreach (MeshRenderer renderer in _barRenderers)
        {
            renderer.material.SetColor("_BarColor", Color.Lerp(_currentColor, _layerColor, _layerAmount));
        }
    }

    public static void DisableIndicator()
    {
        _indicator.ResetAllArrows();
        _indicator._thrusterArrowRoot.gameObject.SetActive(false);
        _indicator.enabled = false;
    }
}
