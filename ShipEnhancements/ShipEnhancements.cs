using OWML.Common;
using OWML.ModHelper;
using System.Collections;
using UnityEngine;
using System.IO;
using System;
using DitzyExtensions.Collection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.EnterpriseServices;

namespace ShipEnhancements;

public class ShipEnhancements : ModBehaviour
{
    public delegate void SwitchEvent(bool enabled);
    public event SwitchEvent OnGravityLandingGearSwitch;

    public static ShipEnhancements Instance;
    public bool oxygenDepleted;
    public bool refillingOxygen;

    public bool HeadlightsDisabled { get; private set; }
    public bool LandingCameraDisabled { get; private set; }
    public float OxygenDrainMultiplier { get; private set; }
    public float FuelDrainMultiplier { get; private set; }
    public float DamageMultiplier { get; private set; }
    public float DamageSpeedMultiplier { get; private set; }
    public bool ShipOxygenRefill { get; private set; }
    public bool OxygenDisabled { get; private set; }
    public bool ShipRepairDisabled { get; private set; }
    public bool GravityLandingGearEnabled { get; private set; }
    public bool AirAutoRollDisabled { get; private set; }
    public bool WaterAutoRollDisabled { get; private set; }
    public bool ThrustModulatorEnabled { get; private set; }
    public int ThrustModulatorLevel { get; private set; }
    public bool ReferenceFrameDisabled { get; private set; }
    public bool MapMarkersDisabled { get; private set; }
    public float FuelTransferMultiplier { get; private set; }
    public float OxygenRefillMultiplier { get; private set; }
    public float TemperatureDamageMultiplier { get; private set; }
    public float TemperatureResistanceMultiplier { get; private set; }
    public bool AutoHatchEnabled { get; private set; }

    private SettingsPresets.PresetName _currentPreset = (SettingsPresets.PresetName)(-1);

    private AssetBundle _shipEnhancementsBundle;
    private float _lastSuitOxygen;
    private bool _shipLoaded = false;
    private OxygenDetector _shipOxygenDetector;
    private ShipResources _shipResources;
    private OxygenVolume _shipOxygen;
    private PlayerResources _playerResources;

    public enum Settings
    {
        disableGravityCrystal,
        disableEjectButton,
        disableHeadlights,
        disableLandingCamera,
        disableShipLights,
        disableShipOxygen,
        oxygenDrainMultiplier,
        fuelDrainMultiplier,
        shipDamageMultiplier,
        shipDamageSpeedMultiplier,
        shipOxygenRefill,
        disableShipRepair,
        enableGravityLandingGear,
        disableAirAutoRoll,
        disableWaterAutoRoll,
        enableThrustModulator,
        temperatureZonesAmount,
        enableTemperatureDamage,
        enableShipFuelTransfer,
        enableJetpackRefuelDrain,
        disableReferenceFrame,
        disableMapMarkers,
        gravityMultiplier,
        fuelTransferMultiplier,
        oxygenRefillMultiplier,
        temperatureDamageMultiplier,
        temperatureResistanceMultiplier,
        enableAutoHatch,
    }

