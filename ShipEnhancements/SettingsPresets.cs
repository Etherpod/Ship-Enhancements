using OWML.Common;
using System.Collections.Generic;
using System;

namespace ShipEnhancements;

public static class SettingsPresets
{
    public static readonly Dictionary<string, object> VanillaSettings = new Dictionary<string, object>()
    {
        { "disableGravityCrystal", false },
        { "disableEjectButton", false },
        { "disableHeadlights", false },
        { "disableLandingCamera", false },
        { "disableShipLights", false },
        { "disableShipOxygen", false },
        { "oxygenDrainMultiplier", 1f },
        { "fuelDrainMultiplier", 1f },
        { "shipDamageMultiplier", 1f },
        { "shipDamageSpeedMultiplier", 1f },
        { "shipOxygenRefill", false },
        { "disableShipRepair", false },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", false },
        { "disableWaterAutoRoll", false },
        { "enableThrustModulator", false },
        { "temperatureZonesAmount", "None" },
        { "enableTemperatureDamage", false },
        { "enableShipFuelTransfer", false },
        { "enableJetpackRefuelDrain", false },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 1f },
        { "oxygenRefillMultiplier", 1f },
        { "temperatureDamageMultiplier", 1f },
        { "temperatureResistanceMultiplier", 1f },
        { "enableAutoHatch", false },
    };

    public static readonly Dictionary<string, object> MinimalSettings = new Dictionary<string, object>()
    {
        { "disableGravityCrystal", true },
        { "disableEjectButton", false },
        { "disableHeadlights", true },
        { "disableLandingCamera", true },
        { "disableShipLights", true },
        { "disableShipOxygen", true },
        { "oxygenDrainMultiplier", 1f },
        { "fuelDrainMultiplier", 1.5f },
        { "shipDamageMultiplier", 1f },
        { "shipDamageSpeedMultiplier", 0.8f },
        { "shipOxygenRefill", false },
        { "disableShipRepair", false },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", false },
        { "disableWaterAutoRoll", false },
        { "enableThrustModulator", false },
        { "temperatureZonesAmount", "None" },
        { "enableTemperatureDamage", false },
        { "enableShipFuelTransfer", false },
        { "enableJetpackRefuelDrain", false },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 1f },
        { "oxygenRefillMultiplier", 1f },
        { "temperatureDamageMultiplier", 1f },
        { "temperatureResistanceMultiplier", 1f },
        { "enableAutoHatch", true },
    };

    public static readonly Dictionary<string, object> HardcoreSettings = new Dictionary<string, object>()
    {
        { "disableGravityCrystal", false },
        { "disableEjectButton", false },
        { "disableHeadlights", false },
        { "disableLandingCamera", false },
        { "disableShipLights", false },
        { "disableShipOxygen", false },
        { "oxygenDrainMultiplier", 400f },
        { "fuelDrainMultiplier", 6f },
        { "shipDamageMultiplier", 2.5f },
        { "shipDamageSpeedMultiplier", 0.60f },
        { "shipOxygenRefill", true },
        { "disableShipRepair", true },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", false },
        { "disableWaterAutoRoll", false },
        { "enableThrustModulator", false },
        { "temperatureZonesAmount", "All" },
        { "enableTemperatureDamage", true },
        { "enableShipFuelTransfer", true },
        { "enableJetpackRefuelDrain", true },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 0.8f },
        { "oxygenRefillMultiplier", 0.75f },
        { "temperatureDamageMultiplier", 2f },
        { "temperatureResistanceMultiplier", 1.5f },
        { "enableAutoHatch", false },
    };

    public static readonly Dictionary<string, object> WandererSettings = new Dictionary<string, object>()
    {
        { "disableGravityCrystal", false },
        { "disableEjectButton", false },
        { "disableHeadlights", false },
        { "disableLandingCamera", false },
        { "disableShipLights", false },
        { "disableShipOxygen", false },
        { "oxygenDrainMultiplier", 25f },
        { "fuelDrainMultiplier", 1f },
        { "shipDamageMultiplier", 0.5f },
        { "shipDamageSpeedMultiplier", 1.2f },
        { "shipOxygenRefill", true },
        { "disableShipRepair", false },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", false },
        { "disableWaterAutoRoll", false },
        { "enableThrustModulator", false },
        { "temperatureZonesAmount", "None" },
        { "enableTemperatureDamage", false },
        { "enableShipFuelTransfer", false },
        { "enableJetpackRefuelDrain", true },
        { "disableReferenceFrame", true },
        { "disableMapMarkers", true },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 1f },
        { "oxygenRefillMultiplier", 1f },
        { "temperatureDamageMultiplier", 1f },
        { "temperatureResistanceMultiplier", 1f },
        { "enableAutoHatch", false },
    };

    public static readonly Dictionary<string, object> PandemoniumSettings = new Dictionary<string, object>()
    {
        { "disableGravityCrystal", false },
        { "disableEjectButton", false },
        { "disableHeadlights", false },
        { "disableLandingCamera", false },
        { "disableShipLights", false },
        { "disableShipOxygen", false },
        { "oxygenDrainMultiplier", 120f },
        { "fuelDrainMultiplier", 6f },
        { "shipDamageMultiplier", 5f },
        { "shipDamageSpeedMultiplier", 1.5f },
        { "shipOxygenRefill", true },
        { "disableShipRepair", false },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", true },
        { "disableWaterAutoRoll", true },
        { "enableThrustModulator", false },
        { "temperatureZonesAmount", "All" },
        { "enableTemperatureDamage", true },
        { "enableShipFuelTransfer", true },
        { "enableJetpackRefuelDrain", true },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 0.3f },
        { "fuelTransferMultiplier", 2f },
        { "oxygenRefillMultiplier", 2f },
        { "temperatureDamageMultiplier", 4f },
        { "temperatureResistanceMultiplier", 0.6f },
        { "enableAutoHatch", false },
    };

    public static Dictionary<PresetName, Dictionary<string, object>> presetDicts { get; private set; }

    public static Dictionary<string, Dictionary<PresetName, object>> settingsPresets { get; private set; }

    private static bool _initialized = false;

    public enum PresetName
    {
        Vanilla,
        Minimal,
        Hardcore,
        Wanderer,
        Pandemonium,
        Custom
    }

    public static void InitializePresets()
    {
        presetDicts = new()
        {
            { PresetName.Vanilla, VanillaSettings },
            { PresetName.Minimal, MinimalSettings },
            { PresetName.Hardcore, HardcoreSettings },
            { PresetName.Wanderer, WandererSettings },
            { PresetName.Pandemonium, PandemoniumSettings },
        };

        settingsPresets = new();

        foreach (KeyValuePair<PresetName, Dictionary<string, object>> pair in presetDicts)
        {
            foreach (KeyValuePair<string, object> setting in pair.Value)
            {
                pair.Key.AddSetting(setting.Key, setting.Value);
            }
        }

        _initialized = true;
        ShipEnhancements.Instance.Configure(ShipEnhancements.Instance.ModHelper.Config);
    }

    public static void AddSetting(this PresetName preset, string name, object value)
    {

        // Is this setting in the dictionary
        if (settingsPresets.ContainsKey(name))
        {
            // Does this setting already contain a value for this preset
            if (settingsPresets[name].ContainsKey(preset))
            {
                // Update value
                settingsPresets[name][preset] = value;
            }
            else
            {
                // Add new preset value
                settingsPresets[name].Add(preset, value);
            }
        }
        // Add setting to dictionary
        else
        {
            settingsPresets.Add(name, new Dictionary<PresetName, object> { { preset, value } });
        }
    }

    public static void ApplyPreset(PresetName preset, IModConfig config)
    {
        if (preset == PresetName.Custom) return;
        foreach (KeyValuePair<string, object> setting in presetDicts[preset])
        {
            config.SetSettingsValue(setting.Key, setting.Value);
        }
    }

    public static PresetName GetPresetFromConfig(string configPreset)
    {
        if (Enum.TryParse(configPreset, out PresetName preset))
        {
            return preset;
        }
        ShipEnhancements.WriteDebugMessage($"Failed to convert {configPreset} to preset.", error: true);
        return PresetName.Custom;
    }

    public static string GetName(this PresetName preset)
    {
        return preset.ToString();
    }

    public static object GetPresetSetting(this PresetName preset, string setting)
    {
        /*switch (preset)
        {
            case PresetName.Vanilla:
                if (VanillaSettings.ContainsKey(setting))
                {
                    return VanillaSettings[setting];
                }
                else
                {
                    ShipEnhancements.WriteDebugMessage($"No setting found for Vanilla preset: {setting}", error: true);
                }
                break;

            case PresetName.Barebones:
                if (BarebonesSettings.ContainsKey(setting))
                {
                    return BarebonesSettings[setting];
                }
                else
                {
                    ShipEnhancements.WriteDebugMessage($"No setting found for Barebones preset: {setting}", error: true);
                }
                break;

            case PresetName.Hardcore:
                if (HardcoreSettings.ContainsKey(setting))
                {
                    return HardcoreSettings[setting];
                }
                else
                {
                    ShipEnhancements.WriteDebugMessage($"No setting found for Hardcore preset: {setting}", error: true);
                }
                break;
        }
        return null;*/
        return settingsPresets[setting][preset];
    }

    public static bool Initialized()
    {
        return _initialized;
    }
}
