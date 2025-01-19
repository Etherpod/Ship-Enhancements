using System;
using UnityEngine;
using UnityEngine.Events;
using static ShipEnhancements.ShipEnhancements;

namespace ShipEnhancements;
public class ShipEnhancementsAPI : IShipEnhancements
{
    public GameObject CreateTemperatureZone(float temperature, float outerRadius, float innerRadius, 
        bool isShell = false, float shellCenterRadius = 0f, float shellCenterThickness = 0f, 
        string objectName = "TemperatureZone")
    {
        GameObject tempZoneObj = LoadPrefab("Assets/ShipEnhancements/TemperatureZonePrefab.prefab");
        tempZoneObj.name = objectName;
        TemperatureZone tempZone = tempZoneObj.GetComponent<TemperatureZone>();
        if (innerRadius > outerRadius)
        {
            WriteDebugMessage($"Warning for temperature zone \"{objectName}\": innerRadius ({innerRadius}) larger than outerRadius ({outerRadius})", warning: true);
            innerRadius = 0f;
        }
        if (isShell)
        {
            if (shellCenterRadius < innerRadius || shellCenterRadius > outerRadius)
            {
                WriteDebugMessage($"Warning for temperature zone \"{objectName}\": shellCenterRadius ({shellCenterRadius}) outside of bounds (outerRadius: {outerRadius}, innerRadius: {innerRadius})", warning: true);
                shellCenterRadius = (innerRadius + outerRadius) / 2f;
            }
            float outerBuffer = outerRadius - shellCenterRadius;
            float innerBuffer = shellCenterRadius - innerRadius;
            if (shellCenterThickness > outerBuffer || shellCenterThickness > innerBuffer)
            {
                WriteDebugMessage($"Warning for temperature zone \"{objectName}\": shellCenterThickness ({shellCenterThickness}) extends out of bounds (outerRadius: {outerRadius}, innerRadius: {innerRadius})", warning: true);
                shellCenterThickness = Mathf.Min(outerBuffer, innerBuffer);
            }
        }
        tempZone.SetProperties(temperature, outerRadius, innerRadius, isShell, shellCenterRadius, shellCenterThickness);
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
        WriteDebugMessage($"Could not find a Ship Enhancements setting named {configName}! From: IShipEnhancements.GetSettingsProperty()", error: true);
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
        WriteDebugMessage($"Could not find a Ship Enhancements setting named {configName}! From: IShipEnhancements.SetSettingsProperty()", error: true);
    }

    public UnityEvent GetPreShipInitializeEvent() => Instance.PreShipInitialize;

    public UnityEvent GetPostShipInitializeEvent() => Instance.PostShipInitialize;
}
