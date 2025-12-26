using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class ShipHullBlendController : ColorBlendController
{
    private List<ShipTextureBlender> _textureBlenders = [];

    protected override void Awake()
    {
        _defaultTheme = [new Color(1f, 1f, 1f, 0f) * 255f];
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
        Color color = theme.HullColor;
        color.a = 255f;
        _blendThemes[i] = [color];
    }

    protected override void UpdateLerp(List<object> start, List<object> end, float lerp)
    {
        SetColor(GetLerp(start, end, lerp));
    }

    protected override List<object> GetLerp(List<object> start, List<object> end, float lerp)
    {
        var newColor = Color.Lerp((Color)start[0], (Color)end[0], lerp);
        newColor.a = Mathf.Lerp(((Color)start[0]).a, ((Color)end[0]).a, lerp);
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
        foreach (var blender in _textureBlenders)
        {
            blender.SetColor(color);
        }
    }

    protected override void ResetColor()
    {
        SetColor(_defaultTheme);
        base.ResetColor();
    }

    public void AddTextureBlender(ShipTextureBlender blender)
    {
        _textureBlenders.Add(blender);
    }
}
