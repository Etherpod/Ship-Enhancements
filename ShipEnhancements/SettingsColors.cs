using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public static class SettingsColors
{
    private static Dictionary<string, Color> _nameToLightingColor = new()
    {
        { "Default", Color.white },
        { "Red", new Color(255, 0, 0) },
        { "Hearthian Orange", new Color(255, 92, 33) },
        { "Orange", new Color(255, 174, 94) },
        { "Yellow", new Color(255, 233, 96) },
        { "Green", new Color(119, 183, 120) },
        { "Ghostly Green", new Color(61, 232, 179) },
        { "Turquoise", new Color(89, 255, 241) },
        { "Blue", new Color(22, 88, 255) },
        { "Nomaian Blue", new Color(102, 107, 255) },
        { "Blacklight", new Color(79, 5, 255) },
        { "Purple", new Color(190, 86, 255) },
        { "Magenta", new Color(216, 119, 255) },
        { "White", new Color(255, 255, 255) },
        { "Divine", new Color(100, 100, 100) },
        { "Rainbow", Color.white }
    };

    private static Dictionary<string, Color> _nameToShipColor = new()
    {
        { "Default", Color.white },
        { "Red", new Color(221, 185, 185) },
        { "Orange", new Color(255, 201, 170) },
        { "Golden", new Color(255, 230, 152) },
        { "Green", new Color(163, 188, 146) },
        { "Turquoise", new Color(213, 255, 254) },
        { "Blue", new Color(210, 231, 255) },
        { "Lavender", new Color(222, 203, 245) },
        { "Pink", new Color(255, 218, 225) },
        { "Gray", new Color(119, 122, 140) },
        { "Rainbow", Color.white }
    };

    public static Color GetLightingColor(string name)
    {
        if (name == "Divine")
        {
            return _nameToLightingColor[name];
        }
        return _nameToLightingColor.ContainsKey(name) ? _nameToLightingColor[name] / 255f : Color.white;
    }

    public static Color GetShipColor(string name)
    {
        return _nameToShipColor.ContainsKey(name) ? _nameToShipColor[name] / 255f : Color.white;
    }
}
