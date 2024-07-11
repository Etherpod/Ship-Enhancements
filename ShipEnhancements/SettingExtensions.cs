using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using static ShipEnhancements.ShipEnhancements;

namespace ShipEnhancements;

public static class SettingExtensions
{
    private static Dictionary<Settings, object> settingValues = new Dictionary<Settings, object>()
    {
        { Settings.disableGravityCrystal, false },
        { Settings.disableEjectButton, false },
        { Settings.disableHeadlights, false },
        { Settings.disableLandingCamera, false },
        { Settings.disableShipLights, false },
        { Settings.disableShipOxygen, false },
        { Settings.oxygenDrainMultiplier, 1f },
        { Settings.fuelDrainMultiplier, 1f },
        { Settings.shipDamageMultiplier, 1f },
        { Settings.shipDamageSpeedMultiplier, 1f },
        { Settings.shipOxygenRefill, false },
        { Settings.disableShipRepair, false },
        { Settings.enableGravityLandingGear, false },
        { Settings.disableAirAutoRoll, false },
        { Settings.disableWaterAutoRoll, false },
        { Settings.enableThrustModulator, false },
        { Settings.temperatureZonesAmount, "None" },
        { Settings.hullTemperatureDamage, false },
        { Settings.enableShipFuelTransfer, false },
        { Settings.enableJetpackRefuelDrain, false },
        { Settings.disableReferenceFrame, false },
        { Settings.disableMapMarkers, false },
        { Settings.gravityMultiplier, 1f },
        { Settings.fuelTransferMultiplier, 1f },
        { Settings.oxygenRefillMultiplier, 1f },
        { Settings.temperatureDamageMultiplier, 1f },
        { Settings.temperatureResistanceMultiplier, 1f },
        { Settings.enableAutoHatch, false },
        { Settings.oxygenTankDrainMultiplier, 1f },
        { Settings.fuelTankDrainMultiplier, 1f },
        { Settings.componentTemperatureDamage, false },
    };

    public static string GetName(this Settings setting)
    {
        return setting.ToString();
    }

    public static object GetValue(this Settings setting)
    {
        JValue value = (JValue)settingValues[setting];
        if (value.Type == JTokenType.Boolean)
        {
            return Convert.ToBoolean(value);
        }
        else if (value.Type == JTokenType.Float)
        {
            return float.Parse(value.ToString());
        }
        else if (value.Type == JTokenType.String)
        {
            return value.ToString();
        }
        return value;
    }

    public static Type GetType(this Settings setting)
    {
        return settingValues[setting].GetType();
    }

    public static void SetValue(this Settings setting, object value)
    {
        settingValues[setting] = value;
    }
}
