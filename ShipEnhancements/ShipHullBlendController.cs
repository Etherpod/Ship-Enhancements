using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class ShipHullBlendController : ColorBlendController
{
    private List<(Material baseMat, Material customMat)> _sharedMaterials = [];
    protected virtual RenderTexture TargetRenderTex => null;
    protected virtual bool IsWoodController => false;

    protected override void Awake()
    {
        _defaultTheme = [Color.white * 255f];
        base.Awake();
    }

    protected override void SetBlendTheme(int i, string themeName)
    {
        if (themeName == "Default")
        {
            _blendThemes[i] = _defaultTheme;
            return;
        }

        HullTheme theme = ShipEnhancements.ThemeManager.GetHullTheme(themeName);
        _blendThemes[i] = [theme.HullColor];
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
        Color color = (Color)theme[0];
        foreach (var pair in _sharedMaterials)
        {
            ShipEnhancements.Instance.textureBlendMat.SetColor("_OverlayColor", color / 255f);
            ShipEnhancements.Instance.textureBlendMat.SetFloat("_BlendFactor", 1f);
            ShipEnhancements.Instance.textureBlendMat.SetFloat("_IsWoodTexture", IsWoodController ? 1f : 0f);
            Graphics.Blit(pair.baseMat.GetTexture("_MainTex"), TargetRenderTex, ShipEnhancements.Instance.textureBlendMat);
            pair.customMat.SetTexture("_MainTex", TargetRenderTex);
        }
    }

    protected override void ResetColor()
    {
        SetColor(_defaultTheme);
        base.ResetColor();
    }

    public void AddSharedMaterial(Material baseMat, Material customMat)
    {
        _sharedMaterials.Add((baseMat, customMat));
    }
}
