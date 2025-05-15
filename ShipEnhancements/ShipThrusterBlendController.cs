using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipThrusterBlendController : ColorBlendController
{
    protected override string CurrentBlend => (string)thrusterColorBlend.GetProperty();
    protected override int NumberOfOptions => int.Parse((string)thrusterColorOptions.GetProperty());
    protected override string OptionStem => "thrusterColor";

    private MeshRenderer _rend;
    private Material _thrustMat;
    private Light _light;
    private ThrustAndAttitudeIndicator _indicator;

    protected override void Awake()
    {
        _rend = GetComponent<MeshRenderer>();
        _thrustMat = _rend?.material;
        _light = GetComponentInChildren<Light>();
        _indicator = SELocator.GetShipTransform().GetComponentInChildren<ThrustAndAttitudeIndicator>(true);

        var tex = (Texture2D)ShipEnhancements.LoadAsset("Assets/ShipEnhancements/ThrusterColors/ThrusterFlames_Default.png");
        _defaultTheme = [tex, _thrustMat.GetColor("_Color").maxColorComponent, 
            _light.color, _indicator._rendererBack.material.GetColor("_BarColor"),
            1.5f, _indicator._lightsBack[0].color];

        base.Awake();
    }

    protected override void SetBlendTheme(int i, string themeName)
    {
        if (themeName == "Default")
        {
            _blendThemes[i] = _defaultTheme;
            return;
        }

        ThrusterTheme theme = ShipEnhancements.ThemeManager.GetThrusterTheme(themeName);
        Texture2D tex = (Texture2D)ShipEnhancements.LoadAsset("Assets/ShipEnhancements/ThrusterColors/"
            + theme.ThrusterColor);
        _blendThemes[i] = [tex, theme.ThrusterIntensity, theme.ThrusterLight,
            theme.IndicatorColor, theme.IndicatorIntensity, theme.IndicatorLight];
    }

    protected override void UpdateLerp(List<object> start, List<object> end, float lerp)
    {
        SetColor(GetLerp(start, end, lerp));
    }

    protected override List<object> GetLerp(List<object> start, List<object> end, float lerp)
    {
        var startTex = (Texture2D)start[0];
        var endTex = (Texture2D)end[0];
        var startPixels = startTex.GetPixels();
        var endPixels = endTex.GetPixels();
        Color[] pixelResult = new Color[startPixels.Length];
        for (int i = 0; i < startPixels.Length; i++)
        {
            pixelResult[i] = Color.Lerp(startPixels[i], endPixels[i], lerp);
        }
        Texture2D newTex = new Texture2D(startTex.width, startTex.height);
        newTex.SetPixels(pixelResult);
        newTex.Apply();

        var thrustIntensity = Mathf.Lerp((float)start[1], (float)end[1], lerp);
        var thrustLight = Color.Lerp((Color)start[2], (Color)end[2], lerp);
        var indicatorColor = Color.Lerp((Color)start[3], (Color)end[3], lerp);
        var indicatorIntensity = Mathf.Lerp((float)start[4], (float)end[4], lerp);
        var indicatorLight = Color.Lerp((Color)start[5], (Color)end[5], lerp);

        return [newTex, thrustIntensity, thrustLight, indicatorColor, 
            indicatorIntensity, indicatorLight];
    }

    protected override void SetColor(Color color)
    {
        SetColor([_defaultTheme[0], 1f, color, color, 1f, color]);
    }

    protected override void SetColor(List<object> theme)
    {
        _thrustMat.SetTexture("_MainTex", (Texture2D)theme[0]);
        _thrustMat.SetColor("_Color", Color.white * Mathf.Pow((float)theme[1], 1));
        _light.color = (Color)theme[2] / 255f;

        ThrustIndicatorManager.SetColor((Color)theme[3] / 191f, 
            (Color)theme[5] / 255f, (float)theme[4]);
    }

    protected override void ResetColor()
    {
        SetColor(_defaultTheme);
        base.ResetColor();
    }
}
