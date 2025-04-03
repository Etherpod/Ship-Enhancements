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
        { Settings.enableGravityLandingGear, (false, false) },
        { Settings.disableAirAutoRoll, (false, false) },
        { Settings.disableWaterAutoRoll, (false, false) },
        { Settings.enableThrustModulator, (false, false) },
        { Settings.temperatureZonesAmount, ("", "") },
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
        { Settings.gravityDirection, ("", "") },
        { Settings.disableScoutRecall, (false, false) },
        { Settings.disableScoutLaunching, (false, false) },
        { Settings.enableScoutLauncherComponent, (false, false) },
        { Settings.enableManualScoutRecall, (false, false) },
        { Settings.enableShipItemPlacement, (false, false) },
        { Settings.addPortableCampfire, (false, false) },
        { Settings.keepHelmetOn, (false, false) },
        { Settings.showWarningNotifications, (false, false) },
        { Settings.shipExplosionMultiplier, (1f, 1f) },
        { Settings.shipBounciness, (1f, 1f) },
        { Settings.enableEnhancedAutopilot, (false, false) },
        { Settings.shipInputLatency, (1f, 1f) },
        { Settings.addEngineSwitch, (false, false) },
        { Settings.idleFuelConsumptionMultiplier, (1f, 1f) },
        { Settings.shipLightColor, ("", "") },
        { Settings.hotThrusters, (false, false) },
        { Settings.extraNoise, (false, false) },
        { Settings.interiorHullColor, ("", "") },
        { Settings.exteriorHullColor, ("", "") },
        { Settings.addTether, (false, false) },
        { Settings.disableDamageIndicators, (false, false) },
        { Settings.addShipSignal, (false, false) },
        { Settings.reactorLifetimeMultiplier, (1f, 1f) },
        { Settings.shipFriction, (1f, 1f) },
        { Settings.enableSignalscopeComponent, (false, false) },
        { Settings.rustLevel, (1f, 1f) },
        { Settings.dirtAccumulationTime, (1f, 1f) },
        { Settings.thrusterColor, ("", "") },
        { Settings.disableSeatbelt, (false, false) },
        { Settings.addPortableTractorBeam, (false, false) },
        { Settings.disableShipSuit, (false, false) },
        { Settings.damageIndicatorColor, ("", "") },
        { Settings.disableAutoLights, (false, false) },
        { Settings.addExpeditionFlag, (false, false) },
        { Settings.addFuelCanister, (false, false) },
        { Settings.cycloneChaos, (1f, 1f) },
        { Settings.moreExplosionDamage, (false, false) },
        { Settings.singleUseTractorBeam, (false, false) },
        { Settings.disableThrusters, (false, false) },
        { Settings.maxDirtAccumulation, (false, false) },
        { Settings.addShipWarpCore, (false, false) },
        { Settings.repairTimeMultiplier, (1f, 1f) },
        { Settings.airDragMultiplier, (1f, 1f) },
        { Settings.addShipClock, (false, false) },
        { Settings.enableStunDamage, (false, false) },
        { Settings.enableRepairConfirmation, (false, false) },
        { Settings.shipGravityFix, (false, false) },
        { Settings.enableRemovableGravityCrystal, (false, false) },
        { Settings.randomHullDamage, (1f, 1f) },
        { Settings.randomComponentDamage, (1f, 1f) },
        { Settings.enableFragileShip, (false, false) },
        { Settings.faultyHeatRegulators, (false, false) },
        { Settings.addErnesto, (false, false) },
        { Settings.repairLimit, (1f, 1f) },
        { Settings.extraEjectButtons, (false, false) },
        { Settings.preventSystemFailure, (false, false) },
        { Settings.addShipCurtain, (false, false) },
        { Settings.addRepairWrench, (false, false) },
        { Settings.funnySounds, (false, false) },
        { Settings.alwaysAllowLockOn, (false, false) },
        { Settings.shipWarpCoreComponent, (false, false) },
        { Settings.disableShipMedkit, (false, false) },
        { Settings.addRadio, (false, false) },
        { Settings.disableFluidPrevention, (false, false) },
        { Settings.disableHazardPrevention, (false, false) },
        { Settings.prolongDigestion, (false, false) },
        { Settings.unlimitedItems, (false, false) },
        { Settings.noiseMultiplier, (1f, 1f) },
        { Settings.waterDamage, (1f, 1f) },
        { Settings.sandDamage, (1f, 1f) },
        { Settings.disableMinimapMarkers, (1f, 1f) },
        { Settings.scoutPhotoMode, (false, false) },
        { Settings.fixShipThrustIndicator, (false, false) },
        { Settings.enableAutoAlign, (false, false) },
        { Settings.shipHornType, ("", "") },
    };

    public static string GetName(this Settings setting)
    {
        return setting.ToString();
    }

    public static object GetValue(this Settings setting)
    {
        if (Instance.ModHelper.Config.GetSettingsValue<string>("preset") == "Random")
        {
            return GetProperty(setting);
        }

        JValue value = (JValue)settingValues[setting].Item1;
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