    private void Awake()
    {
        Instance = this;
        HarmonyLib.Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly());
    }

    private void Start()
    {
        _shipEnhancementsBundle = AssetBundle.LoadFromFile(Path.Combine(ModHelper.Manifest.ModFolderPath, "assets/shipenhancements"));

        ModCompatibility.Initialize();
        SettingsPresets.InitializePresets();

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

            GlobalMessenger.AddListener("SuitUp", OnPlayerSuitUp);
            GlobalMessenger.AddListener("RemoveSuit", OnPlayerRemoveSuit);
            oxygenDepleted = false;

            StartCoroutine(InitializeShip());
        };

        LoadManager.OnStartSceneLoad += (scene, loadScene) =>
        {
            if (scene == OWScene.TitleScreen) UpdateProperties();
            if (scene != OWScene.SolarSystem) return;

            UpdateProperties();
            GlobalMessenger.RemoveListener("SuitUp", OnPlayerSuitUp);
            GlobalMessenger.RemoveListener("RemoveSuit", OnPlayerRemoveSuit);
            if ((bool)Settings.enableAutoHatch.GetValue())
            {
                GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
                GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
            }
            _lastSuitOxygen = 0f;
            _shipOxygenDetector = null;
            _shipLoaded = false;
        };
    }

    private void Update()
    {
        if (!_shipLoaded || LoadManager.GetCurrentScene() != OWScene.SolarSystem 
            || ModCompatibility.GetModSetting("Stonesword.ResourceManagement", "Enable Oxygen Refill")) return;

        if (!oxygenDepleted && _shipResources.GetOxygen() <= 0)
        {
            oxygenDepleted = true;
            if (PlayerState.IsInsideShip())
            {
                string text = "OXYGEN SOURCE DEPLETED";
                NotificationData notificationData = new NotificationData(NotificationTarget.Ship, text, 5f, true);
                NotificationManager.SharedInstance.PostNotification(notificationData, false);

                if (PlayerState.AtFlightConsole() && PlayerState.IsWearingSuit())
                {
                    Locator.GetPlayerSuit().PutOnHelmet();
                }
                _shipOxygen.OnEffectVolumeExit(Locator.GetPlayerDetector());
            }
        }
        else if (oxygenDepleted && _shipResources.GetOxygen() > 0)
        {
            oxygenDepleted = false;
            if (PlayerState.IsInsideShip())
            {
                string text = "REFILLING OXYGEN TANK";
                NotificationData notificationData = new NotificationData(NotificationTarget.Ship, text, 3f, true);
                NotificationManager.SharedInstance.PostNotification(notificationData, false);

                refillingOxygen = true;
                _shipOxygen.OnEffectVolumeEnter(Locator.GetPlayerDetector());
            }
        }
    }

    private void LateUpdate()
    {
        if (!_shipLoaded || LoadManager.GetCurrentScene() != OWScene.SolarSystem) return;

        if (!_playerResources._refillingOxygen && refillingOxygen)
        {
            refillingOxygen = false;
        }
    }

    private void UpdateProperties()
    {
        HeadlightsDisabled = (bool)Settings.disableHeadlights.GetValue();
        LandingCameraDisabled = (bool)Settings.disableLandingCamera.GetValue();
        OxygenDrainMultiplier = (float)Settings.oxygenDrainMultiplier.GetValue();
        FuelDrainMultiplier = (float)Settings.fuelDrainMultiplier.GetValue();
        DamageMultiplier = (float)Settings.shipDamageMultiplier.GetValue();
        DamageSpeedMultiplier = (float)Settings.shipDamageSpeedMultiplier.GetValue();
        ShipOxygenRefill = (bool)Settings.shipOxygenRefill.GetValue();
        OxygenDisabled = (bool)Settings.disableShipOxygen.GetValue();
        ShipRepairDisabled = (bool)Settings.disableShipRepair.GetValue();
        GravityLandingGearEnabled = (bool)Settings.enableGravityLandingGear.GetValue();
        AirAutoRollDisabled = (bool)Settings.disableAirAutoRoll.GetValue();
        WaterAutoRollDisabled = (bool)Settings.disableWaterAutoRoll.GetValue();
        ThrustModulatorEnabled = (bool)Settings.enableThrustModulator.GetValue();
        ThrustModulatorLevel = 5;
        ReferenceFrameDisabled = (bool)Settings.disableReferenceFrame.GetValue();
        MapMarkersDisabled = (bool)Settings.disableMapMarkers.GetValue();
        FuelTransferMultiplier = (float)Settings.fuelTransferMultiplier.GetValue();
        OxygenRefillMultiplier = (float)Settings.oxygenRefillMultiplier.GetValue();
        TemperatureDamageMultiplier = (float)Settings.temperatureDamageMultiplier.GetValue();
        TemperatureResistanceMultiplier = (float)Settings.temperatureResistanceMultiplier.GetValue();
        AutoHatchEnabled = (bool)Settings.enableAutoHatch.GetValue();
    }

    private IEnumerator InitializeShip()
    {
        yield return new WaitUntil(() => Locator._shipBody != null);

        GameObject buttonConsole = LoadPrefab("Assets/ShipEnhancements/ButtonConsole.prefab");
        AssetBundleUtilities.ReplaceShaders(buttonConsole);
        Instantiate(buttonConsole, Locator.GetShipBody().transform.Find("Module_Cockpit"));

        Material material1 = (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_VillageCabin_Recolored_mat.mat");
        Material material2 = (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_VillageMetal_Recolored_mat.mat");
        Material material3 = (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_VillagePlanks_Recolored_mat.mat");
        List<Material> materials = [.. GameObject.Find("Pointlight_HEA_ShipCockpit").GetComponent<LightmapController>()._materials];
        materials.Add(material1);
        materials.Add(material2);
        materials.Add(material3);
        GameObject.Find("Pointlight_HEA_ShipCockpit").GetComponent<LightmapController>()._materials = [.. materials];

        _shipResources = Locator.GetShipBody().GetComponent<ShipResources>();
        _shipOxygen = Locator.GetShipBody().GetComponentInChildren<OxygenVolume>();
        _playerResources = Locator.GetPlayerBody().GetComponent<PlayerResources>();

        _shipLoaded = true;
        UpdateSuitOxygen();

        if ((bool)Settings.disableGravityCrystal.GetValue())
        {
            DisableGravityCrystal();
        }
        if ((bool)Settings.disableEjectButton.GetValue())
        {
            Locator.GetShipBody().GetComponentInChildren<ShipEjectionSystem>().GetComponent<InteractReceiver>().DisableInteraction();
        }
        if ((bool)Settings.disableHeadlights.GetValue())
        {
            DisableHeadlights();
        }
        if ((bool)Settings.disableLandingCamera.GetValue())
        {
            DisableLandingCamera();
        }
        if ((bool)Settings.disableShipLights.GetValue())
        {
            foreach (ElectricalSystem system in Locator.GetShipBody().GetComponentsInChildren<ElectricalSystem>())
            {
                foreach (ElectricalComponent component in system._connectedComponents)
                {
                    if (component.gameObject.name.Contains("Point") && component.TryGetComponent(out ShipLight light))
                    {
                        light.SetOn(false);
                        light.GetComponent<Light>().enabled = false;
                    }
                }
            }
            foreach (PulsingLight beacon in Locator.GetShipBody().transform.Find("Module_Cabin/Lights_Cabin/ShipBeacon_Proxy").GetComponentsInChildren<PulsingLight>())
            {
                PulsingLight.s_matPropBlock.SetColor(PulsingLight.s_propID_EmissionColor, beacon._initEmissionColor * 0f);
                beacon._emissiveRenderer.SetPropertyBlock(PulsingLight.s_matPropBlock);
                beacon.gameObject.SetActive(false);
            }
        }
        if ((bool)Settings.disableShipOxygen.GetValue())
        {
            _shipResources.SetOxygen(0f);
            oxygenDepleted = true;
        }
        if ((bool)Settings.shipOxygenRefill.GetValue())
        {
            _shipOxygenDetector = Locator.GetShipDetector().gameObject.AddComponent<OxygenDetector>();
        }
        if (Settings.temperatureZonesAmount.GetValue().ToString() == "Sun")
        {
            GameObject sun = GameObject.Find("Sun_Body");
            if (sun != null)
            {
                GameObject sunTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Sun.prefab");
                Instantiate(sunTempZone, sun.transform.Find("Sector_SUN"));
            }
        }
        else if (Settings.temperatureZonesAmount.GetValue().ToString() == "All")
        {
            AddTemperatureZones();
        }
        if ((bool)Settings.enableTemperatureDamage.GetValue())
        {
            Locator.GetShipDetector().gameObject.AddComponent<ShipTemperatureDetector>();
            Locator.GetShipBody().GetComponentInChildren<ShipFuelGauge>().gameObject.AddComponent<ShipTemperatureGauge>();
        }
        if ((bool)Settings.enableShipFuelTransfer.GetValue())
        {
            GameObject transferVolume = LoadPrefab("Assets/ShipEnhancements/FuelTransferVolume.prefab");
            Instantiate(transferVolume, Locator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>().transform);
        }
        if ((bool)Settings.enableJetpackRefuelDrain.GetValue())
        {
            Locator.GetShipBody().GetComponentInChildren<PlayerRecoveryPoint>().gameObject.AddComponent<ShipRecoveryPoint>();
        }
        if ((float)Settings.gravityMultiplier.GetValue() != 1f && !(bool)Settings.disableGravityCrystal.GetValue())
        {
            ShipDirectionalForceVolume shipGravity = Locator.GetShipBody().GetComponentInChildren<ShipDirectionalForceVolume>();
            shipGravity._fieldMagnitude *= (float)Settings.gravityMultiplier.GetValue();
        }
        if ((bool)Settings.enableAutoHatch.GetValue())
        {
            GlobalMessenger.AddListener("EnterShip", OnEnterShip);
            GlobalMessenger.AddListener("ExitShip", OnExitShip);
            GameObject autoHatchController = LoadPrefab("Assets/ShipEnhancements/ExteriorHatchControls.prefab");
            Instantiate(autoHatchController, Locator.GetShipBody().GetComponentInChildren<HatchController>().transform.parent);
        }
    }

    private static void AddTemperatureZones()
    {
        GameObject sun = GameObject.Find("Sun_Body");
        if (sun != null)
        {
            GameObject sunTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Sun.prefab");
            Instantiate(sunTempZone, sun.transform.Find("Sector_SUN"));
        }

        GameObject vm = GameObject.Find("VolcanicMoon_Body");
        if (vm != null)
        {
            GameObject vmTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_VolcanicMoon.prefab");
            Instantiate(vmTempZone, vm.transform.Find("Sector_VM"));
        }

        GameObject db = GameObject.Find("DarkBramble_Body");
        if (db != null)
        {
            GameObject dbTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_DarkBramble.prefab");
            Instantiate(dbTempZone, db.transform.Find("Sector_DB"));
        }

        GameObject comet = GameObject.Find("Comet_Body");
        if (comet != null)
        {
            GameObject cometTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_InterloperAtmosphere.prefab");
            Instantiate(cometTempZone1, comet.transform.Find("Sector_CO"));
            GameObject cometTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_InterloperDarkSide.prefab");
            Instantiate(cometTempZone2, comet.transform.Find("Sector_CO"));
        }

        GameObject gd = GameObject.Find("GiantsDeep_Body");
        if (gd != null)
        {
            GameObject gdTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_GiantsDeepOcean.prefab");
            Instantiate(gdTempZone1, gd.transform.Find("Sector_GD/Sector_GDInterior"));
            GameObject gdTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_GiantsDeepCore.prefab");
            Instantiate(gdTempZone2, gd.transform.Find("Sector_GD/Sector_GDInterior"));
        }

        GameObject bh = GameObject.Find("BrittleHollow_Body");
        if (bh != null)
        {
            GameObject bhTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_BrittleHollowNorth.prefab");
            Instantiate(bhTempZone1, bh.transform.Find("Sector_BH"));
            GameObject bhTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_BrittleHollowSouth.prefab");
            Instantiate(bhTempZone2, bh.transform.Find("Sector_BH"));
        }

        GameObject th = GameObject.Find("TimberHearth_Body");
        if (th != null)
        {
            GameObject thTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_TimberHearthGeyser.prefab");
            Instantiate(thTempZone1, th.transform.Find("Sector_TH"));
            GameObject thTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_TimberHearthCore.prefab");
            Instantiate(thTempZone2, th.transform.Find("Sector_TH"));
        }

        GameObject ct = GameObject.Find("CaveTwin_Body");
        if (ct != null)
        {
            GameObject ctTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_CaveTwinHot.prefab");
            Instantiate(ctTempZone1, ct.transform.Find("Sector_CaveTwin"));
            GameObject ctTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_CaveTwinCold.prefab");
            Instantiate(ctTempZone2, ct.transform.Find("Sector_CaveTwin"));
        }
    }

    private static void DisableHeadlights()
    {
        ShipHeadlightComponent headlightComponent = Locator.GetShipBody().GetComponentInChildren<ShipHeadlightComponent>();
        headlightComponent._repairReceiver.repairDistance = 0f;
        headlightComponent._damaged = true;
        headlightComponent._repairFraction = 0f;
        headlightComponent.OnComponentDamaged();
    }

    private void DisableGravityCrystal()
    {
        ShipGravityComponent gravityComponent = Locator.GetShipBody().GetComponentInChildren<ShipGravityComponent>();
        gravityComponent._repairReceiver.repairDistance = 0f;
        gravityComponent._damaged = true;
        gravityComponent._repairFraction = 0f;
        gravityComponent.OnComponentDamaged();
        gravityComponent._damageEffect.SetEffectBlend(1f - gravityComponent._repairFraction);
        gravityComponent.GetComponentInChildren<Light>().enabled = false;
        gravityComponent.GetComponentInChildren<ParticleSystem>().Stop();
    }

    private void DisableLandingCamera()
    {
        ShipCameraComponent cameraComponent = Locator.GetShipBody().GetComponentInChildren<ShipCameraComponent>();
        cameraComponent._repairReceiver.repairDistance = 0f;
        cameraComponent._damaged = true;
        cameraComponent._repairFraction = 0f;
        cameraComponent._landingCamera.SetDamaged(true);
    }

    private void OnPlayerSuitUp()
    {
        if (Locator.GetPlayerBody().GetComponent<PlayerResources>()._currentOxygen < _lastSuitOxygen)
        {
            Locator.GetPlayerBody().GetComponent<PlayerResources>()._currentOxygen = _lastSuitOxygen;
        }
    }

    private void OnPlayerRemoveSuit()
    {
        UpdateSuitOxygen();
    }

    public void UpdateSuitOxygen()
    {
        _lastSuitOxygen = Locator.GetPlayerBody().GetComponent<PlayerResources>()._currentOxygen;
    }

    public bool IsShipInOxygen()
    {
        return _shipOxygenDetector != null && _shipOxygenDetector.GetDetectOxygen();
    }

    public void SetGravityLandingGearEnabled(bool enabled)
    {
        if (OnGravityLandingGearSwitch != null)
        {
            OnGravityLandingGearSwitch(enabled);
        }
    }

    public void SetThrustModulatorLevel(int level)
    {
        ThrustModulatorLevel = level;
    }

    private void OnEnterShip()
    {
        HatchController hatchController = Locator.GetShipBody().GetComponentInChildren<HatchController>();
        hatchController._interactVolume.EnableInteraction();
        hatchController.GetComponent<SphereShape>().radius = 1f;
        hatchController.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        hatchController.transform.parent.GetComponentInChildren<AutoHatchController>().DisableInteraction();
    }

    private void OnExitShip()
    {
        HatchController hatchController = Locator.GetShipBody().GetComponentInChildren<HatchController>();
        hatchController._interactVolume.DisableInteraction();
        hatchController.GetComponent<SphereShape>().radius = 3.5f;
        hatchController.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
    }

    public static void WriteDebugMessage(object msg, bool warning = false, bool error = false)
    {
        if (warning)
        {
            Instance.ModHelper.Console.WriteLine(msg.ToString(), MessageType.Warning);
        }
        else if (error)
        {
            Instance.ModHelper.Console.WriteLine(msg.ToString(), MessageType.Error);
        }
        else
        {
            Instance.ModHelper.Console.WriteLine(msg.ToString());
        }
    }

    public static GameObject LoadPrefab(string path)
    {
        return (GameObject)Instance._shipEnhancementsBundle.LoadAsset(path);
    }

    public static AudioClip LoadAudio(string path)
    {
        return (AudioClip)Instance._shipEnhancementsBundle.LoadAsset(path);
    }

    public override void Configure(IModConfig config)
    {
        if (!SettingsPresets.Initialized()) return;

        SettingsPresets.PresetName newPreset = SettingsPresets.GetPresetFromConfig(config.GetSettingsValue<string>("preset"));
        var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];
        if (newPreset != _currentPreset || _currentPreset == (SettingsPresets.PresetName)(-1))
        {
            _currentPreset = newPreset;
            SettingsPresets.ApplyPreset(newPreset, config);
            config.SetSettingsValue("preset", _currentPreset.GetName());
            foreach (Settings setting in allSettings)
            {
                setting.SetValue(config.GetSettingsValue<object>(setting.GetName()));
            }
        }
        else
        {
            var isCustom = false;
            foreach (Settings setting in allSettings)
            {
                setting.SetValue(config.GetSettingsValue<object>(setting.GetName()));
                if (_currentPreset != SettingsPresets.PresetName.Custom)
                {
                    isCustom = isCustom || !_currentPreset.GetPresetSetting(setting.GetName()).Equals(setting.GetValue());
                    /*if (!_currentPreset.GetPresetSetting(setting.GetName()).Equals(setting.GetValue()))
                    {
                        WriteDebugMessage($"{setting.GetValue()} ({setting.GetValue().GetType()}) : {_currentPreset.GetPresetSetting(setting.GetName())} ({_currentPreset.GetPresetSetting(setting.GetName()).GetType()})");
                    }*/
                }
            }
            if (isCustom)
            {
                //WriteDebugMessage("custom");
                _currentPreset = SettingsPresets.PresetName.Custom;
                config.SetSettingsValue("preset", SettingsPresets.PresetName.Custom.GetName());
                SettingsPresets.ApplyPreset(SettingsPresets.PresetName.Custom, config);
            }
        }

        /*_gravityCrystalDisabled = config.GetSettingsValue<bool>("disableGravityCrystal");
        _ejectButtonDisabled = config.GetSettingsValue<bool>("disableEjectButton");
        _headlightsDisabled = config.GetSettingsValue<bool>("disableHeadlights");
        _landingCameraDisabled = config.GetSettingsValue<bool>("disableLandingCamera");
        _shipLightsDisabled = config.GetSettingsValue<bool>("disableShipLights");
        _oxygenDisabled = config.GetSettingsValue<bool>("disableShipOxygen");
        _oxygenDrainMultiplier = config.GetSettingsValue<float>("oxygenDrainMultiplier");
        _fuelDrainMultiplier = config.GetSettingsValue<float>("fuelDrainMultiplier");
        _damageMultiplier = config.GetSettingsValue<float>("shipDamageMultiplier");
        _damageSpeedMultiplier = config.GetSettingsValue<float>("shipDamageSpeedMultiplier");
        _shipOxygenRefill = config.GetSettingsValue<bool>("shipOxygenRefill");
        _shipRepairDisabled = config.GetSettingsValue<bool>("disableShipRepair");
        _gravityLandingGearEnabled = config.GetSettingsValue<bool>("enableGravityLandingGear");
        _airAutoRollDisabled = config.GetSettingsValue<bool>("disableAirAutoRoll");
        _waterAutoRollDisabled = config.GetSettingsValue<bool>("disableWaterAutoRoll");
        _thrustModulatorEnabled = config.GetSettingsValue<bool>("enableThrustModulator");
        _temperatureZonesAmount = config.GetSettingsValue<string>("temperatureZonesAmount");
        _temperatureDamageEnabled = config.GetSettingsValue<bool>("enableTemperatureDamage");
        _shipFuelTransferEnabled = config.GetSettingsValue<bool>("enableShipFuelTransfer");
        _refuelDrainsShip = config.GetSettingsValue<bool>("enableJetpackRefuelDrain");*/
    }

    public override object GetApi()
    {
        return new ShipEnhancementsAPI();
    }
}
