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

    private static Dictionary<string, (Color, float, Color)> _nameToThrusterColor = new()
    {
        { "Default", (Color.white, 1f, Color.white) },
        { "Red", (new Color(191, 23, 23), 3.278829f, new Color(255, 101, 30)) },
        { "White-Orange", (new Color(26, 54, 191), 5.2f, new Color(255, 214, 179)) },
        { "Lime-Orange", (new Color(64, 191, 68), 2.5f, new Color(230, 255, 105)) },
        { "Lime", (new Color(25, 191, 0), 2.574406f, new Color(143, 255, 47)) },
        { "Ghostly Green", (new Color(3, 191, 117), 3.8f, new Color(83, 255, 150)) },
        { "Turquoise", (new Color(4, 39, 191), 4.8f, new Color(131, 239, 255)) },
        { "Blue", (new Color(1, 12, 191), 5.900001f, new Color(177, 218, 255)) },
        { "Purple", (new Color(3, 2, 191), 6.2f, new Color(214, 158, 255)) },
        { "Pink", (new Color(137, 7, 191), 4.8f, new Color(255, 162, 233)) },
        { "Rose", (new Color(31, 26, 191), 4.2f, new Color(252, 182, 255)) },
        { "Rainbow", (Color.white, 1f, Color.white) },
    };

    private static Dictionary<string, Color> _nameToIndicatorColor = new()
    {
        { "Default", Color.white },
        { "Red", new Color(255, 16, 23) },
        { "White-Orange", new Color(255, 182, 124) },
        { "Lime-Orange", new Color(211, 255, 30) },
        { "Lime", new Color(106, 243, 0) },
        { "Ghostly Green", new Color(28, 255, 43) },
        { "Turquoise", new Color(37, 255, 236) },
        { "Blue", new Color(19, 92, 255) },
        { "Purple", new Color(78, 14, 383) },
        { "Pink", new Color(255, 67, 255) },
        { "Rose", new Color(255, 57, 109) },
        { "Rainbow", Color.white },
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

    public static (Color, float, Color) GetThrusterColor(string name)
    {
        return _nameToThrusterColor.ContainsKey(name) ? (_nameToThrusterColor[name].Item1 / 191f, 
            _nameToThrusterColor[name].Item2, _nameToThrusterColor[name].Item3 / 255f) : _nameToThrusterColor["Default"];
    }

    public static Color GetIndicatorColor(string name)
    {
        return _nameToIndicatorColor.ContainsKey(name) ? _nameToIndicatorColor[name] / 255f : _nameToIndicatorColor["Default"];
    }
}
