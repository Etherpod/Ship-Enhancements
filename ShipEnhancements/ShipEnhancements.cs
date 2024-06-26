using System;
using OWML.Common;
using OWML.ModHelper;
using System.Collections;
using UnityEngine;
using System.IO;
using System.Security.Policy;
using UnityEngine.PostProcessing;

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
    
    private bool _gravityCrystalDisabled;
    private bool _ejectButtonDisabled;
    private bool _headlightsDisabled;
    private bool _landingCameraDisabled;
    private bool _shipLightsDisabled;
    private bool _oxygenDisabled;
    private float _oxygenDrainMultiplier;
    private float _fuelDrainMultiplier;
    private float _damageMultiplier;
    private float _damageSpeedMultiplier;
    private bool _shipOxygenRefill;
    private bool _shipRepairDisabled;
    private bool _gravityLandingGearEnabled;
    private bool _airAutoRollDisabled;
    private bool _waterAutoRollDisabled;
    private bool _thrustModulatorEnabled;
    private string _temperatureZonesAmount;
    private bool _temperatureDamageEnabled;
    private bool _shipFuelTransferEnabled;
    private bool _refuelDrainsShip;

    private AssetBundle _shipEnhancementsBundle;
    private float _lastSuitOxygen;
    private bool _shipLoaded = false;
    private OxygenDetector _shipOxygenDetector;
    

    private void Awake()
    {
        Instance = this;
        HarmonyLib.Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly());
    }

    private void Start()
    {
        _shipEnhancementsBundle = AssetBundle.LoadFromFile(Path.Combine(ModHelper.Manifest.ModFolderPath, "assets/shipenhancements"));

        _gravityCrystalDisabled = ModHelper.Config.GetSettingsValue<bool>("disableGravityCrystal");
        _ejectButtonDisabled = ModHelper.Config.GetSettingsValue<bool>("disableEjectButton");
        _headlightsDisabled = ModHelper.Config.GetSettingsValue<bool>("disableHeadlights");
        _landingCameraDisabled = ModHelper.Config.GetSettingsValue<bool>("disableLandingCamera");
        _shipLightsDisabled = ModHelper.Config.GetSettingsValue<bool>("disableShipLights");
        _oxygenDrainMultiplier = ModHelper.Config.GetSettingsValue<float>("oxygenDrainMultiplier");
        _fuelDrainMultiplier = ModHelper.Config.GetSettingsValue<float>("fuelDrainMultiplier");
        _damageMultiplier = ModHelper.Config.GetSettingsValue<float>("shipDamageMultiplier");
        _damageSpeedMultiplier = ModHelper.Config.GetSettingsValue<float>("shipDamageSpeedMultiplier");
        _shipOxygenRefill = ModHelper.Config.GetSettingsValue<bool>("shipOxygenRefill");
        _shipRepairDisabled = ModHelper.Config.GetSettingsValue<bool>("disableShipRepair");
        _gravityLandingGearEnabled = ModHelper.Config.GetSettingsValue<bool>("enableGravityLandingGear");
        _airAutoRollDisabled = ModHelper.Config.GetSettingsValue<bool>("disableAirAutoRoll");
        _waterAutoRollDisabled = ModHelper.Config.GetSettingsValue<bool>("disableWaterAutoRoll");
        _thrustModulatorEnabled = ModHelper.Config.GetSettingsValue<bool>("enableThrustModulator");
        _temperatureZonesAmount = ModHelper.Config.GetSettingsValue<string>("temperatureZonesAmount");
        _temperatureDamageEnabled = ModHelper.Config.GetSettingsValue<bool>("enableTemperatureDamage");
        _shipFuelTransferEnabled = ModHelper.Config.GetSettingsValue<bool>("enableShipFuelTransfer");
        _refuelDrainsShip = ModHelper.Config.GetSettingsValue<bool>("enableJetpackRefuelDrain");

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
            _lastSuitOxygen = 0f;
            _shipOxygenDetector = null;
            _shipLoaded = false;
        };
    }

    private void Update()
    {
        if (!_shipLoaded || LoadManager.GetCurrentScene() != OWScene.SolarSystem) return;

        OxygenVolume shipOxygen = Locator.GetShipBody().GetComponentInChildren<OxygenVolume>();

        if (!oxygenDepleted && Locator.GetShipBody().GetComponent<ShipResources>().GetOxygen() <= 0)
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
                shipOxygen.OnEffectVolumeExit(Locator.GetPlayerDetector());
            }
        }
        else if (oxygenDepleted && Locator.GetShipBody().GetComponent<ShipResources>().GetOxygen() > 0)
        {
            oxygenDepleted = false;
            if (PlayerState.IsInsideShip())
            {
                string text = "REFILLING OXYGEN TANK";
                NotificationData notificationData = new NotificationData(NotificationTarget.Ship, text, 3f, true);
                NotificationManager.SharedInstance.PostNotification(notificationData, false);

                refillingOxygen = true;
                shipOxygen.OnEffectVolumeEnter(Locator.GetPlayerDetector());
            }
        }
    }

    private void LateUpdate()
    {
        if (!_shipLoaded || LoadManager.GetCurrentScene() != OWScene.SolarSystem) return;

        if (!Locator.GetPlayerBody().GetComponent<PlayerResources>()._refillingOxygen && refillingOxygen)
        {
            refillingOxygen = false;
        }
    }

    private void UpdateProperties()
    {
        HeadlightsDisabled = _headlightsDisabled;
        LandingCameraDisabled = _landingCameraDisabled;
        OxygenDrainMultiplier = _oxygenDrainMultiplier;
        FuelDrainMultiplier = _fuelDrainMultiplier;
        DamageMultiplier = _damageMultiplier;
        DamageSpeedMultiplier = _damageSpeedMultiplier;
        ShipOxygenRefill = _shipOxygenRefill;
        OxygenDisabled = _oxygenDisabled;
        ShipRepairDisabled = _shipRepairDisabled;
        GravityLandingGearEnabled = _gravityLandingGearEnabled;
        AirAutoRollDisabled = _airAutoRollDisabled;
        WaterAutoRollDisabled = _waterAutoRollDisabled;
        ThrustModulatorEnabled = _thrustModulatorEnabled;
        ThrustModulatorLevel = 5;
    }

    private IEnumerator InitializeShip()
    {
        yield return new WaitUntil(() => Locator._shipBody != null);

        GameObject buttonConsole = LoadPrefab("Assets/ShipEnhancements/ButtonConsole.prefab");
        AssetBundleUtilities.ReplaceShaders(buttonConsole);
        Instantiate(buttonConsole, Locator.GetShipBody().transform.Find("Module_Cockpit"));

        _shipLoaded = true;
        UpdateSuitOxygen();

        if (_gravityCrystalDisabled)
        {
            DisableGravityCrystal();
        }
        if (_ejectButtonDisabled)
        {
            Locator.GetShipBody().GetComponentInChildren<ShipEjectionSystem>().GetComponent<InteractReceiver>().DisableInteraction();
        }
        if (_headlightsDisabled)
        {
            DisableHeadlights();
        }
        if (_landingCameraDisabled)
        {
            DisableLandingCamera();
        }
        if (_shipLightsDisabled)
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
        if (_oxygenDisabled)
        {
            Locator.GetShipBody().GetComponent<ShipResources>().SetOxygen(0f);
            oxygenDepleted = true;
        }
        if (_shipOxygenRefill)
        {
            _shipOxygenDetector = Locator.GetShipDetector().gameObject.AddComponent<OxygenDetector>();
        }
        if (_temperatureZonesAmount == "Sun")
        {
            GameObject sun = GameObject.Find("Sun_Body");
            if (sun != null)
            {
                GameObject sunTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Sun.prefab");
                Instantiate(sunTempZone, sun.transform.Find("Sector_SUN"));
            }
        }
        else if (_temperatureZonesAmount == "All")
        {
            AddTemperatureZones();
        }
        if (_temperatureDamageEnabled)
        {
            Locator.GetShipDetector().gameObject.AddComponent<ShipTemperatureDetector>();
            Locator.GetShipBody().GetComponentInChildren<ShipFuelGauge>().gameObject.AddComponent<ShipTemperatureGauge>();
        }
        if (_shipFuelTransferEnabled)
        {
            GameObject transferVolume = LoadPrefab("Assets/ShipEnhancements/FuelTransferVolume.prefab");
            Instantiate(transferVolume, Locator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>().transform);
        }
        if (_refuelDrainsShip)
        {
            Locator.GetShipBody().GetComponentInChildren<PlayerRecoveryPoint>().gameObject.AddComponent<ShipRecoveryPoint>();
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

    public static void WriteDebugMessage(object msg)
    {
        Instance.ModHelper.Console.WriteLine(msg.ToString());
    }

    public static GameObject LoadPrefab(string path)
    {
        return (GameObject)Instance._shipEnhancementsBundle.LoadAsset(path);
    }

    public override void Configure(IModConfig config)
    {
        _gravityCrystalDisabled = ModHelper.Config.GetSettingsValue<bool>("disableGravityCrystal");
        _ejectButtonDisabled = ModHelper.Config.GetSettingsValue<bool>("disableEjectButton");
        _headlightsDisabled = ModHelper.Config.GetSettingsValue<bool>("disableHeadlights");
        _landingCameraDisabled = ModHelper.Config.GetSettingsValue<bool>("disableLandingCamera");
        _shipLightsDisabled = ModHelper.Config.GetSettingsValue<bool>("disableShipLights");
        _oxygenDisabled = ModHelper.Config.GetSettingsValue<bool>("disableShipOxygen");
        _oxygenDrainMultiplier = ModHelper.Config.GetSettingsValue<float>("oxygenDrainMultiplier");
        _fuelDrainMultiplier = ModHelper.Config.GetSettingsValue<float>("fuelDrainMultiplier");
        _damageMultiplier = ModHelper.Config.GetSettingsValue<float>("shipDamageMultiplier");
        _damageSpeedMultiplier = ModHelper.Config.GetSettingsValue<float>("shipDamageSpeedMultiplier");
        _shipOxygenRefill = ModHelper.Config.GetSettingsValue<bool>("shipOxygenRefill");
        _shipRepairDisabled = ModHelper.Config.GetSettingsValue<bool>("disableShipRepair");
        _gravityLandingGearEnabled = ModHelper.Config.GetSettingsValue<bool>("enableGravityLandingGear");
        _airAutoRollDisabled = ModHelper.Config.GetSettingsValue<bool>("disableAirAutoRoll");
        _waterAutoRollDisabled = ModHelper.Config.GetSettingsValue<bool>("disableWaterAutoRoll");
        _thrustModulatorEnabled = ModHelper.Config.GetSettingsValue<bool>("enableThrustModulator");
        _temperatureZonesAmount = ModHelper.Config.GetSettingsValue<string>("temperatureZonesAmount");
        _temperatureDamageEnabled = ModHelper.Config.GetSettingsValue<bool>("enableTemperatureDamage");
        _shipFuelTransferEnabled = ModHelper.Config.GetSettingsValue<bool>("enableShipFuelTransfer");
        _refuelDrainsShip = ModHelper.Config.GetSettingsValue<bool>("enableJetpackRefuelDrain");
    }
}
