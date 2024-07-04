using OWML.Common;
using System.Collections.Generic;
using System.Linq;

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
    };

    public static readonly Dictionary<string, object> BarebonesSettings = new Dictionary<string, object>()
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
    };

    public static Dictionary<string, Dictionary<PresetName, object>> settingsPresets { get; private set; }

    public enum PresetName
    {
        Vanilla,
        Barebones,
        Hardcore,
        Custom
    }

    public static void InitializePresets()
    {
        settingsPresets = new();

        VanillaSettings.ForEach(setting => 
        {
            PresetName.Vanilla.AddSetting(setting.Key, setting.Value); 
        });
        BarebonesSettings.ForEach(setting => 
        {
            PresetName.Vanilla.AddSetting(setting.Key, setting.Value); 
        });
        HardcoreSettings.ForEach(setting => 
        {
            PresetName.Hardcore.AddSetting(setting.Key, setting.Value); 
        });
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
        ShipEnhancements.WriteDebugMessage("Applying");
        switch (preset)
        {
            case PresetName.Vanilla:
                ShipEnhancements.WriteDebugMessage("Vanilla");
                VanillaSettings.ForEach(setting =>
                {
                    ShipEnhancements.WriteDebugMessage("Selecting");
                    ShipEnhancements.WriteDebugMessage("Before: " + ShipEnhancements.Instance.ModHelper.Config.GetSettingsValue<object>(setting.Key));
                    config.SetSettingsValue(setting.Key, setting.Value);
                    ShipEnhancements.WriteDebugMessage("After: " + ShipEnhancements.Instance.ModHelper.Config.GetSettingsValue<object>(setting.Key));
                });
                break;

            case PresetName.Barebones:
                ShipEnhancements.WriteDebugMessage("Barebones");
                BarebonesSettings.ForEach(setting => 
                {
                    ShipEnhancements.WriteDebugMessage("Selecting");
                    config.SetSettingsValue(setting.Key, setting.Value); 
                });
                break;

            case PresetName.Hardcore:
                ShipEnhancements.WriteDebugMessage("Hardcore");
                HardcoreSettings.ForEach(setting => 
                {
                    ShipEnhancements.WriteDebugMessage("Selecting");
                    config.SetSettingsValue(setting.Key, setting.Value); 
                });
                break;
        }
    }

    public static PresetName GetPresetName(string configPreset)
    {
        if (configPreset == "Vanilla")
        {
            return PresetName.Vanilla;
        }
        else if (configPreset == "Bare-bones")
        {
            return PresetName.Barebones;
        }
        else if (configPreset == "Hardcore")
        {
            return PresetName.Hardcore;
        }
        else
        {
            return PresetName.Custom;
        }
    }
}
