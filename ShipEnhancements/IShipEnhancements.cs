using System;
using UnityEngine;

namespace ShipEnhancements;

public interface IShipEnhancements
{
    /// <summary>
    /// Creates a custom temperature zone.
    /// </summary>
    /// <param name="temperature">The temperature of the zone. Needs to be between 100 and -100.</param>
    /// <param name="outerRadius">The distance from the center where the temperature starts to increase or decrease.</param>
    /// <param name="innerRadius">The distance from the center where the temperature stops increasing or decreasing. Needs to be less than the outer radius.</param>
    /// <param name="isShell">Determines whether the zone should be hollow in the inside. In other words, the temperature will return to zero when you reach the inner radius instead of remaining at the highest or lowest temperature.</param>
    /// <param name="shellCenterRadius">The distance from the center where the temperature is the highest or lowest. Needs to be between the outer radius and the inner radius.</param>
    /// <param name="shellCenterThickness">Makes the shell center radius cover a larger area.</param>
    /// <returns>A prefab of a temperature zone, which needs to be instantiated manually.</returns>
    public GameObject CreateTemperatureZone(float temperature, float outerRadius, float innerRadius,
        bool isShell = false, float shellCenterRadius = 0f, float shellCenterThickness = 0f, 
        string objectName = "TemperatureZone");
}