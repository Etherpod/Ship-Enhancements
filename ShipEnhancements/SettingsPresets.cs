using OWML.Common;
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
        { "enableJetpackRefuelDrain", false },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 1f },
        { "oxygenRefillMultiplier", 1f },
        { "temperatureDamageMultiplier", 1f },
        { "temperatureResistanceMultiplier", 1f },
        { "enableAutoHatch", true },
        { "oxygenTankDrainMultiplier", 1f },
        { "fuelTankDrainMultiplier", 3f },
        { "componentTemperatureDamage", false },
        { "atmosphereAngularDragMultiplier", 1f },
        { "spaceAngularDragMultiplier", 1f },
        { "disableRotationSpeedLimit", false },
        { "gravityDirection", "Down" },
        { "disableScoutRecall", false },
        { "disableScoutLaunching", false },
        { "enableScoutLauncherComponent", true },
        { "enableManualScoutRecall", true },
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
        { "disableDamageIndicators", true },
        { "addShipSignal", false },
        { "reactorLifetimeMultiplier", 1f },
        { "disableShipFriction", false },
        { "enableSignalscopeComponent", false },
        { "rustLevel", 0f },
        { "dirtAccumulationTime", 0f },
        { "thrusterColor", "Default" },
        { "disableSeatbelt", true },
        { "addPortableTractorBeam", false },
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
        { "fuelDrainMultiplier", 0.8f },
        { "shipDamageMultiplier", 0.6f },
        { "shipDamageSpeedMultiplier", 1.5f },
        { "shipOxygenRefill", true },
        { "disableShipRepair", false },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", false },
        { "disableWaterAutoRoll", false },
        { "enableThrustModulator", false },
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
        { "shipExplosionMultiplier", 0.8f },
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
        { "addTether", true },
        { "disableDamageIndicators", false },
        { "addShipSignal", true },
        { "reactorLifetimeMultiplier", 1f },
        { "disableShipFriction", false },
        { "enableSignalscopeComponent", false },
        { "rustLevel", 0f },
        { "dirtAccumulationTime", 0f },
        { "thrusterColor", "Default" },
        { "disableSeatbelt", false },
        { "addPortableTractorBeam", false },
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
        { "shipDamageSpeedMultiplier", 0.6f },
        { "shipOxygenRefill", true },
        { "disableShipRepair", true },
        { "enableGravityLandingGear", false },
        { "disableAirAutoRoll", false },
        { "disableWaterAutoRoll", false },
        { "enableThrustModulator", false },
        { "temperatureZonesAmount", "All" },
        { "hullTemperatureDamage", true },
        { "enableShipFuelTransfer", true },
        { "enableJetpackRefuelDrain", true },
        { "disableReferenceFrame", false },
        { "disableMapMarkers", false },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 0.8f },
        { "oxygenRefillMultiplier", 0.9f },
        { "temperatureDamageMultiplier", 2f },
        { "temperatureResistanceMultiplier", 1.5f },
        { "enableAutoHatch", false },
        { "oxygenTankDrainMultiplier", 0.01f },
        { "fuelTankDrainMultiplier", 0.1f },
        { "componentTemperatureDamage", false },
        { "atmosphereAngularDragMultiplier", 1f },
        { "spaceAngularDragMultiplier", 1f },
        { "disableRotationSpeedLimit", false },
        { "gravityDirection", "Down" },
        { "disableScoutRecall", false },
        { "disableScoutLaunching", false },
        { "enableScoutLauncherComponent", true },
        { "enableManualScoutRecall", false },
        { "enableShipItemPlacement", false },
        { "addPortableCampfire", false },
        { "keepHelmetOn", true },
        { "showWarningNotifications", true },
        { "shipExplosionMultiplier", 2f },
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
        { "addTether", true },
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
        { "hullTemperatureDamage", false },
        { "enableShipFuelTransfer", false },
        { "enableJetpackRefuelDrain", true },
        { "disableReferenceFrame", true },
        { "disableMapMarkers", true },
        { "gravityMultiplier", 1f },
        { "fuelTransferMultiplier", 1f },
        { "oxygenRefillMultiplier", 0.8f },
        { "temperatureDamageMultiplier", 1f },
        { "temperatureResistanceMultiplier", 1f },
        { "enableAutoHatch", false },
        { "oxygenTankDrainMultiplier", 1f },
        { "fuelTankDrainMultiplier", 1.2f },
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
        { "enablePersistentInput", false },
        { "shipInputLatency", 0f },
        { "addEngineSwitch", false },
        { "idleFuelConsumptionMultiplier", 0f },
        { "shipLightColor", "Default" },
        { "hotThrusters", true },
        { "extraNoise", true },
        { "interiorHullColor", "Default" },
        { "exteriorHullColor", "Default" },
        { "addTether", true },
        { "disableDamageIndicators", false },
        { "addShipSignal", false },
        { "reactorLifetimeMultiplier", 1f },
        { "disableShipFriction", false },
        { "enableSignalscopeComponent", true },
        { "rustLevel", 0f },
        { "dirtAccumulationTime", 0f },
        { "thrusterColor", "Default" },
        { "disableSeatbelt", false },
        { "addPortableTractorBeam", false },
    };

    public static readonly Dictionary<string, object> PandemoniumSettings = new Dictionary<string, object>()
    {
        { "disableGravityCrystal", false },
        { "disableEjectButton", false },
        { "disableHeadlights", false },
        { "disableLandingCamera", true },
        { "disableShipLights", false },
        { "disableShipOxygen", false },
        { "oxygenDrainMultiplier", 600f },
        { "fuelDrainMultiplier", 12f },
        { "shipDamageMultiplier", 12f },
        { "shipDamageSpeedMultiplier", 0.4f },
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
        { "gravityMultiplier", 0.2f },
        { "fuelTransferMultiplier", 2f },
        { "oxygenRefillMultiplier", 0.4f },
        { "temperatureDamageMultiplier", 8f },
        { "temperatureResistanceMultiplier", 0.4f },
        { "enableAutoHatch", true },
        { "oxygenTankDrainMultiplier", 10f },
        { "fuelTankDrainMultiplier", 10f },
        { "componentTemperatureDamage", true },
        { "atmosphereAngularDragMultiplier", 1.5f },
        { "spaceAngularDragMultiplier", 1f },
        { "disableRotationSpeedLimit", false },
        { "gravityDirection", "Random" },
        { "disableScoutRecall", false },
        { "disableScoutLaunching", false },
        { "enableScoutLauncherComponent", true },
        { "enableManualScoutRecall", true },
        { "enableShipItemPlacement", false },
        { "addPortableCampfire", false },
        { "keepHelmetOn", true },
        { "showWarningNotifications", true },
        { "shipExplosionMultiplier", 50f },
        { "shipBounciness", 1f },
        { "enablePersistentInput", false },
        { "shipInputLatency", 0.1f },
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
    };

    /*public static readonly Dictionary<string, object[]> RandomSettings = new Dictionary<string, object[]>()
    {
        { "disableGravityCrystal", [false, false, true] },
        { "disableEjectButton", [false, false, true] },
        { "disableHeadlights", [false, true] },
        { "disableLandingCamera", [false, true] },
        { "disableShipLights", [false, false, true] },
        { "disableShipOxygen", [false, false, true] },
        { "oxygenDrainMultiplier", [1f, 10f, 100f, 500f, 1f, 2000f] },
        { "fuelDrainMultiplier", [1f, 2f, 5f, 10f, 0.5f, 100f] },
        { "shipDamageMultiplier", [1f, 2f, 3f, 0.1f, 20f] },
        { "shipDamageSpeedMultiplier", [1f, 0.8f, 1.2f, 0.1f, 5f] },
        { "shipOxygenRefill", [false, true, true, true] },
        { "disableShipRepair", [false, false, false, false, true] },
        { "enableGravityLandingGear", [false, false, true] },
        { "disableAirAutoRoll", [false, true] },
        { "disableWaterAutoRoll", [false, true] },
        { "enableThrustModulator", [false, false, true] },
        { "temperatureZonesAmount", ["None", "None", "Sun", "All"] },
        { "hullTemperatureDamage", [false, true] },
        { "enableShipFuelTransfer", [false, true, true, true] },
        { "enableJetpackRefuelDrain", [false, true, true, true] },
        { "disableReferenceFrame", [false, false, true] },
        { "disableMapMarkers", [false, true] },
        { "gravityMultiplier", [1f, 1f, 0.1f, 3f] },
        { "fuelTransferMultiplier", [0.2f, 10f] },
        { "oxygenRefillMultiplier", [0.1f, 3f] },
        { "temperatureDamageMultiplier", [0.2f, 20f] },
        { "temperatureResistanceMultiplier", [0.05f, 2.5f] },
        { "enableAutoHatch", [false, false, true] },
        { "oxygenTankDrainMultiplier", [0.01f, 10f] },
        { "fuelTankDrainMultiplier", [0.01f, 10f] },
        { "componentTemperatureDamage", [false, false, true] },
        { "atmosphereAngularDragMultiplier", [1f, 1f, 0f, 2f] },
        { "spaceAngularDragMultiplier", [1f, 1f, 0f, 2f] },
        { "disableRotationSpeedLimit", [false, false, false, true] },
        { "gravityDirection", ["Down", "Down", "Random"] },
        { "disableScoutRecall", [false, false, true] },
        { "disableScoutLaunching", [false, false, true] },
        { "enableScoutLauncherComponent", [false, true] },
        { "enableManualScoutRecall", [false, false, true] },
        { "enableShipItemPlacement", [false, true] },
        { "addPortableCampfire", [false, false, true] },
        { "keepHelmetOn", [true] },
        { "showWarningNotifications", [true] },
        { "shipExplosionMultiplier", [0.1f, 50f] },
        { "shipBounciness", [0f, 0f, 0f, 2f] },
        { "enablePersistentInput", [false, false, true] },
        { "shipInputLatency", [0f, 0f, 0f, 0f, 0.2f, 1f, 0f, 0.5f] },
        { "addEngineSwitch", [false, false, true] },
        { "idleFuelConsumptionMultiplier", [0f, 0f, 0f, 0f, 0f, 3f] },
        { "shipLightColor", [
            "Default",
            "Default",
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
            ] },
        { "hotThrusters", [false, true] },
        { "extraNoise", [false, true] },
        { "interiorHullColor", [
            "Default",
            "Default",
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
            ] },
        { "exteriorHullColor", [
            "Default",
            "Default",
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
            ] },
        { "addTether", [false, false, true] },
        { "disableDamageIndicators", [false, false, true] },
        { "addShipSignal", [false, true] },
        { "reactorLifetimeMultiplier", [1f, 0.1f, 3f] },
        { "disableShipFriction", [false, false, false, true] },
        { "enableSignalscopeComponent", [false, true] },
        { "rustLevel", [0f, 0f, 0f, 0.1f, 0.2f, 0.5f, 0f, 1f] },
        { "dirtAccumulationTime", [0f, 0f, 0f, 0f, 800f, 100f, 500f] },
        { "thrusterColor", [
            "Default",
            "Default",
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
            ] },
        { "disableSeatbelt", [false, false, false, true] },
        { "addPortableTractorBeam", [false, false, true] },
    };*/

    public static readonly Dictionary<string, RandomSettingValue> RandomSettings = new Dictionary<string, RandomSettingValue>()
    {
        { "disableGravityCrystal", new RandomSettingValue(0.3f) },
        { "disableEjectButton", new RandomSettingValue(0.3f) },
        { "disableHeadlights", new RandomSettingValue(0.5f) },
        { "disableLandingCamera", new RandomSettingValue(0.5f) },
        { "disableShipLights", new RandomSettingValue(0.3f) },
        { "disableShipOxygen", new RandomSettingValue(0.3f) },
        { "oxygenDrainMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (1f, 10f),
            (10f, 50f),
            (50f, 100f),
            (100f, 200f),
            (200f, 800f)
        }, 0.6f, 1f) },
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
        }, 0.4f, 1f) },
        { "shipDamageSpeedMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.5f, 1f),
            (1f, 1.5f),
            (1.5f, 2.5f),
            (2.5f, 3.5f),
            (3.5f, 5f)
        }, 0.6f, 1f)},
        { "shipOxygenRefill", new RandomSettingValue(0.8f) },
        { "disableShipRepair", new RandomSettingValue(0.1f) },
        { "enableGravityLandingGear", new RandomSettingValue(0.3f) },
        { "disableAirAutoRoll", new RandomSettingValue(0.5f) },
        { "disableWaterAutoRoll", new RandomSettingValue(0.5f) },
        { "enableThrustModulator", new RandomSettingValue(0.3f) },
        { "temperatureZonesAmount", new RandomSettingValue(["Sun", "All"], 0.5f, "None") },
        { "hullTemperatureDamage", new RandomSettingValue(0.5f) },
        { "enableShipFuelTransfer", new RandomSettingValue(0.8f) },
        { "enableJetpackRefuelDrain", new RandomSettingValue(0.8f) },
        { "disableReferenceFrame", new RandomSettingValue(0.3f) },
        { "disableMapMarkers", new RandomSettingValue(0.5f) },
        { "gravityMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.5f, 0.9f),
            (0.9f, 1.2f),
            (1.2f, 1.8f)
        }, 0.2f, 1f) },
        { "fuelTransferMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.2f, 1f),
            (1f, 1.5f),
            (1.5f, 2f),
            (2f, 5f)
        }, 0.3f, 1f) },
        { "oxygenRefillMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.5f, 0.9f),
            (0.9f, 1.2f),
            (1.2f, 1.8f),
            (1.8f, 2.5f),
            (2.5f, 5f)
        }, 0.3f, 1f) },
        { "temperatureDamageMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.1f, 0.5f),
            (0.5f, 1f),
            (1f, 1.2f),
            (1.2f, 1.8f),
            (1.8f, 2.5f),
            (2.5f, 5f)
        }, 0.3f, 1f) },
        { "temperatureResistanceMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.1f, 0.4f),
            (0.4f, 0.75f),
            (0.75f, 1f),
            (1f, 1.5f),
            (1.5f, 3f),
        }, 0.3f, 1f) },
        { "enableAutoHatch", new RandomSettingValue(0.3f) },
        { "oxygenTankDrainMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.5f, 1f),
            (1f, 2.5f),
            (2.5f, 4f),
            (4f, 10f),
        }, 0.3f, 1f) },
        { "fuelTankDrainMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0.05f, 0.5f),
            (0.5f, 1.2f),
            (1.2f, 2f),
            (2f, 5f),
        }, 0.3f, 1f) },
        { "componentTemperatureDamage", new RandomSettingValue(0.3f) },
        { "atmosphereAngularDragMultiplier", new RandomSettingValue(0f, 2f, 0.1f, 1f) },
        { "spaceAngularDragMultiplier", new RandomSettingValue(0f, 2f, 0.1f, 1f) },
        { "disableRotationSpeedLimit", new RandomSettingValue(0.1f) },
        { "gravityDirection", new RandomSettingValue(["Random"], 0.1f, "Default") },
        { "disableScoutRecall", new RandomSettingValue(0.3f) },
        { "disableScoutLaunching", new RandomSettingValue(0.2f) },
        { "enableScoutLauncherComponent", new RandomSettingValue(0.5f) },
        { "enableManualScoutRecall", new RandomSettingValue(0.4f) },
        { "enableShipItemPlacement", new RandomSettingValue(0.5f) },
        { "addPortableCampfire", new RandomSettingValue(0.4f) },
        { "keepHelmetOn", new RandomSettingValue(1f) },
        { "showWarningNotifications", new RandomSettingValue(0.7f) },
        { "shipExplosionMultiplier", new RandomSettingValue(0.5f, 50f, 0.5f, 1f) },
        { "shipBounciness", new RandomSettingValue(new (object, object)[]
        {
            (0f, 0.5f),
            (0f, 1f),
            (1f, 2f),
            (0f, 3f)
        }, 0.2f, 0f) },
        { "enablePersistentInput", new RandomSettingValue(0.3f) },
        { "shipInputLatency", new RandomSettingValue(new (object, object)[]
        {
            (0f, 0.5f),
            (0.5f, 1.5f),
            (0f, 1f),
            (0.8f, 2f)
        }, 0.2f, 0f) },
        { "addEngineSwitch", new RandomSettingValue(0.3f) },
        { "idleFuelConsumptionMultiplier", new RandomSettingValue(new (object, object)[]
        {
            (0f, 0.8f),
            (0f, 1f),
            (0f, 5f),
            (0f, 8f)
        }, 0.1f, 0f) },
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
            ], 0.5f, "Default") },
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
            ], 0.5f, "Default") },
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
            ], 0.5f, "Default") },
        { "addTether", new RandomSettingValue(0.3f) },
        { "disableDamageIndicators", new RandomSettingValue(0.3f) },
        { "addShipSignal", new RandomSettingValue(0.5f) },
        { "reactorLifetimeMultiplier", new RandomSettingValue(0.1f, 3f, 0.3f, 1f) },
        { "disableShipFriction", new RandomSettingValue(0.1f) },
        { "enableSignalscopeComponent", new RandomSettingValue(0.5f) },
        { "rustLevel", new RandomSettingValue(new (object, object)[]
        {
            (0.1f, 0.2f),
            (0.2f, 0.25f),
            (0.25f, 0.4f),
            (0.4f, 1f),
        }, 0.15f, 0f) },
        { "dirtAccumulationTime", new RandomSettingValue(new (object, object)[]
        {
            (60f, 110f),
            (110f, 140f),
            (140f, 200f),
            (200f, 350f),
            (350f, 600f),
        }, 0.15f, 0f) },
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
            ], 0.5f, "Default") },
        { "disableSeatbelt", new RandomSettingValue(0.1f) },
        { "addPortableTractorBeam", new RandomSettingValue(0.3f) },
    };

    public static Dictionary<PresetName, Dictionary<string, object>> presetDicts { get; private set; }

    public static Dictionary<string, Dictionary<PresetName, object>> settingsPresets { get; private set; }

    private static bool _initialized = false;

    public enum PresetName
    {
        VanillaPlus,
        Minimal,
        Relaxed,
        Hardcore,
        Wanderer,
        Pandemonium,
        Random,
        Custom
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
