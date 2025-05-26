using UnityEngine;
using UnityEngine.Events;

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

    /// <summary>
    /// Gets the in-game value of the specified config setting.
    /// </summary>
    /// <param name="configName">The name of the setting. It should be in lower camel case.</param>
    /// <returns>The current in-game value of the config setting, not the value as seen in the config.</returns>
    public object GetSettingsProperty(string configName);

    /// <summary>
    /// Sets the in-game value of the specified config setting. Doesn't affect the displayed value in the mod config.
    /// </summary>
    /// <param name="configName">The name of the setting. It should be in lower camel case.</param>
    /// <param name="value">The value to assign to the setting. This will reset every time the scene loads.</param>
    public void SetSettingsProperty(string configName, object value);

    /// <summary>
    /// Sets the visibility of a config setting in the mod settings menu. This resets when the game closes.
    /// </summary>
    /// <param name="configName">The name of the setting. It should be in lower camel case.</param>
    /// <param name="visible">Should this setting be visible in the mod settings menu?</param>
    /// <param name="forceRefresh">Set this to true if the mod settings menu is open when you call the method.</param>
    public void SetSettingsOptionVisible(string configName, bool visible, bool forceRefresh = false);

    /// <summary>
    /// Hides all of the config settings in the mod settings menu. This resets when the game closes.
    /// </summary>
    /// <param name="forceRefresh">Set this to true if the mod settings menu is open when you call the method.</param>
    public void HideAllSettings(bool forceRefresh = false);

    /// <summary>
    /// Shows all of the config settings in the mod settings menu. This resets when the game closes.
    /// </summary>
    /// <param name="forceRefresh">Set this to true if the mod settings menu is open when you call the method.</param>
    public void ShowAllSettings(bool forceRefresh = false);

    /// <summary>
    /// Gets the event that is invoked before Ship Enhancements makes any changes to the ship.
    /// </summary>
    /// <returns>The UnityEvent event that will be invoked.</returns>
    public UnityEvent GetPreShipInitializeEvent();

    /// <summary>
    /// Gets the event that is invoked after Ship Enhancements finishes making changes to the ship.
    /// </summary>
    /// <returns>The UnityEvent event that will be invoked</returns>
    public UnityEvent GetPostShipInitializeEvent();
}