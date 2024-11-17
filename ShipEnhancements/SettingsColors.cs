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

    private static Dictionary<string, ((Color, float) hull, (Color, float) component, Color alarm, (Color, float) alarmLit, Color light)> _nameToDamageColor = new()
    {
        { "Default", ((Color.white, 1f), (Color.white, 1f), Color.white, (Color.white, 1f), Color.white) },
        {
            "Orange",
            ((new Color(191, 24, 2), 1.2f),
            (new Color(191, 35, 0), 1.4f),
            new Color(190, 113, 71),
            (new Color(191, 99, 57), 0.89f),
            new Color(255, 140, 0))
        },
        {
            "Yellow",
            ((new Color(191, 99, 0), 0.82f),
            (new Color(191, 108, 0), 1.4f),
            new Color(48, 54, 77),
            (new Color(191, 154, 59), 0.87f),
            new Color(255, 238, 0))
        },
        {
            "Green",
            ((new Color(37, 149, 0), 0f),
            (new Color(111, 235, 0), 0f),
            new Color(141, 195, 106),
            (new Color(77, 191, 28), 0.8f),
            new Color(163, 255, 30))
        },
        {
            "Outer Wilds Beta",
            ((new Color(0, 29, 4), 0f),
            (new Color(0, 29, 4), 0f),
            new Color(1, 58, 74),
            (new Color(191, 82, 81), 0.8f),
            new Color(255, 0, 0))
        },
        {
            "Ghostly Green",
            ((new Color(0, 212, 42), 0f),
            (new Color(0, 222, 85), 0f),
            new Color(99, 204, 138),
            (new Color(39, 191, 84), 0.72f),
            new Color(30, 255, 115))
        },
        {
            "Turquoise",
            ((new Color(0, 197, 191), 0f),
            (new Color(0, 172, 226), 0f),
            new Color(130, 200, 204),
            (new Color(55, 191, 182), 0.73f),
            new Color(0, 255, 253))
        },
        {
            "Blue",
            ((new Color(20, 105, 244), 0f),
            (new Color(16, 49, 191), 1.33f),
            new Color(79, 137, 192),
            (new Color(55, 99, 191), 0.95f),
            new Color(0, 163, 255))
        },
        {
            "Dark Blue",
            ((new Color(0, 14, 79), 0f),
            (new Color(10, 33, 135), 0f),
            new Color(74, 103, 192),
            (new Color(32, 61, 191), 1.05f),
            new Color(0, 90, 255))
        },
        {
            "Nomaian Blue",
            ((new Color(28, 38, 204), 0f),
            (new Color(21, 18, 191), 1.37f),
            new Color(120, 133, 221),
            (new Color(54, 58, 191), 1.2f),
            new Color(88, 0, 255))
        },
        {
            "Purple",
            ((new Color(72, 0, 255), 0f),
            (new Color(28, 0, 191), 1.6f),
            new Color(124, 81, 178),
            (new Color(74, 54, 191), 1.16f),
            new Color(110, 0, 255))
        },
        {
            "Lavender",
            ((new Color(134, 39, 191), 0.5f),
            (new Color(74, 30, 191), 1.5f),
            new Color(168, 120, 195),
            (new Color(108, 88, 191), 1.08f),
            new Color(214, 160, 255))
        },
        {
            "Pink",
            ((new Color(191, 18, 94), 1.11f),
            (new Color(191, 16, 111), 1.75f),
            new Color(209, 84, 201),
            (new Color(191, 81, 185), 0.82f),
            new Color(255, 83, 216))
        },
        { "Rainbow", ((Color.white, 1f), (Color.white, 1f), Color.white, (Color.white, 1f), Color.white) },
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

    public static (Color hull, Color component, Color alarm, Color alarmLit, Color light) GetDamageColor(string name)
    {
        if (_nameToDamageColor.ContainsKey(name))
        {
            ((Color, float) hull, (Color, float) component, Color alarm, (Color, float) alarmLit, Color light) = _nameToDamageColor[name];
            (Color hull, Color component, Color alarm, Color alarmLit, Color light) color;
            color.hull = GetUnitColor(hull);
            ShipEnhancements.WriteDebugMessage(color.hull);
            color.component = GetUnitColor(component);
            color.alarm = GetUnitColor((alarm, 0f));
            color.alarmLit = GetUnitColor(alarmLit);
            color.light = GetUnitColor((light, 0f));

            return color;
        }

        return (Color.white, Color.white, Color.white, Color.white, Color.white);
    }

    private static Color GetUnitColor((Color, float) color)
    {
        Color newColor;
        if (color.Item2 > 0f)
        {
            newColor = color.Item1 / 191f * Mathf.Pow(2, color.Item2);
        }
        else
        {
            newColor = color.Item1 / 255f;
        }
        newColor.a = 1f;
        return newColor;
    }
}
