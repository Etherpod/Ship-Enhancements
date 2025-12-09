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

	/// <summary>
	/// Creates a temperature zone using the given parameters.
	/// </summary>
	/// <param name="name">The name of the temperature zone. Defaults to TemperatureZone.</param>
	/// <param name="parent">The name of the GameObject to parent the temperature zone to.</param>
	/// <param name="temperature">The maximum temperature of this zone, between 100 and -100.</param>
	/// <param name="outerRadius">The radius at which the temperature will start moving towards the maximum temperature.</param>
	/// <param name="innerRadius">The radius at which the temperature reaches maximum.</param>
	/// <param name="isShell">Makes the temperature zone hollow, reaching max temperature at shellCenterRadius and returning to zero at innerRadius.</param>
	/// <param name="shellCenterRadius">The radius at which the temperature reaches maximum for a shell zone.</param>
	/// <param name="shellCenterThickness">Adds a buffer on each side of the shellCenterRadius to make the area of max temperature wider. This is the total thickness of that area.</param>
	/// <param name="isDayNight">Allows temperature to change depending on which side of the planet you're on.</param>
	/// <param name="nightTemperature">The temperature on the night side of the planet.</param>
	/// <param name="twilightAngle">The total angle that makes up the twilight zone. A larger angle means it will take longer to fade from the day temperature to the night temperature.</param>
	/// <param name="customSunName">Specifies what to use as the sun. Set this if your temperature zone is in a custom star system, otherwise leave it empty.</param>
	/// <returns>The created temperature zone.</returns>
	public GameObject AddTemperatureZone(string name, Transform parent, float temperature, float outerRadius,
		float innerRadius, bool isShell,
		float shellCenterRadius, float shellCenterThickness, bool isDayNight, float nightTemperature,
		float twilightAngle, string customSunName);

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
	/// Sets every setting to its default config value.
	/// </summary>
	public void ResetSettings();

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