using UnityEngine;

namespace ShipEnhancements;
public class ShipEnhancementsAPI : IShipEnhancements
{
    public GameObject CreateTemperatureZone(float temperature, float outerRadius, float innerRadius, 
        bool isShell = false, float shellCenterRadius = 0f, float shellCenterThickness = 0f, 
        string objectName = "TemperatureZone")
    {
        GameObject tempZoneObj = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/TemperatureZonePrefab.prefab");
        tempZoneObj.name = objectName;
        TemperatureZone tempZone = tempZoneObj.GetComponent<TemperatureZone>();
        if (innerRadius > outerRadius)
        {
            ShipEnhancements.WriteDebugMessage($"Warning for temperature zone \"{objectName}\": innerRadius ({innerRadius}) larger than outerRadius ({outerRadius})", warning: true);
            innerRadius = 0f;
        }
        if (isShell)
        {
            if (shellCenterRadius < innerRadius || shellCenterRadius > outerRadius)
            {
                ShipEnhancements.WriteDebugMessage($"Warning for temperature zone \"{objectName}\": shellCenterRadius ({shellCenterRadius}) outside of bounds (outerRadius: {outerRadius}, innerRadius: {innerRadius})", warning: true);
                shellCenterRadius = (innerRadius + outerRadius) / 2f;
            }
            float outerBuffer = outerRadius - shellCenterRadius;
            float innerBuffer = shellCenterRadius - innerRadius;
            if (shellCenterThickness > outerBuffer || shellCenterThickness > innerBuffer)
            {
                ShipEnhancements.WriteDebugMessage($"Warning for temperature zone \"{objectName}\": shellCenterThickness ({shellCenterThickness}) extends out of bounds (outerRadius: {outerRadius}, innerRadius: {innerRadius})", warning: true);
                shellCenterThickness = Mathf.Min(outerBuffer, innerBuffer);
            }
        }
        tempZone.SetProperties(temperature, outerRadius, innerRadius, isShell, shellCenterRadius, shellCenterThickness);
        return tempZoneObj;
    }
}
