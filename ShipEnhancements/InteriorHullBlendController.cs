using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class InteriorHullBlendController : ColorBlendController
{
    protected override string CurrentBlend => (string)interiorHullColorBlend.GetProperty();
    protected override int NumberOfOptions => int.Parse((string)interiorHullColorOptions.GetProperty());
    protected override string OptionStem => "interiorHullColor";

    private List<Material> _sharedMaterials = [];

    protected override void Awake()
    {
        _defaultColor = Color.white;
        base.Awake();
    }

    protected override void SetColor(Color color)
    {
        foreach (Material mat in _sharedMaterials)
        {
            mat.SetColor("_Color", color);
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
