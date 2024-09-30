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

    public delegate void ResourceEvent();
    public event ResourceEvent OnFuelDepleted;
    public event ResourceEvent OnFuelRestored;

    public delegate void EngineEvent(bool enabled);
    public event EngineEvent OnEngineStateChanged;

    public static ShipEnhancements Instance;
    public bool oxygenDepleted;
    public bool refillingOxygen;
    public bool fuelDepleted;
    public bool angularDragEnabled;
    public float levelOneSpinSpeed = 8f;
    public float levelTwoSpinSpeed = 16f;
    public float maxSpinSpeed = 24f;
    public bool probeDestroyed;
    public bool engineOn;
    public Tether playerTether;

    public IAchievements AchievementsAPI;

    public UITextType probeLauncherName { get; private set; }
    public UITextType signalscopeName { get; private set; }
    public ItemType portableCampfireType { get; private set; }
    public ItemType tetherHookType { get; private set; }
    public ItemType portableTractorBeamType { get; private set; }
    public SignalName shipSignalName { get; private set; }
    public int thrustModulatorLevel { get; private set; }

    private SettingsPresets.PresetName _currentPreset = (SettingsPresets.PresetName)(-1);

    private AssetBundle _shipEnhancementsBundle;
    private PhysicMaterial _bouncyMaterial;
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
        atmosphereAngularDragMultiplier,
        spaceAngularDragMultiplier,
        disableRotationSpeedLimit,
        gravityDirection,
        disableScoutRecall,
        disableScoutLaunching,
        enableScoutLauncherComponent,
        enableManualScoutRecall,
        enableShipItemPlacement,
        addPortableCampfire,
        keepHelmetOn,
        showWarningNotifications,
        shipExplosionMultiplier,
        shipBounciness,
        enablePersistentInput,
        shipInputLatency,
        addEngineSwitch,
        idleFuelConsumptionMultiplier,
        shipLightColor,
        hotThrusters,
        extraNoise,
        interiorHullColor,
        exteriorHullColor,
        addTether,
        disableDamageIndicators,
        addShipSignal,
        reactorLifetimeMultiplier,
        disableShipFriction,
        enableSignalscopeComponent,
        rustLevel,
        dirtAccumulationTime,
        thrusterColor,
        disableSeatbelt,
        addPortableTractorBeam,
    }

    private void Awake()
    {
        Instance = this;
        HarmonyLib.Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly());
    }

    private void Start()
    {
        _shipEnhancementsBundle = AssetBundle.LoadFromFile(Path.Combine(ModHelper.Manifest.ModFolderPath, "assets/shipenhancements"));
        AchievementsAPI = ModHelper.Interaction.TryGetModApi<IAchievements>("xen.AchievementTracker");

        SettingsPresets.InitializePresets();

        probeLauncherName = EnumUtils.Create<UITextType>("ScoutLauncher");
        signalscopeName = EnumUtils.Create<UITextType>("Signalscope");
        portableCampfireType = EnumUtils.Create<ItemType>("PortableCampfire");
        tetherHookType = EnumUtils.Create<ItemType>("TetherHook");
        portableTractorBeamType = EnumUtils.Create<ItemType>("PortableTractorBeam");
        shipSignalName = EnumUtils.Create<SignalName>("Ship");

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

            GlobalMessenger.RemoveListener("SuitUp", OnPlayerSuitUp);
            GlobalMessenger.RemoveListener("RemoveSuit", OnPlayerRemoveSuit);
            GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
            if ((float)Settings.spaceAngularDragMultiplier.GetProperty() > 0 || (float)Settings.atmosphereAngularDragMultiplier.GetProperty() > 0)
            {
                Locator.GetShipDetector().GetComponent<ShipFluidDetector>().OnEnterFluid -= OnEnterFluid;
                Locator.GetShipDetector().GetComponent<ShipFluidDetector>().OnExitFluid -= OnExitFluid;
            }
            if ((bool)Settings.enableAutoHatch.GetProperty())
            {
                GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
                GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
            }
            UpdateProperties();
            _lastSuitOxygen = 0f;
            _shipLoaded = false;
            playerTether = null;
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

        if ((float)Settings.idleFuelConsumptionMultiplier.GetProperty() > 0f && !(bool)Settings.addEngineSwitch.GetProperty())
        {
            SELocator.GetShipResources().DrainFuel(0.5f * (float)Settings.idleFuelConsumptionMultiplier.GetProperty() * Time.deltaTime);
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
            OnFuelDepleted?.Invoke();
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
            OnFuelRestored?.Invoke();
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

    private void FixedUpdate()
    {
        if ((float)Settings.shipInputLatency.GetProperty() > 0f)
        {
            InputLatencyController.FixedUpdate();
        }
    }

    #region Initialization

    private void UpdateProperties()
    {
        var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];

        foreach (Settings setting in allSettings)
        {
            if (ModHelper.Config.GetSettingsValue<string>("preset") == "Random")
            {
                setting.SetProperty(SettingsPresets.GetPresetSetting(SettingsPresets.PresetName.Random, setting.GetName()));
            }
            else
            {
                setting.SetProperty(ModHelper.Config.GetSettingsValue<object>(setting.GetName()));
            }
        }
    }

    private void InitializeAchievements()
    {
        AchievementsAPI.RegisterAchievement("SE_TORQUE_EXPLOSION", false, this);
        AchievementsAPI.RegisterAchievement("SE_RGB_SETUP", false, this);
        AchievementsAPI.RegisterAchievement("SE_HOW_DID_WE_GET_HERE", false, this);
        AchievementsAPI.RegisterAchievement("SE_SCOUT_LOST_CONNECTION", false, this);
        AchievementsAPI.RegisterAchievement("SE_FIRE_HAZARD", false, this);
        AchievementsAPI.RegisterAchievement("SE_HULK_SMASH", false, this);
        AchievementsAPI.RegisterAchievement("SE_BAD_INTERNET", false, this);
    }

    private IEnumerator InitializeShip()
    {
        yield return new WaitUntil(() => Locator._shipBody != null);

        SELocator.Initalize();
        ThrustIndicatorManager.Initialize();

        GameObject buttonConsole = LoadPrefab("Assets/ShipEnhancements/ButtonConsole.prefab");
        AssetBundleUtilities.ReplaceShaders(buttonConsole);
        Instantiate(buttonConsole, Locator.GetShipBody().transform.Find("Module_Cockpit"));

        Material material1 = (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_VillageCabin_Recolored_mat.mat");
        Material material2 = (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_VillageMetal_Recolored_mat.mat");
        Material material3 = (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_VillagePlanks_Recolored_mat.mat");
        Material material4 = (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_CampsiteProps_Recolored_mat.mat");
        Transform cockpitLight = Locator.GetShipTransform().Find("Module_Cockpit/Lights_Cockpit/Pointlight_HEA_ShipCockpit");
        List<Material> materials = [.. cockpitLight.GetComponent<LightmapController>()._materials];
        materials.Add(material1);
        materials.Add(material2);
        materials.Add(material3);
        materials.Add(material4);
        cockpitLight.GetComponent<LightmapController>()._materials = [.. materials];

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
            GameObject ejectButtonTape = LoadPrefab("Assets/ShipEnhancements/EjectButtonTape.prefab");
            AssetBundleUtilities.ReplaceShaders(ejectButtonTape);
            Instantiate(ejectButtonTape, Locator.GetShipBody().transform.Find("Module_Cockpit/Geo_Cockpit"));
        }
        if ((bool)Settings.disableHeadlights.GetValue())
        {
            DisableHeadlights();
        }
        if ((bool)Settings.disableLandingCamera.GetValue())
        {
            DisableLandingCamera();
        }
        bool coloredLights = (string)Settings.shipLightColor.GetValue() != "Default";
        if ((bool)Settings.disableShipLights.GetValue() || coloredLights)
        {
            Color lightColor = SettingsColors.GetLightingColor((string)Settings.shipLightColor.GetValue());
            foreach (ElectricalSystem system in Locator.GetShipBody().GetComponentsInChildren<ElectricalSystem>())
            {
                foreach (ElectricalComponent component in system._connectedComponents)
                {
                    if (component.gameObject.name.Contains("Pointlight") && component.TryGetComponent(out ShipLight light))
                    {
                        if ((bool)Settings.disableShipLights.GetValue())
                        {
                            light.SetOn(false);
                            light._light.enabled = false;
                        }
                        else if (coloredLights)
                        {
                            light._light.color = lightColor;
                            light._baseEmission = lightColor;
                            if (light.IsPowered() && light.IsOn())
                            {
                                light._matPropBlock.SetColor(light._propID_EmissionColor, lightColor);
                                light._emissiveRenderer.SetPropertyBlock(light._matPropBlock);
                            }
                            if ((string)Settings.shipLightColor.GetValue() == "Rainbow")
                            {
                                light.gameObject.AddComponent<RainbowShipLight>();
                            }
                        }
                    }
                }
            }
            foreach (PulsingLight beacon in Locator.GetShipBody().transform.Find("Module_Cabin/Lights_Cabin/ShipBeacon_Proxy").GetComponentsInChildren<PulsingLight>())
            {
                if ((bool)Settings.disableShipLights.GetValue())
                {
                    PulsingLight.s_matPropBlock.SetColor(PulsingLight.s_propID_EmissionColor, beacon._initEmissionColor * 0f);
                    beacon._emissiveRenderer.SetPropertyBlock(PulsingLight.s_matPropBlock);
                    beacon.gameObject.SetActive(false);
                }
                else if (coloredLights)
                {
                    beacon.GetComponent<Light>().color = lightColor;
                    beacon._initEmissionColor = lightColor;
                    if ((string)Settings.shipLightColor.GetValue() == "Rainbow")
                    {
                        beacon.gameObject.AddComponent<RainbowShipLight>();
                    }
                }
            }
        }
        if ((bool)Settings.disableShipOxygen.GetValue())
        {
            SELocator.GetShipResources().SetOxygen(0f);
            oxygenDepleted = true;
            Locator.GetShipTransform().Find("Module_Cockpit/Props_Cockpit/Props_HEA_ShipFoliage").gameObject.SetActive(false);
        }
        if (Settings.temperatureZonesAmount.GetValue().ToString() != "None")
        {
            Locator.GetShipBody().GetComponentInChildren<ShipFuelGauge>().gameObject.AddComponent<ShipTemperatureGauge>();
            GameObject hullTempDial = LoadPrefab("Assets/ShipEnhancements/ShipTempDial.prefab");
            Instantiate(hullTempDial, Locator.GetShipTransform().Find("Module_Cockpit"));

            if (Settings.temperatureZonesAmount.GetValue().ToString() == "Sun")
            {
                GameObject sun = GameObject.Find("Sun_Body");
                if (sun != null)
                {
                    GameObject sunTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Sun.prefab");
                    Instantiate(sunTempZone, sun.transform.Find("Sector_SUN"));
                }
            }
            else
            {
                AddTemperatureZones();
            }
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
        if ((float)Settings.spaceAngularDragMultiplier.GetValue() > 0 || (float)Settings.atmosphereAngularDragMultiplier.GetValue() > 0)
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
                case "Random":
                    direction = new Vector3(UnityEngine.Random.Range(-1f, 1f), 
                        UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
                    break;
            }
            shipGravity._fieldDirection = direction;
        }
        if ((bool)Settings.enableManualScoutRecall.GetValue() || (bool)Settings.disableScoutRecall.GetValue() || (bool)Settings.disableScoutLaunching.GetValue())
        {
            ShipProbeLauncherEffects launcherEffects = Locator.GetShipBody().GetComponentInChildren<PlayerProbeLauncher>()
                .gameObject.AddComponent<ShipProbeLauncherEffects>();
            if ((bool)Settings.enableManualScoutRecall.GetValue())
            {
                GameObject probePickupVolume = LoadPrefab("Assets/ShipEnhancements/PlayerProbePickupVolume.prefab");
                Instantiate(probePickupVolume, Locator.GetProbe().transform);
            }
            GameObject shipProbePickupVolume = LoadPrefab("Assets/ShipEnhancements/ShipProbePickupVolume.prefab");
            GameObject shipProbeVolume = Instantiate(shipProbePickupVolume, launcherEffects.transform);

            if ((bool)Settings.enableScoutLauncherComponent.GetValue())
            {
                Locator.GetShipBody().GetComponentInChildren<ProbeLauncherComponent>().SetProbeLauncherEffects(launcherEffects);
            }
        }
        if ((bool)Settings.disableScoutRecall.GetValue() && (bool)Settings.disableScoutLaunching.GetValue() && (bool)Settings.enableScoutLauncherComponent.GetValue())
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
        if ((float)Settings.shipBounciness.GetValue() > 0f)
        {
            Locator.GetShipTransform().gameObject.AddComponent<ShipBouncyHull>();
        }
        if ((bool)Settings.enablePersistentInput.GetValue())
        {
            Locator.GetShipBody().gameObject.AddComponent<ShipPersistentInput>();
        }
        if ((float)Settings.shipInputLatency.GetValue() > 0f)
        {
            InputLatencyController.Initialize();
        }
        if ((bool)Settings.hotThrusters.GetValue() || (string)Settings.thrusterColor.GetValue() != "Default")
        {
            GameObject flameHazardVolume = LoadPrefab("Assets/ShipEnhancements/FlameHeatVolume.prefab");
            foreach (ThrusterFlameController flame in Locator.GetShipTransform().GetComponentsInChildren<ThrusterFlameController>())
            {
                GameObject volume = Instantiate(flameHazardVolume, Vector3.zero, Quaternion.identity, flame.transform);
                volume.transform.localPosition = Vector3.zero;
                volume.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                if (!flame.enabled)
                {
                    flame.transform.localScale = Vector3.zero;
                }

                string color = (string)Settings.thrusterColor.GetValue();

                if (color == "Rainbow")
                {
                    flame.gameObject.AddComponent<RainbowShipThrusters>();
                }
                else if (color != "Default")
                {
                    MeshRenderer rend = flame.GetComponent<MeshRenderer>();
                    Color baseColor = rend.material.GetColor("_Color");
                    float alpha = baseColor.a;
                    (Color, float, Color) emissiveColor = SettingsColors.GetThrusterColor(color);
                    Color newColor = emissiveColor.Item1 * Mathf.Pow(emissiveColor.Item2, 2);
                    newColor.a = alpha;
                    rend.material.SetColor("_Color", newColor);

                    Light light = flame.GetComponentInChildren<Light>();
                    light.color = emissiveColor.Item3;

                    ThrustIndicatorManager.SetColor(SettingsColors.GetIndicatorColor(color));
                }
            }
        }
        if ((float)Settings.reactorLifetimeMultiplier.GetValue() != 1f)
        {
            ShipReactorComponent reactor = Locator.GetShipTransform().GetComponentInChildren<ShipReactorComponent>();
            float multiplier = (float)Settings.reactorLifetimeMultiplier.GetValue();
            reactor._minCountdown *= multiplier;
            reactor._maxCountdown *= multiplier;
        }

        SetHullColor();

        if ((bool)Settings.addTether.GetValue())
        {
            GameObject hook = LoadPrefab("Assets/ShipEnhancements/TetherHook.prefab");
            AssetBundleUtilities.ReplaceShaders(hook);

            GameObject socketParent = Instantiate(LoadPrefab("Assets/ShipEnhancements/HookSocketParent.prefab"), Locator.GetShipTransform());
            socketParent.transform.localPosition = Vector3.zero;
            foreach (TetherHookSocket socket in socketParent.GetComponentsInChildren<TetherHookSocket>())
            {
                GameObject hookItem = Instantiate(hook);
                socket.PlaceIntoSocket(hookItem.GetComponent<TetherHookItem>());
                ModHelper.Events.Unity.FireOnNextUpdate(() =>
                {
                    hookItem.transform.localScale = Vector3.one * 0.7f;
                });
            }

            Locator.GetPlayerBody().gameObject.AddComponent<TetherPromptController>();
        }
        if ((bool)Settings.addShipSignal.GetValue())
        {
            GameObject signal = LoadPrefab("Assets/ShipEnhancements/ShipSignal.prefab");
            AudioSignal shipSignal = Instantiate(signal, Locator.GetShipTransform()
                .GetComponentInChildren<ShipCockpitUI>()._sigScopeDish).GetComponent<AudioSignal>();
            shipSignal.SetSector(Locator.GetShipTransform().GetComponentInChildren<Sector>());
            shipSignal._name = shipSignalName;
        }
        if ((bool)Settings.disableShipFriction.GetValue())
        {
            PhysicMaterial mat = (PhysicMaterial)LoadAsset("Assets/ShipEnhancements/FrictionlessShip.physicMaterial");
            foreach (Collider collider in Locator.GetShipTransform().GetComponentsInChildren<Collider>(true))
            {
                collider.material = mat;
            }
        }
        if ((float)Settings.rustLevel.GetValue() > 0f || (float)Settings.dirtAccumulationTime.GetValue() > 0f)
        {
            GameObject rustController = LoadPrefab("Assets/ShipEnhancements/RustController.prefab");
            AssetBundleUtilities.ReplaceShaders(rustController);
            Instantiate(rustController, Locator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry"));
        }
        if ((bool)Settings.addPortableTractorBeam.GetValue())
        {
            GameObject tractor = LoadPrefab("Assets/ShipEnhancements/PortableTractorBeamItem.prefab");
            AssetBundleUtilities.ReplaceShaders(tractor);
            GameObject tractorObj = Instantiate(tractor);
            GameObject tractorSocket = LoadPrefab("Assets/ShipEnhancements/PortableTractorBeamSocket.prefab");
            AssetBundleUtilities.ReplaceShaders(tractorSocket);
            GameObject tractorSocketObj = Instantiate(tractorSocket, Locator.GetShipTransform().Find("Module_Cabin"));
            tractorSocketObj.GetComponent<PortableTractorBeamSocket>().SetTractorBeamItem(tractorObj.GetComponent<PortableTractorBeamItem>());
        }

        engineOn = !(bool)Settings.addEngineSwitch.GetValue();

        ShipNotifications.Initialize();
        SELocator.LateInitialize();
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

    private void SetHullColor()
    {
        MeshRenderer suppliesRenderer = Locator.GetShipTransform().
            Find("Module_Supplies/Geo_Supplies/Supplies_Geometry/Supplies_Interior").GetComponent<MeshRenderer>();
        Material inSharedMat = suppliesRenderer.sharedMaterials[0];

        Transform buttonPanel = Locator.GetShipTransform().GetComponentInChildren<CockpitButtonPanel>().transform;
        MeshRenderer buttonPanelRenderer = buttonPanel.Find("Panel/PanelBody.001").GetComponent<MeshRenderer>();
        Material inSharedMat2 = buttonPanelRenderer.sharedMaterials[0];
        if ((string)Settings.interiorHullColor.GetProperty() != "Default")
        {

            if ((string)Settings.interiorHullColor.GetProperty() == "Rainbow")
            {
                if (Locator.GetShipTransform().TryGetComponent(out RainbowShipHull rainbowHull))
                {
                    rainbowHull.AddSharedMaterial(inSharedMat);
                    rainbowHull.AddSharedMaterial(inSharedMat2);
                }
                else
                {
                    rainbowHull = Locator.GetShipTransform().gameObject.AddComponent<RainbowShipHull>();
                    rainbowHull.AddSharedMaterial(inSharedMat);
                    rainbowHull.AddSharedMaterial(inSharedMat2);
                }
            }
            else
            {
                inSharedMat.SetColor("_Color",
                    SettingsColors.GetShipColor((string)Settings.interiorHullColor.GetProperty()));
                inSharedMat2.SetColor("_Color",
                    SettingsColors.GetShipColor((string)Settings.interiorHullColor.GetProperty()));
            }
        }
        else
        {
            inSharedMat.SetColor("_Color", Color.white);
            inSharedMat2.SetColor("_Color", Color.white);
        }

        MeshRenderer cabinRenderer = Locator.GetShipTransform().
            Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Exterior").GetComponent<MeshRenderer>();
        Material outSharedMat = cabinRenderer.sharedMaterials[3];
        if ((string)Settings.exteriorHullColor.GetProperty() != "Default")
        {
            if ((string)Settings.exteriorHullColor.GetProperty() == "Rainbow")
            {
                if (Locator.GetShipTransform().TryGetComponent(out RainbowShipHull rainbowHull))
                {
                    rainbowHull.AddSharedMaterial(outSharedMat);
                }
                else
                {
                    rainbowHull = Locator.GetShipTransform().gameObject.AddComponent<RainbowShipHull>();
                    rainbowHull.AddSharedMaterial(outSharedMat);
                }
            }
            else
            {
                outSharedMat.SetColor("_Color",
                    SettingsColors.GetShipColor((string)Settings.exteriorHullColor.GetProperty()));
            }
        }
        else
        {
            outSharedMat.SetColor("_Color", Color.white);
        }
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
        Locator.GetShipBody()._rigidbody.angularDrag = 0.94f * (float)Settings.atmosphereAngularDragMultiplier.GetProperty();
        Locator.GetShipBody().GetComponent<ShipThrusterModel>()._angularDrag = 0.94f * (float)Settings.atmosphereAngularDragMultiplier.GetProperty();
    }

    private void OnExitFluid(FluidVolume fluid)
    {
        if (Locator.GetShipDetector().GetComponent<ShipFluidDetector>()._activeVolumes.Count == 0)
        {
            angularDragEnabled = false;
            Locator.GetShipBody()._rigidbody.angularDrag = 0.94f * (float)Settings.spaceAngularDragMultiplier.GetProperty();
            Locator.GetShipBody().GetComponent<ShipThrusterModel>()._angularDrag = 0.94f * (float)Settings.spaceAngularDragMultiplier.GetProperty();
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
        OnGravityLandingGearSwitch?.Invoke(enabled);
    }

    public void SetThrustModulatorLevel(int level)
    {
        thrustModulatorLevel = level;
    }

    public void SetEngineOn(bool state)
    {
        engineOn = state;
        OnEngineStateChanged?.Invoke(state);
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

    public static object LoadAsset(string path)
    {
        return Instance._shipEnhancementsBundle.LoadAsset(path);
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
                if (_currentPreset != SettingsPresets.PresetName.Custom && _currentPreset != SettingsPresets.PresetName.Random)
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
