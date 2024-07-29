using OWML.Common;
using OWML.ModHelper;
using System.Collections;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using OWML.Utils;

namespace ShipEnhancements;

public class ShipEnhancements : ModBehaviour
{
    public delegate void SwitchEvent(bool enabled);
    public event SwitchEvent OnGravityLandingGearSwitch;

    public static ShipEnhancements Instance;
    public bool oxygenDepleted;
    public bool refillingOxygen;
    public bool fuelDepleted;
    public bool angularDragEnabled;
    public float levelOneSpinSpeed = 8f;
    public float levelTwoSpinSpeed = 16f;
    public float maxSpinSpeed = 24f;
    public bool probeDestroyed;

    public UITextType probeLauncherName { get; private set; }
    public ItemType portableCampfireType { get; private set; }
    public int thrustModulatorLevel { get; private set; }

    private SettingsPresets.PresetName _currentPreset = (SettingsPresets.PresetName)(-1);

    private AssetBundle _shipEnhancementsBundle;
    private float _lastSuitOxygen;
    private float _lastShipOxygen;
    private float _lastShipFuel;
    private bool _shipLoaded = false;
    private bool _shipDestroyed;

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
        angularDragMultiplier,
        disableSpaceAngularDrag,
        disableRotationSpeedLimit,
        gravityDirection,
        disableScoutLauncher,
        enableScoutLauncherComponent,
        enableManualScoutRecall,
        enableShipItemPlacement,
        addPortableCampfire,
        keepHelmetOn,
        showWarningNotifications,
        shipExplosionMultiplier,
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

        probeLauncherName = EnumUtils.Create<UITextType>("ScoutLauncher");
        portableCampfireType = EnumUtils.Create<ItemType>("PortableCampfire");

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

            GlobalMessenger.AddListener("SuitUp", OnPlayerSuitUp);
            GlobalMessenger.AddListener("RemoveSuit", OnPlayerRemoveSuit);
            GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
            oxygenDepleted = false;
            fuelDepleted = false;
            angularDragEnabled = false;
            probeDestroyed = false;
            _shipDestroyed = false;

