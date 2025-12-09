using System;
using ShipEnhancements.ModMenu;
using UnityEngine;
using UnityEngine.Events;
using static ShipEnhancements.ShipEnhancements;

namespace ShipEnhancements;
public class ShipEnhancementsAPI : IShipEnhancements
{
    [Obsolete("This method is deprecated, please use AddTemperatureZone instead.")]
    public GameObject CreateTemperatureZone(float temperature, float outerRadius, float innerRadius, 
        bool isShell = false, float shellCenterRadius = 0f, float shellCenterThickness = 0f, 
        string objectName = "TemperatureZone")
    {
        GameObject tempZoneObj = LoadPrefab("Assets/ShipEnhancements/TemperatureZonePrefab.prefab");
        tempZoneObj.name = objectName;
        TemperatureZone tempZone = tempZoneObj.GetComponent<TemperatureZone>();
        if (innerRadius > outerRadius)
        {
            LogMessage($"Warning for temperature zone \"{objectName}\": innerRadius ({innerRadius}) larger than outerRadius ({outerRadius})", warning: true);
            innerRadius = 0f;
        }
        if (isShell)
        {
            if (shellCenterRadius < innerRadius || shellCenterRadius > outerRadius)
            {
                LogMessage($"Warning for temperature zone \"{objectName}\": shellCenterRadius ({shellCenterRadius}) outside of bounds (outerRadius: {outerRadius}, innerRadius: {innerRadius})", warning: true);
                shellCenterRadius = (innerRadius + outerRadius) / 2f;
            }
            float outerBuffer = outerRadius - shellCenterRadius;
            float innerBuffer = shellCenterRadius - innerRadius;
            if (shellCenterThickness > outerBuffer || shellCenterThickness > innerBuffer)
            {
                LogMessage($"Warning for temperature zone \"{objectName}\": shellCenterThickness ({shellCenterThickness}) extends out of bounds (outerRadius: {outerRadius}, innerRadius: {innerRadius})", warning: true);
                shellCenterThickness = Mathf.Min(outerBuffer, innerBuffer);
            }
        }
        tempZone.SetProperties(temperature, outerRadius, innerRadius, isShell, shellCenterRadius, shellCenterThickness);
        return tempZoneObj;
    }

    public GameObject AddTemperatureZone(string name, Transform parent, float temperature, float outerRadius,
        float innerRadius, bool isShell,
        float shellCenterRadius, float shellCenterThickness, bool isDayNight, float nightTemperature,
        float twilightAngle, string customSunName)
    {
        GameObject tempZoneObj = new GameObject(name);
        tempZoneObj.SetActive(false);
        tempZoneObj.transform.parent = parent;
        tempZoneObj.transform.localPosition = Vector3.zero;
        TemperatureZone tempZone = isDayNight ? tempZoneObj.AddComponent<DayNightTemperatureZone>() : tempZoneObj.AddComponent<TemperatureZone>();
        tempZoneObj.AddComponent<SphereShape>();
        tempZoneObj.AddComponent<OWTriggerVolume>();
        tempZoneObj.layer = LayerMask.NameToLayer("BasicEffectVolume");
        tempZoneObj.SetActive(true);
        if (innerRadius > outerRadius)
        {
            LogMessage($"Error when creating temperature zone \"{name}\": innerRadius ({innerRadius}) is larger than outerRadius ({outerRadius})", warning: true);
            innerRadius = 0f;
        }
        if (isShell)
        {
            if (shellCenterRadius < innerRadius || shellCenterRadius > outerRadius)
            {
                LogMessage($"Error when creating temperature zone \"{name}\": shellCenterRadius ({shellCenterRadius}) is outside of bounds (outerRadius: {outerRadius}, innerRadius: {innerRadius})", warning: true);
                shellCenterRadius = (innerRadius + outerRadius) / 2f;
            }
            float outerBuffer = outerRadius - shellCenterRadius;
            float innerBuffer = shellCenterRadius - innerRadius;
            if (shellCenterThickness > outerBuffer || shellCenterThickness > innerBuffer)
            {
                LogMessage($"Error when creating temperature zone \"{name}\": shellCenterThickness ({shellCenterThickness}) extends out of bounds (outerRadius: {outerRadius}, innerRadius: {innerRadius})", warning: true);
                shellCenterThickness = Mathf.Min(outerBuffer, innerBuffer);
            }
        }
        tempZone.SetProperties(temperature, outerRadius, innerRadius, isShell, 
            shellCenterRadius, shellCenterThickness, nightTemperature, twilightAngle, customSunName);
        return tempZoneObj;
    }

    public object GetSettingsProperty(string configName)
    {
        var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];
        foreach (Settings setting in allSettings)
        {
            if (setting.GetName() == configName)
            {
                return setting.GetProperty();
            }
        }
        LogMessage($"Could not find a Ship Enhancements setting named {configName}! From: IShipEnhancements.GetSettingsProperty()", error: true);
        return null;
    }

    public void SetSettingsProperty(string configName, object value)
    {
        var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];
        foreach (Settings setting in allSettings)
        {
            if (setting.GetName() == configName)
            {
                setting.SetProperty(value);
                return;
            }
        }
        LogMessage($"Could not find a Ship Enhancements setting named {configName}! From: IShipEnhancements.SetSettingsProperty()", error: true);
    }

    public void SetSettingsOptionVisible(string configName, bool visible, bool forceRefresh = false)
    {
        if (configName == "preset")
        {
            SEMenuManager.HidePreset = visible;
            if (forceRefresh)
            {
                SEMenuManager.RedrawSettingsMenu();
            }
            return;
        }
        
        var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];
        foreach (Settings setting in allSettings)
        {
            if (setting.GetName() == configName)
            {
                if (visible && SEMenuManager.HiddenSettings.Contains(setting))
                {
                    SEMenuManager.HiddenSettings.Remove(setting);
                }
                else if (!visible && !SEMenuManager.HiddenSettings.Contains(setting))
                {
                    SEMenuManager.HiddenSettings.Add(setting);
                }

                if (forceRefresh)
                {
                    SEMenuManager.RedrawSettingsMenu();
                }
                return;
            }
        }
    }

    public void HideAllSettings(bool forceRefresh = false)
    {
        SEMenuManager.HiddenSettings.Clear();
        var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];
        foreach (var setting in allSettings)
        {
            SEMenuManager.HiddenSettings.Add(setting);
        }

        SEMenuManager.HidePreset = true;

        if (forceRefresh)
        {
            SEMenuManager.RedrawSettingsMenu();
        }
    }

    public void ShowAllSettings(bool forceRefresh = false)
    {
        SEMenuManager.HiddenSettings.Clear();
        SEMenuManager.HidePreset = false;

        if (forceRefresh)
        {
            SEMenuManager.RedrawSettingsMenu();
        }
    }

    public void ResetSettings()
    {
        SettingExtensions.ResetCustomSettings();
    }

    public UnityEvent GetPreShipInitializeEvent() => Instance.PreShipInitialize;

    public UnityEvent GetPostShipInitializeEvent() => Instance.PostShipInitialize;
}
