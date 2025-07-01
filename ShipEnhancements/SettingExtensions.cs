using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public static class SettingExtensions
{
    private static Dictionary<Settings, (object value, object property)> settingValues = new Dictionary<Settings, (object, object)>()
    {
        { disableGravityCrystal, (false, false) },
        { disableEjectButton, (false, false) },
        { disableHeadlights, (false, false) },
        { disableLandingCamera, (false, false) },
        { disableShipLights, (false, false) },
        { disableShipOxygen, (false, false) },
        { oxygenDrainMultiplier, (1f, 1f) },
        { fuelDrainMultiplier, (1f, 1f) },
        { shipDamageMultiplier, (1f, 1f) },
        { shipDamageSpeedMultiplier, (1f, 1f) },
        { shipOxygenRefill, (false, false) },
        { enableGravityLandingGear, (false, false) },
        { disableAirAutoRoll, (false, false) },
        { disableWaterAutoRoll, (false, false) },
        { enableThrustModulator, (false, false) },
        { temperatureZonesAmount, ("", "") },
        { hullTemperatureDamage, (false, false) },
        { enableShipFuelTransfer, (false, false) },
        { enableJetpackRefuelDrain, (false, false) },
        { disableReferenceFrame, (false, false) },
        { disableMapMarkers, (false, false) },
        { gravityMultiplier, (1f, 1f) },
        { fuelTransferMultiplier, (1f, 1f) },
        { oxygenRefillMultiplier, (1f, 1f) },
        { temperatureDamageMultiplier, (1f, 1f) },
        { temperatureResistanceMultiplier, (1f, 1f) },
        { enableAutoHatch, (false, false) },
        { oxygenTankDrainMultiplier, (1f, 1f) },
        { fuelTankDrainMultiplier, (1f, 1f) },
        { componentTemperatureDamage, (false, false) },
        { atmosphereAngularDragMultiplier, (1f, 1f) },
        { spaceAngularDragMultiplier, (1f, 1f) },
        { disableRotationSpeedLimit, (false, false) },
        { gravityDirection, ("", "") },
        { disableScoutRecall, (false, false) },
        { disableScoutLaunching, (false, false) },
        { enableScoutLauncherComponent, (false, false) },
        { enableManualScoutRecall, (false, false) },
        { enableShipItemPlacement, (false, false) },
        { addPortableCampfire, (false, false) },
        { keepHelmetOn, (false, false) },
        { showWarningNotifications, (false, false) },
        { shipExplosionMultiplier, (1f, 1f) },
        { shipBounciness, (1f, 1f) },
        { enableEnhancedAutopilot, (false, false) },
        { shipInputLatency, (1f, 1f) },
        { addEngineSwitch, (false, false) },
        { idleFuelConsumptionMultiplier, (1f, 1f) },
        { shipLightColorOptions, ("", "") },
        { shipLightColor1, ("", "") },
        { shipLightColor2, ("", "") },
        { shipLightColor3, ("", "") },
        { shipLightColorBlend, ("", "") },
        { hotThrusters, (false, false) },
        { extraNoise, (false, false) },
        { interiorHullColorOptions, ("", "") },
        { interiorHullColor1, ("", "") },
        { interiorHullColor2, ("", "") },
        { interiorHullColor3, ("", "") },
        { interiorHullColorBlend, ("", "") },
        { exteriorHullColorOptions, ("", "") },
        { exteriorHullColor1, ("", "") },
        { exteriorHullColor2, ("", "") },
        { exteriorHullColor3, ("", "") },
        { exteriorHullColorBlend, ("", "") },
        { addTether, (false, false) },
        { disableDamageIndicators, (false, false) },
        { addShipSignal, (false, false) },
        { reactorLifetimeMultiplier, (1f, 1f) },
        { shipFriction, (1f, 1f) },
        { enableSignalscopeComponent, (false, false) },
        { rustLevel, (1f, 1f) },
        { dirtAccumulationTime, (1f, 1f) },
        { thrusterColorOptions, ("", "") },
        { thrusterColor1, ("", "") },
        { thrusterColor2, ("", "") },
        { thrusterColor3, ("", "") },
        { thrusterColorBlend, ("", "") },
        { disableSeatbelt, (false, false) },
        { addPortableTractorBeam, (false, false) },
        { disableShipSuit, (false, false) },
        { indicatorColorOptions, ("", "") },
        { indicatorColor1, ("", "") },
        { indicatorColor2, ("", "") },
        { indicatorColor3, ("", "") },
        { indicatorColorBlend, ("", "") },
        { disableAutoLights, (false, false) },
        { addExpeditionFlag, (false, false) },
        { addFuelCanister, (false, false) },
        { cycloneChaos, (1f, 1f) },
        { moreExplosionDamage, (false, false) },
        { singleUseTractorBeam, (false, false) },
        { disableThrusters, (false, false) },
        { maxDirtAccumulation, (false, false) },
        { shipWarpCoreType, ("", "") },
        { repairTimeMultiplier, (1f, 1f) },
        { airDragMultiplier, (1f, 1f) },
        { addShipClock, (false, false) },
        { enableStunDamage, (false, false) },
        { enableRepairConfirmation, (false, false) },
        { shipGravityFix, (false, false) },
        { enableRemovableGravityCrystal, (false, false) },
        { randomHullDamage, (1f, 1f) },
        { randomComponentDamage, (1f, 1f) },
        { enableFragileShip, (false, false) },
        { faultyHeatRegulators, (false, false) },
        { addErnesto, (false, false) },
        { repairLimit, (1f, 1f) },
        { extraEjectButtons, (false, false) },
        { preventSystemFailure, (false, false) },
        { addShipCurtain, (false, false) },
        { addRepairWrench, (false, false) },
        { funnySounds, (false, false) },
        { alwaysAllowLockOn, (false, false) },
        { disableShipMedkit, (false, false) },
        { addRadio, (false, false) },
        { disableFluidPrevention, (false, false) },
        { disableHazardPrevention, (false, false) },
        { prolongDigestion, (false, false) },
        { unlimitedItems, (false, false) },
        { noiseMultiplier, (1f, 1f) },
        { waterDamage, (1f, 1f) },
        { sandDamage, (1f, 1f) },
        { disableMinimapMarkers, (1f, 1f) },
        { scoutPhotoMode, (false, false) },
        { fixShipThrustIndicator, (false, false) },
        { enableAutoAlign, (false, false) },
        { shipHornType, ("", "") },
        { randomIterations, (1f, 1f) },
        { randomDifficulty, (1f, 1f) },
        { disableHatch, (false, false) },
        { splitLockOn, (false, false) },
        { enableColorBlending, (false, false) },
        { enableShipTemperature, (false, false) },
    };

    private static Dictionary<Settings, object> savedCustomSettings = new(settingValues.Count);

    public static Dictionary<string, string> customObjLabels = new();

    public static string GetName(this Settings setting)
    {
        return setting.ToString();
    }

    public static object GetValue(this Settings setting)
    {
        JValue value = (JValue)settingValues[setting].value;
        if (value.Type == JTokenType.Boolean)
        {
            return Convert.ToBoolean(value);
        }
        else if (value.Type == JTokenType.Float)
        {
            return float.Parse(value.ToString());
        }
        else if (value.Type == JTokenType.Integer)
        {
            return (float)int.Parse(value.ToString());
        }
        else if (value.Type == JTokenType.String)
        {
            return value.ToString();
        }
        return value;
    }

    public static void SetValue(this Settings setting, object value)
    {
        settingValues[setting] = (value, settingValues[setting].property);
    }

    public static object GetProperty(this Settings setting)
    {
        if (settingValues[setting].property is JValue)
        {
            JValue value = (JValue)settingValues[setting].property;
            if (value.Type == JTokenType.Boolean)
            {
                return Convert.ToBoolean(value);
            }
            else if (value.Type == JTokenType.Float)
            {
                return float.Parse(value.ToString());
            }
            else if (value.Type == JTokenType.Integer)
            {
                return (float)int.Parse(value.ToString());
            }
            else if (value.Type == JTokenType.String)
            {
                return value.ToString();
            }
            return value;
        }

        return settingValues[setting].property;
    }

    public static void SetProperty(this Settings setting, object value)
    {
        settingValues[setting] = (settingValues[setting].value, value);
    }

    public static object ConvertJValue(object obj)
    {
        if (obj is not JValue) return null;

        JValue value = (JValue)obj;
        if (value.Type == JTokenType.Boolean)
        {
            return Convert.ToBoolean(value);
        }
        else if (value.Type == JTokenType.Float)
        {
            return float.Parse(value.ToString());
        }
        else if (value.Type == JTokenType.Integer)
        {
            return (float)int.Parse(value.ToString());
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

    public static void SaveCustomSettings()
    {
        foreach (var (setting, value) in settingValues)
        {
            if (SettingsPresets.VanillaPlusSettings.ContainsKey(setting.GetName()))
            {
                savedCustomSettings[setting] = value.value;
            }
        }
    }

    public static void LoadCustomSettings()
    {
        foreach (var (setting, value) in savedCustomSettings)
        {
            if (SettingsPresets.VanillaPlusSettings.ContainsKey(setting.GetName()))
            {
                settingValues[setting] = (value, settingValues[setting].property);
            }
        }
    }

    public static void ResetCustomSettings()
    {
        foreach (var (setting, value) in settingValues)
        {
            savedCustomSettings[setting] = Instance.ModHelper.DefaultConfig.GetSettingsValue<object>(setting.GetName());
            Instance.ModHelper.Config.SetSettingsValue(setting.GetName(), savedCustomSettings[setting]);
        }
    }

    public static T AsEnum<T>(this string enumName) where T : struct =>
    Enum.TryParse<T>(enumName, out var result) ? result 
        : throw new ArgumentException($"Enum '{enumName}' does not exist.");

    public static bool IsEnum<T>(this string enumName) where T : struct =>
    Enum.TryParse<T>(enumName, out var result);

    public static ColorHSV AsHSV(this Color color)
    {
        Color.RGBToHSV(color, out float H, out float S, out float V);
        return new ColorHSV(H, S, V);
    }

    public static Color AsRGB(this ColorHSV color)
    {
        return Color.HSVToRGB(color.h, color.s, color.v);
    }
}
