using OWML.Common;
using OWML.ModHelper;
using System.Collections;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using OWML.ModHelper.Menus;

namespace ShipEnhancements;

public class ShipEnhancements : ModBehaviour
{
    public delegate void SwitchEvent(bool enabled);
    public event SwitchEvent OnGravityLandingGearSwitch;

    public static ShipEnhancements Instance;
    public bool oxygenDepleted;
    public bool refillingOxygen;
    public bool fuelDepleted;

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
    public float OxygenTankDrainMultiplier { get; private set; }
    public float FuelTankDrainMultiplier { get; private set; }
    public bool HullTemperatureDamage { get; private set; }
    public bool ComponentTemperatureDamage { get; private set; }

    private SettingsPresets.PresetName _currentPreset = (SettingsPresets.PresetName)(-1);

    private AssetBundle _shipEnhancementsBundle;
    private float _lastSuitOxygen;
    private float _lastShipOxygen;
    private bool _startOxygenRefill = false;
    private bool _shipLoaded = false;
    private OxygenDetector _shipOxygenDetector;
    private ShipResources _shipResources;
    private OxygenVolume _shipOxygen;
    private PlayerResources _playerResources;
    private NotificationData _oxygenDepletedNotification = new NotificationData(NotificationTarget.Ship, "SHIP OXYGEN DEPLETED", 5f, true);

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
        hullTemperatureDamage,
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
        oxygenTankDrainMultiplier,
        fuelTankDrainMultiplier,
        componentTemperatureDamage,
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
            fuelDepleted = false;
            _startOxygenRefill = false;

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
        if (!_shipLoaded || LoadManager.GetCurrentScene() != OWScene.SolarSystem) return;

        if (!oxygenDepleted && _shipResources.GetOxygen() <= 0 && !(ShipOxygenRefill && IsShipInOxygen()))
        {
            oxygenDepleted = true;

            NotificationManager.SharedInstance.PostNotification(_oxygenDepletedNotification, true);

            if (PlayerState.IsInsideShip())
            {
                if (PlayerState.IsWearingSuit() && !Locator.GetPlayerSuit().IsWearingHelmet())
                {
                    Locator.GetPlayerSuit().PutOnHelmet();
                }
                _shipOxygen.OnEffectVolumeExit(Locator.GetPlayerDetector());
            }

            ShipOxygenTankComponent oxygenTank = Locator.GetShipBody().GetComponentInChildren<ShipOxygenTankComponent>();
            if (oxygenTank.isDamaged)
            {
                oxygenTank._damageEffect._particleSystem.Stop();
                oxygenTank._damageEffect._particleAudioSource.Stop();
            }
        }
        else if (oxygenDepleted && (_shipResources.GetOxygen() > 0 || (ShipOxygenRefill && IsShipInOxygen())))
        {
            oxygenDepleted = false;
            refillingOxygen = true;

            NotificationManager.SharedInstance.UnpinNotification(_oxygenDepletedNotification);

            if (PlayerState.IsInsideShip())
            {
                _shipOxygen.OnEffectVolumeEnter(Locator.GetPlayerDetector());
            }

            ShipOxygenTankComponent oxygenTank = Locator.GetShipBody().GetComponentInChildren<ShipOxygenTankComponent>();
            if (oxygenTank.isDamaged)
            {
                oxygenTank._damageEffect._particleSystem.Play();
                oxygenTank._damageEffect._particleAudioSource.Play();
            }
        }

        if (ShipOxygenRefill)
        {
            if (!_startOxygenRefill && _shipResources._currentOxygen > _lastShipOxygen)
            {
                _startOxygenRefill = true;
                string text = "REFILLING OXYGEN TANK";
                NotificationData notificationData = new NotificationData(NotificationTarget.Ship, text, 3f, true);
                NotificationManager.SharedInstance.PostNotification(notificationData, false);
            }
            else if (_startOxygenRefill && _shipResources._currentOxygen < _lastShipOxygen && _shipResources._currentOxygen / _shipResources._maxOxygen < 0.99f)
            {
                _startOxygenRefill = false;
            }

            _lastShipOxygen = _shipResources._currentOxygen;
        }

        if (!fuelDepleted && _shipResources._currentFuel <= 0f)
        {
            fuelDepleted = true;
            ShipFuelTankComponent fuelTank = Locator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>();
            if (fuelTank.isDamaged)
            {
                fuelTank._damageEffect._particleSystem.Stop();
                fuelTank._damageEffect._particleAudioSource.Stop();
            }
        }
        else if (fuelDepleted && _shipResources._currentFuel > 0f)
        {
            fuelDepleted = false;
            ShipFuelTankComponent fuelTank = Locator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>();
            if (fuelTank.isDamaged)
            {
                fuelTank._damageEffect._particleSystem.Play();
                fuelTank._damageEffect._particleAudioSource.Play();
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
        OxygenTankDrainMultiplier = (float)Settings.oxygenTankDrainMultiplier.GetValue();
        FuelTankDrainMultiplier = (float)Settings.fuelTankDrainMultiplier.GetValue();
        HullTemperatureDamage = (bool)Settings.hullTemperatureDamage.GetValue();
        ComponentTemperatureDamage = (bool)Settings.componentTemperatureDamage.GetValue();
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
        _lastShipOxygen = _shipResources._currentOxygen;

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
        if ((bool)Settings.hullTemperatureDamage.GetValue() || (bool)Settings.componentTemperatureDamage.GetValue())
        {
            Locator.GetShipDetector().gameObject.AddComponent<ShipTemperatureDetector>();
            Locator.GetShipBody().GetComponentInChildren<ShipFuelGauge>().gameObject.AddComponent<ShipTemperatureGauge>();
            GameObject hullTempDial = LoadPrefab("Assets/ShipEnhancements/ShipTempDial.prefab");
            Instantiate(hullTempDial, Locator.GetShipTransform().Find("Module_Cockpit"));
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
            GameObject supernovaTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Supernova.prefab");
            Instantiate(supernovaTempZone, sun.GetComponentInChildren<SupernovaEffectController>().transform);
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
        var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];
        if (!SettingsPresets.Initialized())
        {
            return;
        }

        SettingsPresets.PresetName newPreset = SettingsPresets.GetPresetFromConfig(config.GetSettingsValue<string>("preset"));
        if (newPreset != _currentPreset || _currentPreset == (SettingsPresets.PresetName)(-1))
        {
            _currentPreset = newPreset;
            config.SetSettingsValue("preset", _currentPreset.GetName());
            foreach (Settings setting in allSettings)
            {
                setting.SetValue(config.GetSettingsValue<object>(setting.GetName()));
            }
            SettingsPresets.ApplyPreset(newPreset, config);
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
                }
            }
            if (isCustom)
            {
                _currentPreset = SettingsPresets.PresetName.Custom;
                config.SetSettingsValue("preset", _currentPreset.GetName());
                foreach (Settings setting in allSettings)
                {
                    config.SetSettingsValue(setting.GetName(), setting.GetValue());
                }
                WriteDebugMessage(config.GetSettingsValue<string>("preset"));
                ModHelper.Menus.ModsMenu.GetModMenu(this).UpdateUIValues();
                //SettingsPresets.ApplyPreset(SettingsPresets.PresetName.Custom, config);
            }
        }
    }

    public override object GetApi()
    {
        return new ShipEnhancementsAPI();
    }
}
