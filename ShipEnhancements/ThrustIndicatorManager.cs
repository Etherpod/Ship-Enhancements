using UnityEngine;

namespace ShipEnhancements;

public static class ThrustIndicatorManager
{
    private static ThrustAndAttitudeIndicator _indicator;
    private static Light[] _barLights;
    private static MeshRenderer[] _barRenderers;
    private static Color _currentColor;

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
    }

    public static void SetColor(Color color)
    {
        _currentColor = color;
        foreach (Light light in _barLights)
        {
            light.color = color;
        }
        foreach (MeshRenderer renderer in _barRenderers)
        {
            renderer.material.SetColor("_BarColor", color);
        }
    }

    public static void LayerColor(Color color, float amount)
    {
        foreach (Light light in _barLights)
        {
            light.color = Color.Lerp(_currentColor, color, amount);
        }
        foreach (MeshRenderer renderer in _barRenderers)
        {
            renderer.material.SetColor("_BarColor", Color.Lerp(_currentColor, color, amount));
        }
    }

    public static void DisableIndicator()
    {
        _indicator.ResetAllArrows();
        _indicator._thrusterArrowRoot.gameObject.SetActive(false);
        _indicator.enabled = false;
    }
}
