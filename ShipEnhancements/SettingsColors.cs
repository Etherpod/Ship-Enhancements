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
        { "Red", new Color(154, 92, 93) },
        { "Copper", new Color(168, 136, 115) },
        { "Brown", new Color(115, 74, 45) },
        { "Golden", new Color(255, 230, 152) },
        { "Green", new Color(108, 123, 77) },
        { "Ghostly Green", new Color(58, 168, 112) },
        { "Turquoise", new Color(113, 156, 154) },
        { "Blue", new Color(89, 119, 183) },
        { "Navy Blue", new Color(64, 72, 101) },
        { "Nomaian Blue", new Color(113, 112, 168) },
        { "Purple", new Color(110, 86, 154) },
        { "Pink", new Color(204, 165, 214) },
        { "Gray", new Color(115, 115, 115) },
        { "Black", Color.black },
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
