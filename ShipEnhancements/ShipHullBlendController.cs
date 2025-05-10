using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class ShipHullBlendController : ColorBlendController
{
    private List<Material> _sharedMaterials = [];

    protected override void Awake()
    {
        _defaultColor = Color.white * 255f;
        base.Awake();
    }

    protected override Color GetThemeColor(string themeName)
    {
        return ShipEnhancements.ThemeManager.GetHullTheme(themeName).HullColor;
    }

    protected override void SetColor(Color color)
    {
        foreach (Material mat in _sharedMaterials)
        {
            mat.SetColor("_Color", color / 255f);
        }
    }

    protected override void ResetColor()
    {
        SetColor(_defaultColor);
        base.ResetColor();
    }

    public void AddSharedMaterial(Material mat)
    {
        _sharedMaterials.Add(mat);
    }
}