            StartCoroutine(InitializeShip());
        };

        LoadManager.OnStartSceneLoad += (scene, loadScene) =>
        {
            if (scene == OWScene.TitleScreen) UpdateProperties();
            if (scene != OWScene.SolarSystem) return;

            UpdateProperties();
            GlobalMessenger.RemoveListener("SuitUp", OnPlayerSuitUp);
            GlobalMessenger.RemoveListener("RemoveSuit", OnPlayerRemoveSuit);
            GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
            if ((bool)Settings.disableSpaceAngularDrag.GetValue())
            {
                Locator.GetShipDetector().GetComponent<ShipFluidDetector>().OnEnterFluid -= OnEnterFluid;
                Locator.GetShipDetector().GetComponent<ShipFluidDetector>().OnExitFluid -= OnExitFluid;
            }
            if ((bool)Settings.enableAutoHatch.GetValue())
            {
                GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
                GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
            }
            _lastSuitOxygen = 0f;
            _shipLoaded = false;
        };
    }

    private void Update()
    {
        if (!_shipLoaded || LoadManager.GetCurrentScene() != OWScene.SolarSystem || _shipDestroyed) return;

        if (Locator.GetShipBody().GetAngularVelocity().sqrMagnitude > maxSpinSpeed * maxSpinSpeed)
        {
            ShipOxygenTankComponent oxygenTank = Locator.GetShipBody().GetComponentInChildren<ShipOxygenTankComponent>();
            if (oxygenTank.isDamaged)
            {
                oxygenTank._damageEffect._particleSystem.Stop();
                oxygenTank._damageEffect._particleAudioSource.Stop();
            }
            ShipFuelTankComponent fuelTank = Locator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>();
            if (fuelTank.isDamaged)
            {
                fuelTank._damageEffect._particleSystem.Stop();
                fuelTank._damageEffect._particleAudioSource.Stop();
            }
            Locator.GetShipBody().GetComponent<ShipDamageController>().Explode();
        }

        if (!oxygenDepleted && SELocator.GetShipResources().GetOxygen() <= 0 && !((bool)Settings.shipOxygenRefill.GetProperty() && IsShipInOxygen()))
        {
            oxygenDepleted = true;

            ShipNotifications.OnOxygenDepleted();

            if (PlayerState.IsInsideShip())
            {
                if ((bool)Settings.keepHelmetOn.GetProperty() && PlayerState.IsWearingSuit() && !Locator.GetPlayerSuit().IsWearingHelmet())
                {
                    Locator.GetPlayerSuit().PutOnHelmet();
                }
                SELocator.GetShipOxygenVolume().OnEffectVolumeExit(Locator.GetPlayerDetector());
            }

            ShipOxygenTankComponent oxygenTank = Locator.GetShipBody().GetComponentInChildren<ShipOxygenTankComponent>();
            if (oxygenTank.isDamaged)
            {
                oxygenTank._damageEffect._particleSystem.Stop();
                oxygenTank._damageEffect._particleAudioSource.Stop();
            }
        }
        else if (oxygenDepleted && (SELocator.GetShipResources().GetOxygen() > 0 || ((bool)Settings.shipOxygenRefill.GetProperty() && IsShipInOxygen())))
        {
            oxygenDepleted = false;
            refillingOxygen = true;

            ShipNotifications.OnOxygenRestored();

            if (PlayerState.IsInsideShip())
            {
                SELocator.GetShipOxygenVolume().OnEffectVolumeEnter(Locator.GetPlayerDetector());
            }

            ShipOxygenTankComponent oxygenTank = Locator.GetShipBody().GetComponentInChildren<ShipOxygenTankComponent>();
            if (oxygenTank.isDamaged)
            {
                oxygenTank._damageEffect._particleSystem.Play();
                oxygenTank._damageEffect._particleAudioSource.Play();
            }
        }

        if (!fuelDepleted && SELocator.GetShipResources()._currentFuel <= 0f)
        {
            fuelDepleted = true;
            ShipFuelTankComponent fuelTank = Locator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>();
            if (fuelTank.isDamaged)
            {
                fuelTank._damageEffect._particleSystem.Stop();
                fuelTank._damageEffect._particleAudioSource.Stop();
            }
        }
        else if (fuelDepleted && SELocator.GetShipResources()._currentFuel > 0f)
        {
            fuelDepleted = false;
            ShipFuelTankComponent fuelTank = Locator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>();
            if (fuelTank.isDamaged)
            {
                fuelTank._damageEffect._particleSystem.Play();
                fuelTank._damageEffect._particleAudioSource.Play();
            }
        }

        if ((bool)Settings.showWarningNotifications.GetProperty() && !_shipDestroyed)
        {
            ShipNotifications.UpdateNotifications();
        }

        _lastShipOxygen = SELocator.GetShipResources()._currentOxygen;
        _lastShipFuel = SELocator.GetShipResources()._currentFuel;
    }

    private void LateUpdate()
    {
        if (!_shipLoaded || LoadManager.GetCurrentScene() != OWScene.SolarSystem) return;

        if (!SELocator.GetPlayerResources()._refillingOxygen && refillingOxygen)
        {
            refillingOxygen = false;
        }
    }

    #region Initialization

    private void UpdateProperties()
    {
        var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];

        foreach (Settings setting in allSettings)
        {
            setting.SetProperty(ModHelper.Config.GetSettingsValue<object>(setting.GetName()));
        }
    }

    private IEnumerator InitializeShip()
    {
        yield return new WaitUntil(() => Locator._shipBody != null);

        SELocator.Initalize();

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

        _shipLoaded = true;
        UpdateSuitOxygen();
        _lastShipOxygen = SELocator.GetShipResources()._currentOxygen;

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
            SELocator.GetShipResources().SetOxygen(0f);
            oxygenDepleted = true;
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
        if ((bool)Settings.disableSpaceAngularDrag.GetValue())
        {
            Locator.GetShipDetector().GetComponent<ShipFluidDetector>().OnEnterFluid += OnEnterFluid;
            Locator.GetShipDetector().GetComponent<ShipFluidDetector>().OnExitFluid += OnExitFluid;
        }
        if ((string)Settings.gravityDirection.GetValue() != "Down" && !(bool)Settings.disableGravityCrystal.GetValue())
        {
            ShipDirectionalForceVolume shipGravity = Locator.GetShipBody().GetComponentInChildren<ShipDirectionalForceVolume>();
            Vector3 direction = Vector3.down;
            switch ((string)Settings.gravityDirection.GetValue())
            {
                case "Up":
                    direction = Vector3.up;
                    break;
                case "Left":
                    direction = Vector3.left;
                    break;
                case "Right":
                    direction = Vector3.right;
                    break;
                case "Forward":
                    direction = Vector3.forward;
                    break;
                case "Back":
                    direction = Vector3.back;
                    break;
            }
            shipGravity._fieldDirection = direction;
        }
        if ((bool)Settings.enableManualScoutRecall.GetValue())
        {
            ShipProbeLauncherEffects launcherEffects = Locator.GetShipBody().GetComponentInChildren<PlayerProbeLauncher>()
                .gameObject.AddComponent<ShipProbeLauncherEffects>();
            GameObject probePickupVolume = LoadPrefab("Assets/ShipEnhancements/PlayerProbePickupVolume.prefab");
            Instantiate(probePickupVolume, Locator.GetProbe().transform);
            GameObject shipProbePickupVolume = LoadPrefab("Assets/ShipEnhancements/ShipProbePickupVolume.prefab");
            GameObject shipProbeVolume = Instantiate(shipProbePickupVolume, launcherEffects.transform);

            if ((bool)Settings.enableScoutLauncherComponent.GetValue())
            {
                Locator.GetShipBody().GetComponentInChildren<ProbeLauncherComponent>().SetProbeLauncherEffects(launcherEffects);
            }
        }
        if ((bool)Settings.disableScoutLauncher.GetValue() && (bool)Settings.enableScoutLauncherComponent.GetValue())
        {
            SELocator.GetProbeLauncherComponent()._repairReceiver.repairDistance = 0f;
            SELocator.GetProbeLauncherComponent()._damaged = true;
            SELocator.GetProbeLauncherComponent()._repairFraction = 0f;
            SELocator.GetProbeLauncherComponent().OnComponentDamaged();
        }
        if ((bool)Settings.addPortableCampfire.GetValue())
        {
            Transform suppliesParent = Locator.GetShipTransform().Find("Module_Supplies");
            GameObject portableCampfireSocket = LoadPrefab("Assets/ShipEnhancements/PortableCampfireSocket.prefab");
            PortableCampfireSocket campfireSocket = Instantiate(portableCampfireSocket, suppliesParent).GetComponent<PortableCampfireSocket>();
            GameObject portableCampfireItem = LoadPrefab("assets/ShipEnhancements/PortableCampfireItem.prefab");
            AssetBundleUtilities.ReplaceShaders(portableCampfireItem);
            PortableCampfireItem campfireItem = Instantiate(portableCampfireItem, suppliesParent).GetComponent<PortableCampfireItem>();
            campfireSocket.SetCampfireItem(campfireItem);
        }
        if ((float)Settings.shipExplosionMultiplier.GetValue() != 1f)
        {
            Transform effectsTransform = Locator.GetShipTransform().Find("Effects");
            ExplosionController explosion = effectsTransform.GetComponentInChildren<ExplosionController>();
            explosion._length *= ((float)Settings.shipExplosionMultiplier.GetValue() * 0.75f) + 0.25f;
            explosion._forceVolume._acceleration *= ((float)Settings.shipExplosionMultiplier.GetValue() * 0.25f) + 0.75f;
            explosion.transform.localScale *= (float)Settings.shipExplosionMultiplier.GetValue();
            explosion.GetComponent<SphereCollider>().radius = 0.1f;
            OWAudioSource audio = effectsTransform.Find("ExplosionAudioSource").GetComponent<OWAudioSource>();
            audio.maxDistance *= ((float)Settings.shipExplosionMultiplier.GetValue() * 0.1f) + 0.9f;
            AnimationCurve curve = audio.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
            Keyframe[] newKeys = new Keyframe[curve.keys.Length];
            for (int i = 0; i < curve.keys.Length; i++)
            {
                newKeys[i] = curve.keys[i];
                newKeys[i].value *= ((float)Settings.shipExplosionMultiplier.GetValue() * 0.1f) + 0.9f;
            }
            AnimationCurve newCurve = new();
            foreach (Keyframe key in newKeys)
            {
                newCurve.AddKey(key);
            }
            audio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, newCurve);
        }

        ShipNotifications.Initialize();
    }

    private void AddTemperatureZones()
    {
        GameObject sun = GameObject.Find("Sun_Body");
        if (sun != null)
        {
            GameObject sunTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Sun.prefab");
            Instantiate(sunTempZone, sun.transform.Find("Sector_SUN/Volumes_SUN"));
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

    private void DisableHeadlights()
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

    #endregion

    #region Events

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

    private void OnEnterFluid(FluidVolume fluid)
    {
        angularDragEnabled = true;
        Locator.GetShipBody()._rigidbody.angularDrag = 0.94f * (float)Settings.angularDragMultiplier.GetProperty();
        Locator.GetShipBody().GetComponent<ShipThrusterModel>()._angularDrag = 0.94f * (float)Settings.angularDragMultiplier.GetProperty();
    }

    private void OnExitFluid(FluidVolume fluid)
    {
        if (Locator.GetShipDetector().GetComponent<ShipFluidDetector>()._activeVolumes.Count == 0)
        {
            angularDragEnabled = false;
            Locator.GetShipBody()._rigidbody.angularDrag = 0f;
            Locator.GetShipBody().GetComponent<ShipThrusterModel>()._angularDrag = 0f;
        }
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

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
        Locator.GetShipBody().SetCenterOfMass(Locator.GetShipBody().GetWorldCenterOfMass());
    }

    #endregion

    #region Properties

    public void UpdateSuitOxygen()
    {
        _lastSuitOxygen = Locator.GetPlayerBody().GetComponent<PlayerResources>()._currentOxygen;
    }

    public bool IsShipInOxygen()
    {
        return !_shipDestroyed && SELocator.GetShipOxygenDetector() != null && SELocator.GetShipOxygenDetector().GetDetectOxygen()
            && !Locator.GetShipDetector().GetComponent<ShipFluidDetector>().InFluidType(FluidVolume.Type.WATER);
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
        thrustModulatorLevel = level;
    }

    #endregion

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
        if (!SettingsPresets.Initialized())
        {
            return;
        }

        var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];
        SettingsPresets.PresetName newPreset = SettingsPresets.GetPresetFromConfig(config.GetSettingsValue<string>("preset"));
        if (newPreset != _currentPreset || _currentPreset == (SettingsPresets.PresetName)(-1))
        {
            _currentPreset = newPreset;
            config.SetSettingsValue("preset", _currentPreset.GetName());
            SettingsPresets.ApplyPreset(newPreset, config);
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
            }
        }
    }

    public override object GetApi()
    {
        return new ShipEnhancementsAPI();
    }
}
