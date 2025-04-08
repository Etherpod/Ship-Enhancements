using OWML.Common;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ShipEnhancements;

public static class SettingsPresets
{
    public static readonly Dictionary<string, object> VanillaPlusSettings = new Dictionary<string, object>()
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
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", false },
        { "disableWaterAutoRoll", false },
        { "enableThrustModulator", false },
        { "temperatureZonesAmount", "None" },
        { "hullTemperatureDamage", false },
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
        { "oxygenTankDrainMultiplier", 1f },
        { "fuelTankDrainMultiplier", 1f },
        { "componentTemperatureDamage", false },
        { "atmosphereAngularDragMultiplier", 1f },
        { "spaceAngularDragMultiplier", 1f },
        { "disableRotationSpeedLimit", false },
        { "gravityDirection", "Down" },
        { "disableScoutRecall", false },
        { "disableScoutLaunching", false },
        { "enableScoutLauncherComponent", false },
        { "enableManualScoutRecall", false },
        { "enableShipItemPlacement", false },
        { "addPortableCampfire", false },
        { "keepHelmetOn", true },
        { "showWarningNotifications", true },
        { "shipExplosionMultiplier", 1f },
        { "shipBounciness", 0f },
        { "enableEnhancedAutopilot", false },
        { "shipInputLatency", 0f },
        { "addEngineSwitch", false },
        { "idleFuelConsumptionMultiplier", 0f },
        { "shipLightColor", "Default" },
        { "hotThrusters", false },
        { "extraNoise", false },
        { "interiorHullColor", "Default" },
        { "exteriorHullColor", "Default" },
        { "addTether", false },
        { "disableDamageIndicators", false },
        { "addShipSignal", false },
        { "reactorLifetimeMultiplier", 1f },
        { "shipFriction", 0.5f },
        { "enableSignalscopeComponent", false },
        { "rustLevel", 0f },
        { "dirtAccumulationTime", 0f },
        { "thrusterColor", "Default" },
        { "disableSeatbelt", false },
        { "addPortableTractorBeam", false },
        { "disableShipSuit", false },
        { "damageIndicatorColor", "Default" },
        { "disableAutoLights", false },
        { "addExpeditionFlag", false },
        { "addFuelCanister", false },
        { "cycloneChaos", 0f },
        { "moreExplosionDamage", false },
        { "singleUseTractorBeam", false },
        { "disableThrusters", "None" },
        { "maxDirtAccumulation", 0.8f },
        { "addShipWarpCore", false },
        { "repairTimeMultiplier", 1f },
        { "airDragMultiplier", 1f },
        { "addShipClock", false },
        { "enableStunDamage", false },
        { "enableRepairConfirmation", false },
        { "shipGravityFix", true },
        { "enableRemovableGravityCrystal", false },
        { "randomHullDamage", 0f },
        { "randomComponentDamage", 0f },
        { "enableFragileShip", false },
        { "faultyHeatRegulators", false },
        { "addErnesto", false },
        { "repairLimit", -1f },
        { "extraEjectButtons", false },
        { "preventSystemFailure", false },
        { "addShipCurtain", false },
        { "addRepairWrench", false },
        { "funnySounds", false },
        { "alwaysAllowLockOn", true },
        { "shipWarpCoreComponent", false },
        { "disableShipMedkit", false },
        { "addRadio", false },
        { "disableFluidPrevention", false },
        { "disableHazardPrevention", false },
        { "prolongDigestion", false },
        { "unlimitedItems", false },
        { "noiseMultiplier", 1f },
        { "waterDamage", 0f },
        { "sandDamage", 0f },
        { "disableMinimapMarkers", false },
        { "scoutPhotoMode", false },
        { "fixShipThrustIndicator", true },
        { "enableAutoAlign", false },
        { "shipHornType", "None" },
        { "disableHatch", false },
    };

    public static readonly Dictionary<string, object> MinimalSettings = new Dictionary<string, object>()
    {
        { "disableGravityCrystal", true },
        { "disableEjectButton", true },
        { "disableHeadlights", true },
        { "disableLandingCamera", true },
        { "disableShipLights", true },
        { "disableShipOxygen", true },
        { "oxygenDrainMultiplier", 1f },
        { "fuelDrainMultiplier", 1f },
        { "shipDamageMultiplier", 1f },
        { "shipDamageSpeedMultiplier", 1f },
        { "shipOxygenRefill", false },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", true },
        { "disableWaterAutoRoll", true },
        { "enableThrustModulator", false },
        { "temperatureZonesAmount", "None" },
        { "hullTemperatureDamage", false },
        { "enableShipFuelTransfer", false },
        { "enableJetpackRefuelDrain", false },
        { "disableReferenceFrame", true },
        { "disableMapMarkers", true },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 1f },
        { "oxygenRefillMultiplier", 1f },
        { "temperatureDamageMultiplier", 1f },
        { "temperatureResistanceMultiplier", 1f },
        { "enableAutoHatch", false },
        { "oxygenTankDrainMultiplier", 1f },
        { "fuelTankDrainMultiplier", 1f },
        { "componentTemperatureDamage", false },
        { "atmosphereAngularDragMultiplier", 1f },
        { "spaceAngularDragMultiplier", 1f },
        { "disableRotationSpeedLimit", false },
        { "gravityDirection", "Down" },
        { "disableScoutRecall", true },
        { "disableScoutLaunching", true },
        { "enableScoutLauncherComponent", false },
        { "enableManualScoutRecall", true },
        { "enableShipItemPlacement", false },
        { "addPortableCampfire", false },
        { "keepHelmetOn", true },
        { "showWarningNotifications", true },
        { "shipExplosionMultiplier", 1f },
        { "shipBounciness", 0f },
        { "enableEnhancedAutopilot", false },
        { "shipInputLatency", 0f },
        { "addEngineSwitch", false },
        { "idleFuelConsumptionMultiplier", 0f },
        { "shipLightColor", "Default" },
        { "hotThrusters", false },
        { "extraNoise", false },
        { "interiorHullColor", "Default" },
        { "exteriorHullColor", "Default" },
        { "addTether", false },
        { "disableDamageIndicators", true },
        { "addShipSignal", false },
        { "reactorLifetimeMultiplier", 1f },
        { "shipFriction", 0.5f },
        { "enableSignalscopeComponent", false },
        { "rustLevel", 0f },
        { "dirtAccumulationTime", 0f },
        { "thrusterColor", "Default" },
        { "disableSeatbelt", true },
        { "addPortableTractorBeam", false },
        { "disableShipSuit", false },
        { "damageIndicatorColor", "Default" },
        { "disableAutoLights", false },
        { "addExpeditionFlag", false },
        { "addFuelCanister", false },
        { "cycloneChaos", 0f },
        { "moreExplosionDamage", false },
        { "singleUseTractorBeam", true },
        { "disableThrusters", "Backward" },
        { "maxDirtAccumulation", 0.8f },
        { "addShipWarpCore", false },
        { "repairTimeMultiplier", 1f },
        { "airDragMultiplier", 1f },
        { "addShipClock", false },
        { "enableStunDamage", false },
        { "enableRepairConfirmation", true },
        { "shipGravityFix", true },
        { "enableRemovableGravityCrystal", false },
        { "randomHullDamage", 0f },
        { "randomComponentDamage", 0f },
        { "enableFragileShip", true },
        { "faultyHeatRegulators", false },
        { "addErnesto", false },
        { "repairLimit", 12f },
        { "extraEjectButtons", false },
        { "preventSystemFailure", false },
        { "addShipCurtain", false },
        { "addRepairWrench", true },
        { "funnySounds", false },
        { "alwaysAllowLockOn", true },
        { "shipWarpCoreComponent", false },
        { "disableShipMedkit", true },
        { "addRadio", false },
        { "disableFluidPrevention", true },
        { "disableHazardPrevention", false },
        { "prolongDigestion", false },
        { "unlimitedItems", false },
        { "noiseMultiplier", 1f },
        { "waterDamage", 0f },
        { "sandDamage", 0f },
        { "disableMinimapMarkers", true },
        { "scoutPhotoMode", false },
        { "fixShipThrustIndicator", true },
        { "enableAutoAlign", false },
        { "shipHornType", "None" },
        { "disableHatch", true },
    };

    public static readonly Dictionary<string, object> ImpossibleSettings = new Dictionary<string, object>()
    {
        { "disableGravityCrystal", false },
        { "disableEjectButton", false },
        { "disableHeadlights", false },
        { "disableLandingCamera", false },
        { "disableShipLights", false },
        { "disableShipOxygen", false },
        { "oxygenDrainMultiplier", 200f },
        { "fuelDrainMultiplier", 8f },
        { "shipDamageMultiplier", 5f },
        { "shipDamageSpeedMultiplier", 0.3f },
        { "shipOxygenRefill", false },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", false },
        { "disableWaterAutoRoll", false },
        { "enableThrustModulator", false },
        { "temperatureZonesAmount", "None" },
        { "hullTemperatureDamage", false },
        { "enableShipFuelTransfer", false },
        { "enableJetpackRefuelDrain", false },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 1.5f },
        { "fuelTransferMultiplier", 1f },
        { "oxygenRefillMultiplier", 1f },
        { "temperatureDamageMultiplier", 1f },
        { "temperatureResistanceMultiplier", 1f },
        { "enableAutoHatch", false },
        { "oxygenTankDrainMultiplier", 100f },
        { "fuelTankDrainMultiplier", 100f },
        { "componentTemperatureDamage", false },
        { "atmosphereAngularDragMultiplier", 0.5f },
        { "spaceAngularDragMultiplier", 0f },
        { "disableRotationSpeedLimit", false },
        { "gravityDirection", "Down" },
        { "disableScoutRecall", false },
        { "disableScoutLaunching", false },
        { "enableScoutLauncherComponent", false },
        { "enableManualScoutRecall", false },
        { "enableShipItemPlacement", false },
        { "addPortableCampfire", false },
        { "keepHelmetOn", true },
        { "showWarningNotifications", true },
        { "shipExplosionMultiplier", 100f },
        { "shipBounciness", 1f },
        { "enableEnhancedAutopilot", false },
        { "shipInputLatency", 0f },
        { "addEngineSwitch", false },
        { "idleFuelConsumptionMultiplier", 0f },
        { "shipLightColor", "Default" },
        { "hotThrusters", false },
        { "extraNoise", true },
        { "interiorHullColor", "Default" },
        { "exteriorHullColor", "Default" },
        { "addTether", false },
        { "disableDamageIndicators", false },
        { "addShipSignal", false },
        { "reactorLifetimeMultiplier", 0.5f },
        { "shipFriction", 0f },
        { "enableSignalscopeComponent", false },
        { "rustLevel", 0.4f },
        { "dirtAccumulationTime", 60f },
        { "thrusterColor", "Default" },
        { "disableSeatbelt", false },
        { "addPortableTractorBeam", false },
        { "disableShipSuit", false },
        { "damageIndicatorColor", "Default" },
        { "disableAutoLights", false },
        { "addExpeditionFlag", false },
        { "addFuelCanister", false },
        { "cycloneChaos", 1f },
        { "moreExplosionDamage", true },
        { "singleUseTractorBeam", false },
        { "disableThrusters", "None" },
        { "maxDirtAccumulation", 1f },
        { "addShipWarpCore", false },
        { "repairTimeMultiplier", 5f },
        { "airDragMultiplier", 0f },
        { "addShipClock", false },
        { "enableStunDamage", true },
        { "enableRepairConfirmation", false },
        { "shipGravityFix", true },
        { "enableRemovableGravityCrystal", false },
        { "randomHullDamage", 0f },
        { "randomComponentDamage", 0f },
        { "enableFragileShip", false },
        { "faultyHeatRegulators", false },
        { "addErnesto", false },
        { "repairLimit", -1f },
        { "extraEjectButtons", false },
        { "preventSystemFailure", false },
        { "addShipCurtain", false },
        { "addRepairWrench", false },
        { "funnySounds", false },
        { "alwaysAllowLockOn", true },
        { "shipWarpCoreComponent", false },
        { "disableShipMedkit", false },
        { "addRadio", false },
        { "disableFluidPrevention", false },
        { "disableHazardPrevention", false },
        { "prolongDigestion", false },
        { "unlimitedItems", false },
        { "noiseMultiplier", 5f },
        { "waterDamage", 1f },
        { "sandDamage", 1f },
        { "disableMinimapMarkers", false },
        { "scoutPhotoMode", false },
        { "fixShipThrustIndicator", true },
        { "enableAutoAlign", false },
        { "shipHornType", "None" },
        { "disableHatch", false },
    };

    public static readonly Dictionary<string, object> NewStuffSettings = new Dictionary<string, object>()
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
        { "shipOxygenRefill", true },
        { "enableGravityLandingGear", true },
        { "disableAirAutoRoll", true },
        { "disableWaterAutoRoll", false },
        { "enableThrustModulator", true },
        { "temperatureZonesAmount", "All" },
        { "hullTemperatureDamage", true },
        { "enableShipFuelTransfer", true },
        { "enableJetpackRefuelDrain", true },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 1f },
        { "oxygenRefillMultiplier", 1f },
        { "temperatureDamageMultiplier", 1f },
        { "temperatureResistanceMultiplier", 1f },
        { "enableAutoHatch", false },
        { "oxygenTankDrainMultiplier", 1f },
        { "fuelTankDrainMultiplier", 1f },
        { "componentTemperatureDamage", true },
        { "atmosphereAngularDragMultiplier", 1f },
        { "spaceAngularDragMultiplier", 1f },
        { "disableRotationSpeedLimit", false },
        { "gravityDirection", "Down" },
        { "disableScoutRecall", false },
        { "disableScoutLaunching", false },
        { "enableScoutLauncherComponent", true },
        { "enableManualScoutRecall", false },
        { "enableShipItemPlacement", true },
        { "addPortableCampfire", true },
        { "keepHelmetOn", true },
        { "showWarningNotifications", true },
        { "shipExplosionMultiplier", 1f },
        { "shipBounciness", 0f },
        { "enableEnhancedAutopilot", true },
        { "shipInputLatency", 0f },
        { "addEngineSwitch", true },
        { "idleFuelConsumptionMultiplier", 0f },
        { "shipLightColor", "Default" },
        { "hotThrusters", false },
        { "extraNoise", false },
        { "interiorHullColor", "Default" },
        { "exteriorHullColor", "Default" },
        { "addTether", true },
        { "disableDamageIndicators", false },
        { "addShipSignal", true },
        { "reactorLifetimeMultiplier", 1f },
        { "shipFriction", 0.5f },
        { "enableSignalscopeComponent", true },
        { "rustLevel", 0f },
        { "dirtAccumulationTime", 0f },
        { "thrusterColor", "Default" },
        { "disableSeatbelt", false },
        { "addPortableTractorBeam", true },
        { "disableShipSuit", false },
        { "damageIndicatorColor", "Default" },
        { "disableAutoLights", true },
        { "addExpeditionFlag", true },
        { "addFuelCanister", true },
        { "cycloneChaos", 0f },
        { "moreExplosionDamage", false },
        { "singleUseTractorBeam", false },
        { "disableThrusters", "None" },
        { "maxDirtAccumulation", 0.8f },
        { "addShipWarpCore", true },
        { "repairTimeMultiplier", 1f },
        { "airDragMultiplier", 1f },
        { "addShipClock", true },
        { "enableStunDamage", false },
        { "enableRepairConfirmation", true },
        { "shipGravityFix", true },
        { "enableRemovableGravityCrystal", true },
        { "randomHullDamage", 0f },
        { "randomComponentDamage", 0f },
        { "enableFragileShip", false },
        { "faultyHeatRegulators", false },
        { "addErnesto", false },
        { "repairLimit", -1f },
        { "extraEjectButtons", true },
        { "preventSystemFailure", true },
        { "addShipCurtain", true },
        { "addRepairWrench", true },
        { "funnySounds", false },
        { "alwaysAllowLockOn", true },
        { "shipWarpCoreComponent", true },
        { "disableShipMedkit", false },
        { "addRadio", true },
        { "disableFluidPrevention", false },
        { "disableHazardPrevention", false },
        { "prolongDigestion", false },
        { "unlimitedItems", false },
        { "noiseMultiplier", 1f },
        { "waterDamage", 0f },
        { "sandDamage", 0f },
        { "disableMinimapMarkers", false },
        { "scoutPhotoMode", true },
        { "fixShipThrustIndicator", true },
        { "enableAutoAlign", true },
        { "shipHornType", "Default" },
        { "disableHatch", false },
    };

    public static readonly Dictionary<string, object> PandemoniumSettings = new Dictionary<string, object>()
    {
        { "disableGravityCrystal", false },
        { "disableEjectButton", true },
        { "disableHeadlights", false },
        { "disableLandingCamera", true },
        { "disableShipLights", false },
        { "disableShipOxygen", false },
        { "oxygenDrainMultiplier", 400f },
        { "fuelDrainMultiplier", 8f },
        { "shipDamageMultiplier", 2f },
        { "shipDamageSpeedMultiplier", 0.4f },
        { "shipOxygenRefill", true },
        { "enableGravityLandingGear", true },
        { "disableAirAutoRoll", true },
        { "disableWaterAutoRoll", true },
        { "enableThrustModulator", true },
        { "temperatureZonesAmount", "All" },
        { "hullTemperatureDamage", true },
        { "enableShipFuelTransfer", true },
        { "enableJetpackRefuelDrain", true },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 0.2f },
        { "fuelTransferMultiplier", 5f },
        { "oxygenRefillMultiplier", 0.4f },
        { "temperatureDamageMultiplier", 8f },
        { "temperatureResistanceMultiplier", 2f },
        { "enableAutoHatch", true },
        { "oxygenTankDrainMultiplier", 10f },
        { "fuelTankDrainMultiplier", 10f },
        { "componentTemperatureDamage", true },
        { "atmosphereAngularDragMultiplier", 1.5f },
        { "spaceAngularDragMultiplier", 0.5f },
        { "disableRotationSpeedLimit", false },
        { "gravityDirection", "Random" },
        { "disableScoutRecall", false },
        { "disableScoutLaunching", false },
        { "enableScoutLauncherComponent", true },
        { "enableManualScoutRecall", true },
        { "enableShipItemPlacement", true },
        { "addPortableCampfire", true },
        { "keepHelmetOn", true },
        { "showWarningNotifications", true },
        { "shipExplosionMultiplier", 25f },
        { "shipBounciness", 0.5f },
        { "enableEnhancedAutopilot", true },
        { "shipInputLatency", 0f },
        { "addEngineSwitch", true },
        { "idleFuelConsumptionMultiplier", 1f },
        { "shipLightColor", "Rainbow" },
        { "hotThrusters", true },
        { "extraNoise", true },
        { "interiorHullColor", "Rainbow" },
        { "exteriorHullColor", "Rainbow" },
        { "addTether", true },
        { "disableDamageIndicators", true },
        { "addShipSignal", true },
        { "reactorLifetimeMultiplier", 0.5f },
        { "shipFriction", 0f },
        { "enableSignalscopeComponent", true },
        { "rustLevel", 0.5f },
        { "dirtAccumulationTime", 350f },
        { "thrusterColor", "Default" },
        { "disableSeatbelt", true },
        { "addPortableTractorBeam", true },
        { "disableShipSuit", false },
        { "damageIndicatorColor", "Rainbow" },
        { "disableAutoLights", true },
        { "addExpeditionFlag", true },
        { "addFuelCanister", true },
        { "cycloneChaos", 1f },
        { "moreExplosionDamage", true },
        { "singleUseTractorBeam", true },
        { "disableThrusters", "None" },
        { "maxDirtAccumulation", 0.8f },
        { "addShipWarpCore", true },
        { "repairTimeMultiplier", 0.1f },
        { "airDragMultiplier", 0f },
        { "addShipClock", true },
        { "enableStunDamage", true },
        { "enableRepairConfirmation", true },
        { "shipGravityFix", true },
        { "enableRemovableGravityCrystal", true },
        { "randomHullDamage", 0.1f },
        { "randomComponentDamage", 0.1f },
        { "enableFragileShip", false },
        { "faultyHeatRegulators", true },
        { "addErnesto", true },
        { "repairLimit", -1f },
        { "extraEjectButtons", true },
        { "preventSystemFailure", true },
        { "addShipCurtain", true },
        { "addRepairWrench", true },
        { "funnySounds", false },
        { "alwaysAllowLockOn", true },
        { "shipWarpCoreComponent", true },
        { "disableShipMedkit", true },
        { "addRadio", true },
        { "disableFluidPrevention", true },
        { "disableHazardPrevention", true },
        { "prolongDigestion", true },
        { "unlimitedItems", false },
        { "noiseMultiplier", 1f },
        { "waterDamage", 0.5f },
        { "sandDamage", 0.5f },
        { "disableMinimapMarkers", false },
        { "scoutPhotoMode", true },
        { "fixShipThrustIndicator", true },
        { "enableAutoAlign", true },
        { "shipHornType", "Annoying" },
        { "disableHatch", false },
    };

    // Random preset composition
    // A. Random chance, return true
    // B. Random chance, return random value between min/max
    // C. Random chance, return weighted strings or weighted number ranges
    // Floats in tuples are either number ranges or select chance at min/max difficulty
    public static readonly Dictionary<string, RandomSettingValue> RandomSettings = new Dictionary<string, RandomSettingValue>()
    {
        { "disableGravityCrystal", new RandomSettingValue((0.2f, 0.5f)) },
        { "disableEjectButton", new RandomSettingValue((0.2f, 0.5f)) },
        { "disableHeadlights", new RandomSettingValue((0.2f, 0.5f)) },
        { "disableLandingCamera", new RandomSettingValue((0.2f, 0.5f)) },
        { "disableShipLights", new RandomSettingValue((0.2f, 0.5f)) },
        { "disableShipOxygen", new RandomSettingValue((0f, 0.08f)) },
        { "oxygenDrainMultiplier", new RandomSettingValue(
        [
            ((1f, 10f), (1f, 0.8f)),
            ((10f, 50f), (0.5f, 1f)),
            ((50f, 200f), (0.1f, 1.05f)),
            ((200f, 500f), (0f, 1.1f)),
            ((500f, 1000f), (0f, 1.1f)),
        ], (0.4f, 0.75f), 1f) },
        { "fuelDrainMultiplier", new RandomSettingValue(
        [
            ((0.5f, 1.5f), (1f, 1f)),
            ((1.5f, 4f), (0.5f, 1f)),
            ((4f, 12f), (0.1f, 1.05f)),
            ((12f, 20f), (0f, 1.1f)),
            ((20f, 50f), (0f, 0.8f)),
        ], (0.25f, 0.5f), 1f) },
        { "shipDamageMultiplier", new RandomSettingValue(
        [
            (0f, (0.3f, 0f)),
            ((0.2f, 0.7f), (0.5f, 0.1f)),
            ((0.7f, 1.2f), (1f, 1f)),
            ((1.2f, 2f), (0.2f, 1.1f)),
            ((2f, 10f), (0f, 1.2f)),
        ], (0.25f, 0.5f), 1f) },
        { "shipDamageSpeedMultiplier", new RandomSettingValue(
        [
            ((0.05f, 0.5f), (0f, 0.5f)),
            ((0.5f, 0.9f), (0f, 1f)),
            ((0.9f, 1.1f), (1f, 1f)),
            ((1.1f, 5f), (0.8f, 0.4f)),
        ], (0.25f, 0.5f), 1f)},
        { "shipOxygenRefill", new RandomSettingValue((0.8f, 0.8f)) },
        { "enableGravityLandingGear", new RandomSettingValue((0.5f, 0.5f)) },
        { "disableAirAutoRoll", new RandomSettingValue((0.2f, 0.5f)) },
        { "disableWaterAutoRoll", new RandomSettingValue((0.2f, 0.5f)) },
        { "enableThrustModulator", new RandomSettingValue((0.5f, 0.5f)) },
        { "temperatureZonesAmount", new RandomSettingValue(
        [
            ("Sun", (1f, 0.2f)),
            ("Hot", (0f, 0.25f)),
            ("Cold", (0.2f, 0.25f)),
            ("All", (0f, 1f)),
        ], (0.2f, 0.5f), "None") },
        { "hullTemperatureDamage", new RandomSettingValue((0.5f, 0.8f)) },
        { "enableShipFuelTransfer", new RandomSettingValue((0.8f, 0.8f)) },
        { "enableJetpackRefuelDrain", new RandomSettingValue((0.3f, 0.8f)) },
        { "disableReferenceFrame", new RandomSettingValue((0f, 0.5f)) },
        { "disableMapMarkers", new RandomSettingValue((0f, 0.5f)) },
        { "gravityMultiplier", new RandomSettingValue(
        [
            ((0f, 0.8f), (0f, 1.1f)),
            ((0.8f, 1.5f), (1f, 1f)),
            ((1.5f, 2f), (0f, 1.1f)),
        ], (0.1f, 0.5f), 1f) },
        { "fuelTransferMultiplier", new RandomSettingValue(
        [
            ((0.05f, 0.5f), (0f, 0.5f)),
            ((0.5f, 0.9f), (0f, 1f)),
            ((0.9f, 1.1f), (1f, 1f)),
            ((1.1f, 5f), (0.8f, 0.4f)),
        ], (0.1f, 0.5f), 1f) },
        { "oxygenRefillMultiplier", new RandomSettingValue(
        [
            ((0.1f, 0.3f), (0f, 0.8f)),
            ((0.3f, 0.8f), (0f, 1.2f)),
            ((0.8f, 1.5f), (1f, 1f)),
            ((1.5f, 5f), (0.8f, 0.4f)),
        ], (0.1f, 0.5f), 1f) },
        { "temperatureDamageMultiplier", new RandomSettingValue(
        [
            ((0.2f, 0.7f), (0.4f, 0.3f)),
            ((0.7f, 1.2f), (1f, 1f)),
            ((1.2f, 2f), (0.2f, 1.1f)),
            ((2f, 10f), (0f, 1.2f)),
        ], (0.4f, 0.8f), 1f) },
        { "temperatureResistanceMultiplier", new RandomSettingValue(
        [
            ((0.05f, 0.2f), (0f, 0.6f)),
            ((0.2f, 0.8f), (0f, 1f)),
            ((0.8f, 1.5f), (1f, 1f)),
            ((1.5f, 3f), (0.5f, 0.3f)),
        ], (0.4f, 0.8f), 1f) },
        { "enableAutoHatch", new RandomSettingValue((0.2f, 0.5f)) },
        { "oxygenTankDrainMultiplier", new RandomSettingValue(
        [
            ((0.5f, 1.2f), (1f, 1f)),
            ((1.2f, 2.5f), (0.2f, 1.1f)),
            ((2.5f, 4f), (0f, 1.1f)),
            ((4f, 10f), (0f, 1.2f)),
        ], (0.3f, 0.8f), 1f) },
        { "fuelTankDrainMultiplier", new RandomSettingValue(
        [
            ((0.05f, 0.5f), (0.6f, 0.1f)),
            ((0.5f, 1.2f), (1f, 1f)),
            ((1.2f, 2f), (0.2f, 1.1f)),
            ((2f, 5f), (0f, 1.2f)),
        ], (0.3f, 0.8f), 1f) },
        { "componentTemperatureDamage", new RandomSettingValue((0.2f, 0.8f)) },
        { "atmosphereAngularDragMultiplier", new RandomSettingValue(
        [
            (0f, (0f, 1.2f)),
            ((0.1f, 0.7f), (0f, 1f)),
            ((0.7f, 1.2f), (1f, 1f)),
            ((1.2f, 2f), (0.2f, 1.1f)),
            ((2f, 5f), (0f, 1.2f)),
        ], (0f, 0.5f), 1f) },
        { "spaceAngularDragMultiplier", new RandomSettingValue(
        [
            (0f, (0f, 1.2f)),
            ((0.1f, 0.7f), (0f, 1f)),
            ((0.7f, 1.2f), (1f, 1f)),
            ((1.2f, 2f), (0.2f, 1.1f)),
            ((2f, 5f), (0f, 1.2f)),
        ], (0f, 0.5f), 1f) },
        { "disableRotationSpeedLimit", new RandomSettingValue((0f, 0.2f)) },
        { "gravityDirection", new RandomSettingValue(
        [
            ("Up", (1f, 1f)),
            ("Left", (1f, 1f)),
            ("Right", (1f, 1f)),
            ("Forward", (1f, 1f)),
            ("Back", (1f, 1f)),
            ("Random", (0.5f, 1f))
        ], (0f, 0.1f), "Down") },
        { "disableScoutRecall", new RandomSettingValue((0f, 0.5f)) },
        { "disableScoutLaunching", new RandomSettingValue((0.1f, 0.5f)) },
        { "enableScoutLauncherComponent", new RandomSettingValue((0.2f, 0.5f)) },
        { "enableManualScoutRecall", new RandomSettingValue((0f, 0.4f)) },
        { "enableShipItemPlacement", new RandomSettingValue((0.5f, 0.5f)) },
        { "addPortableCampfire", new RandomSettingValue((0.5f, 0.5f)) },
        //{ "keepHelmetOn", new RandomSettingValue(1f, 1f) },
        //{ "showWarningNotifications", new RandomSettingValue(1f, 1f) },
        { "shipExplosionMultiplier", new RandomSettingValue(
        [
            ((0.2f, 1.5f), (1f, 0.2f)),
            ((1.5f, 3f), (0.1f, 1f)),
            ((3f, 20f), (0f, 1.1f)),
            ((20f, 50f), (0f, 0.5f)),
        ], (0.3f, 0.75f), 1f) },
        { "shipBounciness", new RandomSettingValue(
        [
            ((0f, 0.2f), (1f, 0.5f)),
            ((0.2f, 0.5f), (0.1f, 1f)),
            ((0.5f, 1f), (0f, 1.1f)),
            ((1f, 3f), (0f, 0.5f)),
        ], (0.01f, 0.1f), 0f) },
        { "enableEnhancedAutopilot", new RandomSettingValue((0.5f, 0.5f)) },
        { "shipInputLatency", new RandomSettingValue(
        [
            ((0f, 0.2f), (1f, 1f)),
            ((0.2f, 0.5f), (0.1f, 1.1f)),
            ((0.5f, 1f), (0f, 1.1f)),
            ((1f, 3f), (0f, 0.5f)),
            ((3f, 10f), (0f, 0.01f)),
        ], (0f, 0.1f), 0f) },
        { "addEngineSwitch", new RandomSettingValue((0.3f, 0.5f)) },
        { "idleFuelConsumptionMultiplier", new RandomSettingValue(
        [
            ((0f, 0.5f), (1f, 1f)),
            ((0.5f, 1.5f), (0.1f, 1f)),
            ((0.5f, 1f), (0f, 1.1f)),
            ((1f, 3f), (0f, 0.5f)),
        ], (0f, 0.5f), 0f) },
        { "shipLightColor", new RandomSettingValue(
        [
            ("Red", (1f, 1f)),
            ("Hearthian Orange", (1f, 1f)),
            ("Orange", (1f, 1f)),
            ("Yellow", (1f, 1f)),
            ("Green", (1f, 1f)),
            ("Ghostly Green", (1f, 1f)),
            ("Turquoise", (1f, 1f)),
            ("Blue", (1f, 1f)),
            ("Nomaian Blue", (1f, 1f)),
            ("Blacklight", (1f, 1f)),
            ("Purple", (1f, 1f)),
            ("Magenta", (1f, 1f)),
            ("White", (1f, 1f)),
            ("Divine", (0f, 0.5f)),
            ("Rainbow", (0f, 0.5f)),
        ], (0.1f, 0.25f), "Default") },
        { "hotThrusters", new RandomSettingValue((0.2f, 0.8f)) },
        { "extraNoise", new RandomSettingValue((0f, 0.8f)) },
        { "interiorHullColor", new RandomSettingValue(
        [
            ("Red", (1f, 1f)),
            ("Orange", (1f, 1f)),
            ("Golden", (1f, 1f)),
            ("Green", (1f, 1f)),
            ("Turquoise", (1f, 1f)),
            ("Blue", (1f, 1f)),
            ("Lavender", (1f, 1f)),
            ("Pink", (1f, 1f)),
            ("Gray", (1f, 1f)),
            ("Rainbow", (0f, 1f)),
        ], (0.1f, 0.25f), "Default") },
        { "exteriorHullColor", new RandomSettingValue(
        [
            ("Red", (1f, 1f)),
            ("Orange", (1f, 1f)),
            ("Golden", (1f, 1f)),
            ("Green", (1f, 1f)),
            ("Turquoise", (1f, 1f)),
            ("Blue", (1f, 1f)),
            ("Lavender", (1f, 1f)),
            ("Pink", (1f, 1f)),
            ("Gray", (1f, 1f)),
            ("Rainbow", (0f, 1f)),
        ], (0.1f, 0.25f), "Default") },
        { "addTether", new RandomSettingValue((0.5f, 0.5f)) },
        { "disableDamageIndicators", new RandomSettingValue((0f, 0.5f)) },
        { "addShipSignal", new RandomSettingValue((0.5f, 0.5f)) },
        { "reactorLifetimeMultiplier", new RandomSettingValue(
        [
            (0f, (0f, 0.2f)),
            ((0.1f, 0.8f), (0.1f, 1.1f)),
            ((0.8f, 1.5f), (1f, 1f)),
            ((1.5f, 4f), (0.5f, 0.5f)),
        ], (0.2f, 0.5f), 1f) },
        { "shipFriction", new RandomSettingValue(
        [
            ((0f, 0.3f), (0f, 1.1f)),
            ((0.3f, 0.7f), (1f, 1f)),
            ((0.7f, 1f), (0f, 1.1f)),
        ], (0.01f, 0.1f), 0.5f) },
        { "enableSignalscopeComponent", new RandomSettingValue((0.3f, 0.5f)) },
        { "rustLevel", new RandomSettingValue(
        [
            ((0.1f, 0.2f), (1f, 0.5f)),
            ((0.2f, 0.5f), (0.2f, 1f)),
            ((0.5f, 0.8f), (0f, 1f)),
            ((0.8f, 1f), (0f, 0.8f)),
        ], (0f, 0.25f), 0f) },
        { "dirtAccumulationTime", new RandomSettingValue(
        [
            ((30f, 60f), (0f, 0.5f)),
            ((60f, 120f), (0.1f, 0.8f)),
            ((120f, 240f), (0.5f, 1f)),
            ((240f, 480f), (1f, 0.5f)),
            ((480f, 600f), (1f, 0.2f)),
        ], (0f, 0.25f), 0f) },
        { "thrusterColor", new RandomSettingValue(
        [
            ("Default", (1f, 1f)),
            ("Red", (1f, 1f)),
            ("White-Orange", (1f, 1f)),
            ("Lime-Orange", (1f, 1f)),
            ("Lime", (1f, 1f)),
            ("Ghostly Green", (1f, 1f)),
            ("Turquoise", (1f, 1f)),
            ("Blue", (1f, 1f)),
            ("Purple", (1f, 1f)),
            ("Rose", (1f, 1f)),
            ("Pink", (1f, 1f)),
            ("Rainbow", (0f, 0.5f)),
        ], (0.1f, 0.25f), "Default") },
        { "disableSeatbelt", new RandomSettingValue((0f, 0.5f)) },
        { "addPortableTractorBeam", new RandomSettingValue((0.5f, 0.5f)) },
        { "disableShipSuit", new RandomSettingValue((0f, 0.02f)) },
        { "damageIndicatorColor", new RandomSettingValue(
        [
            ("Orange", (1f, 1f)),
            ("Yellow", (1f, 1f)),
            ("Green", (1f, 1f)),
            ("Outer Wilds Beta", (1f, 1f)),
            ("Ghostly Green", (1f, 1f)),
            ("Turquoise", (1f, 1f)),
            ("Blue", (1f, 1f)),
            ("Dark Blue", (1f, 1f)),
            ("Nomaian Blue", (1f, 1f)),
            ("Purple", (1f, 1f)),
            ("Lavender", (1f, 1f)),
            ("Pink", (1f, 1f)),
            ("Rainbow", (0f, 0.5f)),
        ], (0.1f, 0.25f), "Default") },
        { "disableAutoLights", new RandomSettingValue((0.5f, 0.5f)) },
        { "addExpeditionFlag", new RandomSettingValue((0.5f, 0.5f)) },
        { "addFuelCanister", new RandomSettingValue((0.5f, 0.5f)) },
        { "cycloneChaos", new RandomSettingValue(
        [
            ((0f, 0.5f), (1f, 1f)),
            ((0.5f, 1f), (0f, 1f)),
        ], (0.2f, 0.5f), 0f) },
        { "moreExplosionDamage", new RandomSettingValue((0.25f, 0.75f)) },
        { "singleUseTractorBeam", new RandomSettingValue((0f, 0.25f)) },
        { "disableThrusters", new RandomSettingValue(
        [
            ("Backward", (1f, 1f)),
            ("Left-Right", (1f, 1f)),
            ("Up-Down", (1f, 1f)),
            ("All Except Forward", (0.5f, 1f)),
        ], (0f, 0.25f), "None") },
        { "maxDirtAccumulation", new RandomSettingValue(
        [
            ((0.2f, 0.4f), (1f, 0.8f)),
            ((0.4f, 0.6f), (0.25f, 0.8f)),
            ((0.6f, 1f), (0f, 1f)),
        ], (0.5f, 0.5f), 0.75f) },
        { "addShipWarpCore", new RandomSettingValue((0.5f, 0.5f)) },
        { "repairTimeMultiplier", new RandomSettingValue(
        [
            ((0.2f, 0.7f), (0.4f, 0.4f)),
            ((0.7f, 1.2f), (1f, 1f)),
            ((1.2f, 2f), (0.2f, 1.1f)),
            ((2f, 10f), (0f, 1.2f)),
        ], (0.2f, 0.5f), 1f) },
        { "airDragMultiplier", new RandomSettingValue(
        [
            (0f, (0f, 1.2f)),
            ((0.1f, 0.7f), (0f, 1f)),
            ((0.7f, 1.2f), (1f, 1f)),
            ((1.2f, 2f), (0.2f, 1.1f)),
            ((2f, 5f), (0f, 1.2f)),
        ], (0.1f, 0.75f), 1f) },
        { "addShipClock", new RandomSettingValue((0.5f, 0.5f)) },
        { "enableStunDamage", new RandomSettingValue((0f, 0.5f)) },
        { "enableRepairConfirmation", new RandomSettingValue((0.8f, 0.5f)) },
        //{ "shipGravityFix", new RandomSettingValue(1f) },
        { "enableRemovableGravityCrystal", new RandomSettingValue((0.2f, 0.5f)) },
        { "randomHullDamage", new RandomSettingValue((0f, 1f), (0f, 0.2f), 0f) },
        { "randomComponentDamage", new RandomSettingValue((0f, 1f), (0f, 0.2f), 0f) },
        { "enableFragileShip", new RandomSettingValue((0f, 0.2f)) },
        { "faultyHeatRegulators", new RandomSettingValue((0f, 0.25f)) },
        { "addErnesto", new RandomSettingValue((0.05f, 0.05f)) },
        { "repairLimit", new RandomSettingValue(
        [
            (0f, (0f, 0.5f)),
            ((1f, 5f), (0f, 1f)),
            ((5f, 10f), (0f, 1.2f)),
            ((10f, 20f), (0.2f, 0.5f)),
        ], (0.1f, 0.5f), -1f) },
        { "extraEjectButtons", new RandomSettingValue((0.4f, 0.5f)) },
        { "preventSystemFailure", new RandomSettingValue((0.5f, 0.5f)) },
        { "addShipCurtain", new RandomSettingValue((0.2f, 0.2f)) },
        { "addRepairWrench", new RandomSettingValue((0.2f, 0.5f)) },
        { "funnySounds", new RandomSettingValue((0.05f, 0.05f)) },
        //{ "alwaysAllowLockOn", new RandomSettingValue((1f, 1f)) },
        { "shipWarpCoreComponent", new RandomSettingValue((0.5f, 0.5f)) },
        { "disableShipMedkit", new RandomSettingValue((0f, 0.2f)) },
        { "addRadio", new RandomSettingValue((0.5f, 0.5f)) },
        { "disableFluidPrevention", new RandomSettingValue((0f, 0.5f)) },
        { "disableHazardPrevention", new RandomSettingValue((0f, 0.5f)) },
        { "prolongDigestion", new RandomSettingValue((0.5f, 0.2f)) },
        { "unlimitedItems", new RandomSettingValue((0.05f, 0.05f)) },
        { "noiseMultiplier", new RandomSettingValue(
        [
            ((0f, 0.3f), (1f, 1f)),
            ((0.3f, 0.4f), (0.2f, 1f)),
            ((0.4f, 0.6f), (0f, 1.2f)),
            ((0.6f, 0.99f), (0f, 1.2f)),
            (1f, (0f, 1f)),
        ], (0f, 0.75f), 1f) },
        { "waterDamage", new RandomSettingValue(
        [
            ((0f, 0.3f), (1f, 1f)),
            ((0.3f, 0.4f), (0.2f, 1f)),
            ((0.4f, 0.6f), (0f, 1.2f)),
            ((0.6f, 0.99f), (0f, 1.2f)),
            (1f, (0f, 1f)),
        ], (0.1f, 0.5f), 0f) },
        { "sandDamage", new RandomSettingValue(
        [
            ((0f, 0.3f), (1f, 1f)),
            ((0.3f, 0.4f), (0.2f, 1f)),
            ((0.4f, 0.6f), (0f, 1.2f)),
            ((0.6f, 0.99f), (0f, 1.2f)),
            (1f, (0f, 1f)),
        ], (0.1f, 0.5f), 0f) },
        { "disableMinimapMarkers", new RandomSettingValue((0f, 0.5f)) },
        { "scoutPhotoMode", new RandomSettingValue((0.5f, 0.5f)) },
        //{ "fixShipThrustIndicator", new RandomSettingValue((1f, 1f)) },
        { "enableAutoAlign", new RandomSettingValue((0.5f, 0.5f)) },
        { "shipHornType", new RandomSettingValue(
        [
            ("Default", (1f, 1f)),
            ("Old", (1f, 1f)),
            ("Train", (1f, 1f)),
            ("Loud", (1f, 1f)),
            ("Short", (1f, 1f)),
            ("Clown", (1f, 1f)),
            ("Annoying", (1f, 1f)),
        ], (0.25f, 0.5f), "None") },
        { "disableHatch", new RandomSettingValue((0.05f, 0.5f)) },
    };

    public static Dictionary<PresetName, Dictionary<string, object>> presetDicts { get; private set; }

    public static Dictionary<string, Dictionary<PresetName, object>> settingsPresets { get; private set; }

    private static bool _initialized = false;

    public enum PresetName
    {
        VanillaPlus = 1,
        Minimal = 2,
        Impossible = 4,
        NewStuff = 8,
        Pandemonium = 16,
        Random = 32,
        Custom = 0
    }

    public static void InitializePresets()
    {
        presetDicts = new()
        {
            { PresetName.VanillaPlus, VanillaPlusSettings },
            { PresetName.Minimal, MinimalSettings },
            { PresetName.Impossible, ImpossibleSettings },
            { PresetName.NewStuff, NewStuffSettings },
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
        if (preset == PresetName.Custom || preset == PresetName.Random) return;
        foreach (KeyValuePair<string, object> setting in presetDicts[preset])
        {
            if (setting.Value is float)
            {
                config.SetSettingsValue(setting.Key, Math.Round((float)setting.Value * 1000f) / 1000f);
            }
            else
            {
                config.SetSettingsValue(setting.Key, setting.Value);
            }
        }
    }

    public static PresetName GetPresetFromConfig(string configPreset)
    {
        configPreset = configPreset.Replace(" ", "");
        if (Enum.TryParse(configPreset, out PresetName preset))
        {
            return preset;
        }
        ShipEnhancements.WriteDebugMessage($"Failed to convert {configPreset} to preset.", error: true);
        return PresetName.Custom;
    }

    public static string GetName(this PresetName preset)
    {
        if (preset == PresetName.VanillaPlus)
        {
            return "Vanilla Plus";
        }
        if (preset == PresetName.NewStuff)
        {
            return "New Stuff";
        }

        return preset.ToString();
    }

    public static object GetPresetSetting(this PresetName preset, string setting)
    {
        if (preset == PresetName.Random)
        {
            return RandomSettings[setting].GetRandomValue();
        }

        if (settingsPresets.ContainsKey(setting))
        {
            return settingsPresets[setting][preset];
        }
        else
        {
            return null;
        }
    }

    public static bool Initialized()
    {
        return _initialized;
    }
}
