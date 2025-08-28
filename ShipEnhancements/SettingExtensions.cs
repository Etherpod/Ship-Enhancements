using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public static class SettingExtensions
{
    private static Dictionary<Settings, (object value, object property)> settingValues = null;

    private static Dictionary<Settings, object> savedCustomSettings;

    public static Dictionary<string, string> customObjLabels = new();

    private static void InitializeValues()
    {
        settingValues = [];
        var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];

        foreach (Settings setting in allSettings)
        {
            settingValues.Add(setting, (null, null));
        }

        savedCustomSettings = new(settingValues.Count);
    }

    public static string GetName(this Settings setting)
    {
        return setting.ToString();
    }

    public static object GetValue(this Settings setting)
    {
        if (settingValues == null) InitializeValues();

        JValue value = (JValue)settingValues[setting].value;
        if (value.Type == JTokenType.Boolean)
        {
            return Convert.ToBoolean(value);
        }
        else if (value.Type == JTokenType.Float)
        {
            return float.Parse(value.ToString());
        }
        else if (value.Type == JTokenType.Integer)
        {
            return (float)int.Parse(value.ToString());
        }
        else if (value.Type == JTokenType.String)
        {
            return value.ToString();
        }
        return value;
    }

    public static void SetValue(this Settings setting, object value)
    {
        if (settingValues == null) InitializeValues();
        
        if (settingValues[setting].property == null)
        {
            settingValues[setting] = (value, value);
        }
        else
        {
            settingValues[setting] = (value, settingValues[setting].property);
        }
    }

    public static object GetProperty(this Settings setting)
    {
        if (settingValues == null) InitializeValues();

        if (settingValues[setting].property is JValue)
        {
            JValue value = (JValue)settingValues[setting].property;
            if (value.Type == JTokenType.Boolean)
            {
                return Convert.ToBoolean(value);
            }
            else if (value.Type == JTokenType.Float)
            {
                return float.Parse(value.ToString());
            }
            else if (value.Type == JTokenType.Integer)
            {
                return (float)int.Parse(value.ToString());
            }
            else if (value.Type == JTokenType.String)
            {
                return value.ToString();
            }
            return value;
        }

        return settingValues[setting].property;
    }

    public static void SetProperty(this Settings setting, object value)
    {
        if (settingValues == null) InitializeValues();

        settingValues[setting] = (settingValues[setting].value, value);
    }

    public static object ConvertJValue(object obj)
    {
        if (obj is not JValue) return null;

        JValue value = (JValue)obj;
        if (value.Type == JTokenType.Boolean)
        {
            return Convert.ToBoolean(value);
        }
        else if (value.Type == JTokenType.Float)
        {
            return float.Parse(value.ToString());
        }
        else if (value.Type == JTokenType.Integer)
        {
            return (float)int.Parse(value.ToString());
        }
        else if (value.Type == JTokenType.String)
        {
            return value.ToString();
        }
        return value;
    }

    public static Type GetType(this Settings setting)
    {
        if (settingValues == null) InitializeValues();

        return settingValues[setting].GetType();
    }

    public static void SaveCustomSettings()
    {
        if (settingValues == null) InitializeValues();

        foreach (var (setting, value) in settingValues)
        {
            if (SettingsPresets.VanillaPlusSettings.ContainsKey(setting.GetName()))
            {
                savedCustomSettings[setting] = value.value;
            }
        }
    }

    public static void LoadCustomSettings()
    {
        if (settingValues == null) InitializeValues();

        foreach (var (setting, value) in savedCustomSettings)
        {
            if (SettingsPresets.VanillaPlusSettings.ContainsKey(setting.GetName()))
            {
                settingValues[setting] = (value, settingValues[setting].property);
            }
        }
    }

    public static void ResetCustomSettings()
    {
        if (settingValues == null) InitializeValues();

        foreach (var (setting, value) in settingValues)
        {
            if (setting == Settings.addWaterTank)
            {
                ShipEnhancements.WriteDebugMessage("gabagool");
            }
            savedCustomSettings[setting] = Instance.ModHelper.DefaultConfig.GetSettingsValue<object>(setting.GetName());
            Instance.ModHelper.Config.SetSettingsValue(setting.GetName(), savedCustomSettings[setting]);
        }

        Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
        {
            Instance.RedrawSettingsMenu();
        });
    }

    public static T AsEnum<T>(this string enumName) where T : struct =>
    Enum.TryParse<T>(enumName, out var result) ? result 
        : throw new ArgumentException($"Enum '{enumName}' does not exist.");

    public static bool IsEnum<T>(this string enumName) where T : struct =>
    Enum.TryParse<T>(enumName, out var result);

    public static ColorHSV AsHSV(this Color color)
    {
        Color.RGBToHSV(color, out float H, out float S, out float V);
        return new ColorHSV(H, S, V);
    }

    public static Color AsRGB(this ColorHSV color)
    {
        return Color.HSVToRGB(color.h, color.s, color.v);
    }
}
