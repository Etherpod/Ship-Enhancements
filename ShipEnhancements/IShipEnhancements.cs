using System;
using UnityEngine;
using UnityEngine.Events;

namespace ShipEnhancements;

public interface IShipEnhancements
{
    [Obsolete("This method is deprecated, please use AddTemperatureZone instead.")]
    public GameObject CreateTemperatureZone(float temperature, float outerRadius, float innerRadius,
        bool isShell = false, float shellCenterRadius = 0f, float shellCenterThickness = 0f, 
        string objectName = "TemperatureZone");

    public GameObject AddTemperatureZone(TemperatureZoneSettings settings);

    public struct TemperatureZoneSettings
    {
        /// <summary>
        /// The name of the object this temperature zone will go on.
        /// </summary>
        public string name;
        /// <summary>
        /// The object to place this temperature zone on.
        /// </summary>
        public Transform parent;

        /// <summary>
        /// The maximum temperature of this zone, between 100 and -100.
        /// </summary>
        public float temperature;
        /// <summary>
        /// The radius at which the temperature will start moving towards the maximum temperature.
        /// </summary>
        public float outerRadius;
        /// <summary>
        /// The radius at which the temperature reaches maximum.
        /// </summary>
        public float innerRadius;

        /// <summary>
        /// Makes the temperature zone hollow, reaching max temperature at shellCenterRadius and returning to zero at innerRadius.
        /// </summary>
        public bool isShell;
        /// <summary>
        /// The radius at which the temperature reaches maximum for a shell zone.
        /// </summary>
        public float shellCenterRadius;
        /// <summary>
        /// Adds a buffer on each side of the shellCenterRadius to make the area of max temperature wider. This is the total thickness of that area.
        /// </summary>
        public float shellCenterThickness;

        /// <summary>
        /// Allows temperature to change depending on which side of the planet you're on.
        /// </summary>
        public bool isDayNight;
        /// <summary>
        /// The temperature on the night side of the planet.
        /// </summary>
        public float nightTemperature;
        /// <summary>
        /// The total angle that makes up the twilight zone. A larger angle means it will take longer to fade from the day temperature to the night temperature.
        /// </summary>
        public float twilightAngle;
        /// <summary>
        /// Specifies what to use as the sun. Set this if your temperature zone is in a custom star system, otherwise leave it empty.
        /// </summary>
        public string customSunName;

        public TemperatureZoneSettings()
        {
            name = "TemperatureZone";
            customSunName = null;
        }

        public TemperatureZoneSettings(string name, Vector3 position, Transform parent, float temperature, float outerRadius, float innerRadius,
            bool isShell, float shellCenterRadius, float shellCenterThickness, bool isDayNight, float nightTemperature,
            float twilightAngle, string customSunName = null)
        {
            this.name = name;
            this.parent = parent;
            this.temperature = temperature;
            this.outerRadius = outerRadius;
            this.innerRadius = innerRadius;
            this.isShell = isShell;
            this.shellCenterRadius = shellCenterRadius;
            this.shellCenterThickness = shellCenterThickness;
            this.isDayNight = isDayNight;
            this.nightTemperature = nightTemperature;
            this.twilightAngle = twilightAngle;
            this.customSunName = customSunName;
        }
    }

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