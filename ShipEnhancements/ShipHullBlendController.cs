using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class ShipHullBlendController : ColorBlendController
{
    private List<Material> _sharedMaterials = [];

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

    protected override void SetColor(Color color)
    {
        SetColor([color]);
    }

    protected override void SetColor(List<object> theme)
    {
        Color color = (Color)theme[0];
        foreach (Material mat in _sharedMaterials)
        {
            mat.SetColor("_Color", color / 255f);
        }
    }

    protected override void ResetColor()
    {
        SetColor(_defaultTheme);
        base.ResetColor();
    }

    public void AddSharedMaterial(Material mat)
    {
        _sharedMaterials.Add(mat);
    }
}
