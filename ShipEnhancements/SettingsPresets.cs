﻿using OWML.Common;
using System.Collections.Generic;
using System;

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
        { "disableShipRepair", false },
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
        { "enablePersistentInput", false },
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
        { "disableShipFriction", false },
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
        { "chaoticCyclones", false },
        { "moreExplosionDamage", false },
        { "singleUseTractorBeam", false },
        { "disableRetroRockets", false },
        { "maxDirtAccumulation", 0.75f },
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
        { "fuelDrainMultiplier", 1.5f },
        { "shipDamageMultiplier", 0.9f },
        { "shipDamageSpeedMultiplier", 0.8f },
        { "shipOxygenRefill", false },
        { "disableShipRepair", false },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", false },
        { "disableWaterAutoRoll", true },
        { "enableThrustModulator", false },
        { "temperatureZonesAmount", "None" },
        { "hullTemperatureDamage", false },
        { "enableShipFuelTransfer", false },
        { "enableJetpackRefuelDrain", true },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 3f },
        { "oxygenRefillMultiplier", 1f },
        { "temperatureDamageMultiplier", 1f },
        { "temperatureResistanceMultiplier", 1f },
        { "enableAutoHatch", false },
        { "oxygenTankDrainMultiplier", 1f },
        { "fuelTankDrainMultiplier", 3f },
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
        { "showWarningNotifications", false },
        { "shipExplosionMultiplier", 0f },
        { "shipBounciness", 0f },
        { "enablePersistentInput", false },
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
        { "reactorLifetimeMultiplier", 3f },
        { "disableShipFriction", false },
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
        { "chaoticCyclones", false },
        { "moreExplosionDamage", false },
        { "singleUseTractorBeam", true },
        { "disableRetroRockets", true },
        { "maxDirtAccumulation", 0.75f },
        { "addShipWarpCore", false },
        { "repairTimeMultiplier", 1f },
        { "airDragMultiplier", 0f },
        { "addShipClock", false },
        { "enableStunDamage", false },
        { "enableRepairConfirmation", true },
        { "shipGravityFix", true },
        { "enableRemovableGravityCrystal", false },
        { "randomHullDamage", 0f },
        { "randomComponentDamage", 0f },
        { "enableFragileShip", true },
        { "faultyHeatRegulators", false },
    };

    public static readonly Dictionary<string, object> RelaxedSettings = new Dictionary<string, object>()
    {
        { "disableGravityCrystal", false },
        { "disableEjectButton", false },
        { "disableHeadlights", false },
        { "disableLandingCamera", false },
        { "disableShipLights", false },
        { "disableShipOxygen", false },
        { "oxygenDrainMultiplier", 1f },
        { "fuelDrainMultiplier", 1f },
        { "shipDamageMultiplier", 0.4f },
        { "shipDamageSpeedMultiplier", 2f },
        { "shipOxygenRefill", false },
        { "disableShipRepair", false },
        { "enableGravityLandingGear", true },
        { "disableAirAutoRoll", true },
        { "disableWaterAutoRoll", false },
        { "enableThrustModulator", true },
        { "temperatureZonesAmount", "None" },
        { "hullTemperatureDamage", false },
        { "enableShipFuelTransfer", true },
        { "enableJetpackRefuelDrain", false },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 2f },
        { "oxygenRefillMultiplier", 3f },
        { "temperatureDamageMultiplier", 1f },
        { "temperatureResistanceMultiplier", 1f },
        { "enableAutoHatch", false },
        { "oxygenTankDrainMultiplier", 0.5f },
        { "fuelTankDrainMultiplier", 0.5f },
        { "componentTemperatureDamage", false },
        { "atmosphereAngularDragMultiplier", 1f },
        { "spaceAngularDragMultiplier", 1f },
        { "disableRotationSpeedLimit", false },
        { "gravityDirection", "Down" },
        { "disableScoutRecall", false },
        { "disableScoutLaunching", false },
        { "enableScoutLauncherComponent", false },
        { "enableManualScoutRecall", false },
        { "enableShipItemPlacement", true },
        { "addPortableCampfire", true },
        { "keepHelmetOn", true },
        { "showWarningNotifications", true },
        { "shipExplosionMultiplier", 1f },
        { "shipBounciness", 0f },
        { "enablePersistentInput", true },
        { "shipInputLatency", 0f },
        { "addEngineSwitch", false },
        { "idleFuelConsumptionMultiplier", 0f },
        { "shipLightColor", "Default" },
        { "hotThrusters", false },
        { "extraNoise", false },
        { "interiorHullColor", "Default" },
        { "exteriorHullColor", "Default" },
        { "addTether", true },
        { "disableDamageIndicators", false },
        { "addShipSignal", true },
        { "reactorLifetimeMultiplier", 3f },
        { "disableShipFriction", false },
        { "enableSignalscopeComponent", false },
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
        { "chaoticCyclones", false },
        { "moreExplosionDamage", false },
        { "singleUseTractorBeam", false },
        { "disableRetroRockets", false },
        { "maxDirtAccumulation", 0.75f },
        { "addShipWarpCore", true },
        { "repairTimeMultiplier", 0.5f },
        { "airDragMultiplier", 1f },
        { "addShipClock", true },
        { "enableStunDamage", false },
        { "enableRepairConfirmation", true },
        { "shipGravityFix", true },
        { "enableRemovableGravityCrystal", false },
        { "randomHullDamage", 0f },
        { "randomComponentDamage", 0f },
        { "enableFragileShip", false },
        { "faultyHeatRegulators", false },
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
        { "fuelDrainMultiplier", 3f },
        { "shipDamageMultiplier", 2.5f },
        { "shipDamageSpeedMultiplier", 0.75f },
        { "shipOxygenRefill", true },
        { "disableShipRepair", false },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", true },
        { "disableWaterAutoRoll", true },
        { "enableThrustModulator", false },
        { "temperatureZonesAmount", "All" },
        { "hullTemperatureDamage", true },
        { "enableShipFuelTransfer", true },
        { "enableJetpackRefuelDrain", true },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 0.8f },
        { "oxygenRefillMultiplier", 0.2f },
        { "temperatureDamageMultiplier", 2f },
        { "temperatureResistanceMultiplier", 3f },
        { "enableAutoHatch", false },
        { "oxygenTankDrainMultiplier", 0.01f },
        { "fuelTankDrainMultiplier", 0.1f },
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
        { "addPortableCampfire", false },
        { "keepHelmetOn", true },
        { "showWarningNotifications", true },
        { "shipExplosionMultiplier", 5f },
        { "shipBounciness", 0.2f },
        { "enablePersistentInput", false },
        { "shipInputLatency", 0f },
        { "addEngineSwitch", true },
        { "idleFuelConsumptionMultiplier", 1f },
        { "shipLightColor", "Default" },
        { "hotThrusters", true },
        { "extraNoise", true },
        { "interiorHullColor", "Default" },
        { "exteriorHullColor", "Default" },
        { "addTether", false },
        { "disableDamageIndicators", false },
        { "addShipSignal", false },
        { "reactorLifetimeMultiplier", 2f },
        { "disableShipFriction", false },
        { "enableSignalscopeComponent", true },
        { "rustLevel", 0f },
        { "dirtAccumulationTime", 450f },
        { "thrusterColor", "Default" },
        { "disableSeatbelt", false },
        { "addPortableTractorBeam", false },
        { "disableShipSuit", false },
        { "damageIndicatorColor", "Default" },
        { "disableAutoLights", false },
        { "addExpeditionFlag", true },
        { "addFuelCanister", true },
        { "chaoticCyclones", true },
        { "moreExplosionDamage", true },
        { "singleUseTractorBeam", false },
        { "disableRetroRockets", true },
        { "maxDirtAccumulation", 0.75f },
        { "addShipWarpCore", false },
        { "repairTimeMultiplier", 1.8f },
        { "airDragMultiplier", 0.75f },
        { "addShipClock", false },
        { "enableStunDamage", false },
        { "enableRepairConfirmation", false },
        { "shipGravityFix", true },
        { "enableRemovableGravityCrystal", false },
        { "randomHullDamage", 0f },
        { "randomComponentDamage", 0f },
        { "enableFragileShip", true },
        { "faultyHeatRegulators", true },
    };

    public static readonly Dictionary<string, object> WandererSettings = new Dictionary<string, object>()
    {
        { "disableGravityCrystal", false },
        { "disableEjectButton", false },
        { "disableHeadlights", true },
        { "disableLandingCamera", false },
        { "disableShipLights", false },
        { "disableShipOxygen", true },
        { "oxygenDrainMultiplier", 1f },
        { "fuelDrainMultiplier", 0.8f },
        { "shipDamageMultiplier", 0.8f },
        { "shipDamageSpeedMultiplier", 1.2f },
        { "shipOxygenRefill", true },
        { "disableShipRepair", false },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", false },
        { "disableWaterAutoRoll", false },
        { "enableThrustModulator", true },
        { "temperatureZonesAmount", "None" },
        { "hullTemperatureDamage", false },
        { "enableShipFuelTransfer", true },
        { "enableJetpackRefuelDrain", true },
        { "disableReferenceFrame", true },
        { "disableMapMarkers", true },
        { "gravityMultiplier", 1.2f },
        { "fuelTransferMultiplier", 1.5f },
        { "oxygenRefillMultiplier", 0.5f },
        { "temperatureDamageMultiplier", 1f },
        { "temperatureResistanceMultiplier", 1f },
        { "enableAutoHatch", false },
        { "oxygenTankDrainMultiplier", 1f },
        { "fuelTankDrainMultiplier", 5f },
        { "componentTemperatureDamage", false },
        { "atmosphereAngularDragMultiplier", 1f },
        { "spaceAngularDragMultiplier", 0.6f },
        { "disableRotationSpeedLimit", false },
        { "gravityDirection", "Down" },
        { "disableScoutRecall", true },
        { "disableScoutLaunching", false },
        { "enableScoutLauncherComponent", false },
        { "enableManualScoutRecall", true },
        { "enableShipItemPlacement", true },
        { "addPortableCampfire", true },
        { "keepHelmetOn", true },
        { "showWarningNotifications", true },
        { "shipExplosionMultiplier", 1f },
        { "shipBounciness", 0f },
        { "enablePersistentInput", false },
        { "shipInputLatency", 0f },
        { "addEngineSwitch", false },
        { "idleFuelConsumptionMultiplier", 0f },
        { "shipLightColor", "Default" },
        { "hotThrusters", true },
        { "extraNoise", true },
        { "interiorHullColor", "Default" },
        { "exteriorHullColor", "Default" },
        { "addTether", false },
        { "disableDamageIndicators", false },
        { "addShipSignal", false },
        { "reactorLifetimeMultiplier", 2f },
        { "disableShipFriction", false },
        { "enableSignalscopeComponent", true },
        { "rustLevel", 0.1f },
        { "dirtAccumulationTime", 0f },
        { "thrusterColor", "Default" },
        { "disableSeatbelt", false },
        { "addPortableTractorBeam", true },
        { "disableShipSuit", false },
        { "damageIndicatorColor", "Default" },
        { "disableAutoLights", true },
        { "addExpeditionFlag", true },
        { "addFuelCanister", false },
        { "chaoticCyclones", true },
        { "moreExplosionDamage", true },
        { "singleUseTractorBeam", false },
        { "disableRetroRockets", false },
        { "maxDirtAccumulation", 0.75f },
        { "addShipWarpCore", false },
        { "repairTimeMultiplier", 1.2f },
        { "airDragMultiplier", 0f },
        { "addShipClock", true },
        { "enableStunDamage", true },
        { "enableRepairConfirmation", true },
        { "shipGravityFix", true },
        { "enableRemovableGravityCrystal", true },
        { "randomHullDamage", 0f },
        { "randomComponentDamage", 0f },
        { "enableFragileShip", false },
        { "faultyHeatRegulators", false },
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
        { "disableShipRepair", false },
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
        { "enablePersistentInput", true },
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
        { "disableShipFriction", true },
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
        { "chaoticCyclones", true },
        { "moreExplosionDamage", true },
        { "singleUseTractorBeam", true },
        { "disableRetroRockets", false },
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
    };

    public static readonly Dictionary<string, RandomSettingValue> RandomSettings = new Dictionary<string, RandomSettingValue>()
    {
        { "disableGravityCrystal", new RandomSettingValue(0.5f) },
        { "disableEjectButton", new RandomSettingValue(0.5f) },
        { "disableHeadlights", new RandomSettingValue(0.5f) },
        { "disableLandingCamera", new RandomSettingValue(0.5f) },
        { "disableShipLights", new RandomSettingValue(0.5f) },
        { "disableShipOxygen", new RandomSettingValue(0.2f) },
        { "oxygenDrainMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (1f, 10f),
            (10f, 50f),
            (50f, 100f),
            (100f, 200f),
            (200f, 800f)
        }, 0.75f, 1f) },
        { "fuelDrainMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.5f, 2f),
            (2f, 8f),
            (8f, 20f),
            (20f, 50f),
        }, 0.5f, 1f) },
        { "shipDamageMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.1f, 0.8f),
            (0.8f, 1.5f),
            (1.5f, 3f),
            (3f, 6f)
        }, 0.75f, 1f) },
        { "shipDamageSpeedMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.5f, 1f),
            (1f, 1.5f),
            (1.5f, 2.5f),
            (2.5f, 3.5f),
            (3.5f, 5f)
        }, 0.75f, 1f)},
        { "shipOxygenRefill", new RandomSettingValue(0.8f) },
        { "disableShipRepair", new RandomSettingValue(0.1f) },
        { "enableGravityLandingGear", new RandomSettingValue(0.5f) },
        { "disableAirAutoRoll", new RandomSettingValue(0.5f) },
        { "disableWaterAutoRoll", new RandomSettingValue(0.5f) },
        { "enableThrustModulator", new RandomSettingValue(0.5f) },
        { "temperatureZonesAmount", new RandomSettingValue(["All"], 0.5f, "None") },
        { "hullTemperatureDamage", new RandomSettingValue(0.75f) },
        { "enableShipFuelTransfer", new RandomSettingValue(0.8f) },
        { "enableJetpackRefuelDrain", new RandomSettingValue(0.8f) },
        { "disableReferenceFrame", new RandomSettingValue(0.25f) },
        { "disableMapMarkers", new RandomSettingValue(0.25f) },
        { "gravityMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.5f, 0.9f),
            (0.9f, 1.2f),
            (1.2f, 1.8f)
        }, 0.5f, 1f) },
        { "fuelTransferMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.2f, 1f),
            (1f, 1.5f),
            (1.5f, 2f),
            (2f, 5f)
        }, 0.5f, 1f) },
        { "oxygenRefillMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.5f, 0.9f),
            (0.9f, 1.2f),
            (1.2f, 1.8f),
            (1.8f, 2.5f),
            (2.5f, 5f)
        }, 0.5f, 1f) },
        { "temperatureDamageMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.1f, 0.5f),
            (0.5f, 1f),
            (1f, 1.2f),
            (1.2f, 1.8f),
            (1.8f, 2.5f),
            (2.5f, 5f)
        }, 0.8f, 1f) },
        { "temperatureResistanceMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.1f, 0.4f),
            (0.4f, 0.75f),
            (0.75f, 1f),
            (1f, 1.5f),
            (1.5f, 3f),
        }, 0.8f, 1f) },
        { "enableAutoHatch", new RandomSettingValue(0.5f) },
        { "oxygenTankDrainMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.5f, 1f),
            (1f, 2.5f),
            (2.5f, 4f),
            (4f, 10f),
        }, 0.8f, 1f) },
        { "fuelTankDrainMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.05f, 0.5f),
            (0.5f, 1.2f),
            (1.2f, 2f),
            (2f, 5f),
        }, 0.8f, 1f) },
        { "componentTemperatureDamage", new RandomSettingValue(0.5f) },
        { "atmosphereAngularDragMultiplier", new RandomSettingValue(0f, 2f, 0.5f, 1f) },
        { "spaceAngularDragMultiplier", new RandomSettingValue(0f, 2f, 0.5f, 1f) },
        { "disableRotationSpeedLimit", new RandomSettingValue(0.2f) },
        { "gravityDirection", new RandomSettingValue(["Up", "Left", "Right", "Forward", "Back", "Random"], 0.1f, "Down") },
        { "disableScoutRecall", new RandomSettingValue(0.5f) },
        { "disableScoutLaunching", new RandomSettingValue(0.5f) },
        { "enableScoutLauncherComponent", new RandomSettingValue(0.5f) },
        { "enableManualScoutRecall", new RandomSettingValue(0.25f) },
        { "enableShipItemPlacement", new RandomSettingValue(0.5f) },
        { "addPortableCampfire", new RandomSettingValue(0.5f) },
        { "keepHelmetOn", new RandomSettingValue(1f) },
        { "showWarningNotifications", new RandomSettingValue(1f) },
        { "shipExplosionMultiplier", new RandomSettingValue(0.5f, 50f, 0.75f, 1f) },
        { "shipBounciness", new RandomSettingValue(new (object, object)[]
        {
            (0f, 0.2f),
            (0.2f, 0.5f),
            (0.5f, 1f),
            (1f, 3f)
        }, 0.5f, 0f) },
        { "enablePersistentInput", new RandomSettingValue(0.5f) },
        { "shipInputLatency", new RandomSettingValue(new (object, object)[]
        {
            (0f, 0.2f),
            (0.2f, 0.5f),
            (0.5f, 1f),
            (1f, 2f)
        }, 0.1f, 0f) },
        { "addEngineSwitch", new RandomSettingValue(0.5f) },
        { "idleFuelConsumptionMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0f, 0.5f),
            (0.5f, 1.5f),
            (1.5f, 3f),
            (3f, 8f)
        }, 0.5f, 0f) },
        { "shipLightColor", new RandomSettingValue([
            "Default",
            "Red",
            "Hearthian Orange",
            "Orange",
            "Yellow",
            "Green",
            "Ghostly Green",
            "Turquoise",
            "Blue",
            "Nomaian Blue",
            "Blacklight",
            "Purple",
            "Magenta",
            "White",
            "Divine",
            "Rainbow"
            ], 0.25f, "Default") },
        { "hotThrusters", new RandomSettingValue(0.8f) },
        { "extraNoise", new RandomSettingValue(0.8f) },
        { "interiorHullColor", new RandomSettingValue([
            "Default",
            "Red",
            "Orange",
            "Golden",
            "Green",
            "Turquoise",
            "Blue",
            "Lavender",
            "Pink",
            "Gray",
            "Rainbow"
            ], 0.25f, "Default") },
        { "exteriorHullColor", new RandomSettingValue([
            "Default",
            "Red",
            "Orange",
            "Golden",
            "Green",
            "Turquoise",
            "Blue",
            "Lavender",
            "Pink",
            "Gray",
            "Rainbow"
            ], 0.25f, "Default") },
        { "addTether", new RandomSettingValue(0.5f) },
        { "disableDamageIndicators", new RandomSettingValue(0.3f) },
        { "addShipSignal", new RandomSettingValue(0.5f) },
        { "reactorLifetimeMultiplier", new RandomSettingValue(0.1f, 3f, 0.5f, 1f) },
        { "disableShipFriction", new RandomSettingValue(0.25f) },
        { "enableSignalscopeComponent", new RandomSettingValue(0.5f) },
        { "rustLevel", new RandomSettingValue(new (object, object)[]
        {
            (0.1f, 0.2f),
            (0.2f, 0.25f),
            (0.25f, 0.4f),
            (0.4f, 1f),
        }, 0.25f, 0f) },
        { "dirtAccumulationTime", new RandomSettingValue(new (object, object)[]
        {
            (60f, 110f),
            (110f, 140f),
            (140f, 200f),
            (200f, 350f),
            (350f, 600f),
        }, 0.25f, 0f) },
        { "thrusterColor", new RandomSettingValue([
            "Default",
            "Red",
            "White-Orange",
            "Lime-Orange",
            "Lime",
            "Ghostly Green",
            "Turquoise",
            "Blue",
            "Purple",
            "Rose",
            "Pink",
            "Rainbow"
            ], 0.25f, "Default") },
        { "disableSeatbelt", new RandomSettingValue(0.25f) },
        { "addPortableTractorBeam", new RandomSettingValue(0.5f) },
        { "disableShipSuit", new RandomSettingValue(0.1f) },
        { "damageIndicatorColor", new RandomSettingValue([
            "Default",
            "Orange",
            "Yellow",
            "Green",
            "Outer Wilds Beta",
            "Ghostly Green",
            "Turquoise",
            "Blue",
            "Dark Blue",
            "Nomaian Blue",
            "Purple",
            "Lavender",
            "Pink",
            "Rainbow"
            ], 0.25f, "Default") },
        { "disableAutoLights", new RandomSettingValue(0.5f) },
        { "addExpeditionFlag", new RandomSettingValue(0.5f) },
        { "addFuelCanister", new RandomSettingValue(0.5f) },
        { "chaoticCyclones", new RandomSettingValue(0.75f) },
        { "moreExplosionDamage", new RandomSettingValue(0.75f) },
        { "singleUseTractorBeam", new RandomSettingValue(0.25f) },
        { "disableRetroRockets", new RandomSettingValue(0.25f) },
        { "maxDirtAccumulation", new RandomSettingValue(new (object, object)[]
        {
            (0.2f, 0.4f),
            (0.4f, 0.8f),
            (0.8f, 1f)
        }, 0.75f, 0.75f) },
        { "addShipWarpCore", new RandomSettingValue(0.5f) },
        { "repairTimeMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.3f, 0.5f),
            (0.5f, 1.5f),
            (1.5f, 2f),
            (2.5f, 3f)
        }, 0.75f, 1f) },
        { "airDragMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0f, 0f),
            (0f, 0.2f),
            (0.2f, 1.5f),
            (1.5f, 2.5f)
        }, 0.75f, 1f) },
        { "addShipClock", new RandomSettingValue(0.5f) },
        { "enableStunDamage", new RandomSettingValue(0.75f) },
        { "enableRepairConfirmation", new RandomSettingValue(0.75f) },
        { "shipGravityFix", new RandomSettingValue(1f) },
        { "enableRemovableGravityCrystal", new RandomSettingValue(0.5f) },
        { "randomHullDamage", new RandomSettingValue(0f, 1f, 0.4f, 0f) },
        { "randomComponentDamage", new RandomSettingValue(0f, 1f, 0.3f, 0f) },
        { "enableFragileShip", new RandomSettingValue(0.1f) },
        { "faultyHeatRegulators", new RandomSettingValue(0.4f) },
    };

    public static Dictionary<PresetName, Dictionary<string, object>> presetDicts { get; private set; }

    public static Dictionary<string, Dictionary<PresetName, object>> settingsPresets { get; private set; }

    private static bool _initialized = false;

    public enum PresetName
    {
        VanillaPlus = 1,
        Minimal = 2,
        Relaxed = 4,
        Hardcore = 8,
        Wanderer = 16,
        Pandemonium = 32,
        Random = 64,
        Custom = 0
    }

    public static void InitializePresets()
    {
        presetDicts = new()
        {
            { PresetName.VanillaPlus, VanillaPlusSettings },
            { PresetName.Minimal, MinimalSettings },
            { PresetName.Relaxed, RelaxedSettings },
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
        if (preset == PresetName.Custom || preset == PresetName.Random) return;
        foreach (KeyValuePair<string, object> setting in presetDicts[preset])
        {
            config.SetSettingsValue(setting.Key, setting.Value);
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
        return preset == PresetName.VanillaPlus ? "Vanilla Plus" : preset.ToString();
    }

    public static object GetPresetSetting(this PresetName preset, string setting)
    {
        if (preset == PresetName.Random)
        {
            return RandomSettings[setting].GetRandomValue();
        }
        return settingsPresets[setting][preset];
    }

    public static bool Initialized()
    {
        return _initialized;
    }
}
