using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using static ShipEnhancements.ShipEnhancements;

namespace ShipEnhancements;

public static class SettingExtensions
{
    private static Dictionary<Settings, (object, object)> settingValues = new Dictionary<Settings, (object, object)>()
    {
        { Settings.disableGravityCrystal, (false, false) },
        { Settings.disableEjectButton, (false, false) },
        { Settings.disableHeadlights, (false, false) },
        { Settings.disableLandingCamera, (false, false) },
        { Settings.disableShipLights, (false, false) },
        { Settings.disableShipOxygen, (false, false) },
        { Settings.oxygenDrainMultiplier, (1f, 1f) },
        { Settings.fuelDrainMultiplier, (1f, 1f) },
        { Settings.shipDamageMultiplier, (1f, 1f) },
        { Settings.shipDamageSpeedMultiplier, (1f, 1f) },
        { Settings.shipOxygenRefill, (false, false) },
        { Settings.disableShipRepair, (false, false) },
        { Settings.enableGravityLandingGear, (false, false) },
        { Settings.disableAirAutoRoll, (false, false) },
        { Settings.disableWaterAutoRoll, (false, false) },
        { Settings.enableThrustModulator, (false, false) },
        { Settings.temperatureZonesAmount, ("None", "None") },
        { Settings.hullTemperatureDamage, (false, false) },
        { Settings.enableShipFuelTransfer, (false, false) },
        { Settings.enableJetpackRefuelDrain, (false, false) },
        { Settings.disableReferenceFrame, (false, false) },
        { Settings.disableMapMarkers, (false, false) },
        { Settings.gravityMultiplier, (1f, 1f) },
        { Settings.fuelTransferMultiplier, (1f, 1f) },
        { Settings.oxygenRefillMultiplier, (1f, 1f) },
        { Settings.temperatureDamageMultiplier, (1f, 1f) },
        { Settings.temperatureResistanceMultiplier, (1f, 1f) },
        { Settings.enableAutoHatch, (false, false) },
        { Settings.oxygenTankDrainMultiplier, (1f, 1f) },
        { Settings.fuelTankDrainMultiplier, (1f, 1f) },
        { Settings.componentTemperatureDamage, (false, false) },
        { Settings.atmosphereAngularDragMultiplier, (1f, 1f) },
        { Settings.spaceAngularDragMultiplier, (1f, 1f) },
        { Settings.disableRotationSpeedLimit, (false, false) },
        { Settings.gravityDirection, ("Down", "Down") },
        { Settings.disableScoutRecall, (false, false) },
        { Settings.disableScoutLaunching, (false, false) },
        { Settings.enableScoutLauncherComponent, (false, false) },
        { Settings.enableManualScoutRecall, (false, false) },
        { Settings.enableShipItemPlacement, (false, false) },
        { Settings.addPortableCampfire, (false, false) },
        { Settings.keepHelmetOn, (false, false) },
        { Settings.showWarningNotifications, (false, false) },
        { Settings.shipExplosionMultiplier, (1f, 1f) },
        { Settings.zeroGravityCockpitFreeLook, (false, false) },
        { Settings.shipBounciness, (1f, 1f) },
        { Settings.shipIgnitionCancelFix, (false, false) },
        { Settings.enablePersistentInput, (false, false) },
        { Settings.shipInputLatency, (1f, 1f) },
        { Settings.addEngineSwitch, (false, false) },
        { Settings.idleFuelConsumptionMultiplier, (1f, 1f) },
        { Settings.shipLightColor, ("Default", "Default") },
        { Settings.hotThrusters, (false, false) },
        { Settings.extraNoise, (false, false) },
        { Settings.interiorHullColor, ("Default", "Default") },
        { Settings.exteriorHullColor, ("Default", "Default") },
        { Settings.addTether, (false, false) },
    };

    public static string GetName(this Settings setting)
    {
        return setting.ToString();
    }

    public static object GetValue(this Settings setting)
    {
        JValue value = (JValue)settingValues[setting].Item1;
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

    public static void SetValue(this Settings setting, object value)
    {
        settingValues[setting] = (value, settingValues[setting].Item2);
    }

    public static object GetProperty(this Settings setting)
    {
        if (settingValues[setting].Item2 is JValue)
        {
            JValue value = (JValue)settingValues[setting].Item2;
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

        return settingValues[setting].Item2;
    }

    public static void SetProperty(this Settings setting, object value)
    {
        settingValues[setting] = (settingValues[setting].Item1, value);
    }

    public static Type GetType(this Settings setting)
    {
        return settingValues[setting].GetType();
    }
}
