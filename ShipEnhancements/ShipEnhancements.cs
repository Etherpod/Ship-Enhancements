using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using OWML.Utils;
using System.Reflection;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.TextCore.LowLevel;

namespace ShipEnhancements;

public class ShipEnhancements : ModBehaviour
{
    public delegate void SwitchEvent(bool enabled);
    public event SwitchEvent OnGravityLandingGearSwitch;
    public event SwitchEvent OnGravityLandingGearInverted;

    public delegate void ResourceEvent();
    public event ResourceEvent OnFuelDepleted;
    public event ResourceEvent OnFuelRestored;

    public delegate void EngineEvent(bool enabled);
    public event EngineEvent OnEngineStateChanged;

    public UnityEvent PreShipInitialize;
    public UnityEvent PostShipInitialize;

    public static ShipEnhancements Instance;
    public bool oxygenDepleted;
    public bool refillingOxygen;
    public bool fuelDepleted;
    public float levelOneSpinSpeed = 8f;
    public float levelTwoSpinSpeed = 16f;
    public float maxSpinSpeed = 24f;
    public bool probeDestroyed;
    public bool engineOn;
    public Tether playerTether;
    public bool anyPartDamaged;
    public bool groundedByHornfels;
    public bool shipIgniting;

    public static IAchievements AchievementsAPI;
    public static IQSBAPI QSBAPI;
    public static QSBCompatibility QSBCompat;
    public static IQSBInteraction QSBInteraction;

    public static INewHorizons NHAPI;
    public static INHInteraction NHInteraction;

    public static bool VanillaFixEnabled;

    public static uint[] PlayerIDs
    {
        get
        {
            return QSBAPI.GetPlayerIDs().Where(id => id != QSBAPI.GetLocalPlayerID()).ToArray();
        }
    }

    private ShipResourceSyncManager _shipResourceSync;

    public static bool InMultiplayer
    {
        get
        {
            return QSBAPI != null && QSBAPI.GetIsInMultiplayer();
        }
    }

    public UITextType ProbeLauncherName { get; private set; }
    public UITextType SignalscopeName { get; private set; }
    public UITextType WarpCoreName { get; private set; }
    public ItemType PortableCampfireType { get; private set; }
    public ItemType TetherHookType { get; private set; }
    public ItemType PortableTractorBeamType { get; private set; }
    public ItemType ExpeditionFlagType { get; private set; }
    public ItemType FuelTankType { get; private set; }
    public ItemType GravityCrystalType { get; private set; }
    public ItemType RepairWrenchType { get; private set; }
    public ItemType RadioType { get; private set; }
    public SignalName ShipSignalName { get; private set; }
    public int ThrustModulatorLevel { get; private set; }

    private SettingsPresets.PresetName _currentPreset = (SettingsPresets.PresetName)(-1);

    private AssetBundle _shipEnhancementsBundle;
    private float _lastSuitOxygen;
    private bool _shipLoaded = false;
    private bool _shipDestroyed;
    private bool _checkEndConversation = false;
    private bool _setupQSB = false;
    private bool _disableAirWhenZeroOxygen = false;
    private bool _unsubFromBodyLoaded = false;
    private bool _unsubFromSystemLoaded = false;
    private bool _unsubFromShipSpawn = false;

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
        shipFriction,
        enableSignalscopeComponent,
        rustLevel,
        dirtAccumulationTime,
        thrusterColor,
        disableSeatbelt,
        addPortableTractorBeam,
        disableShipSuit,
        damageIndicatorColor,
        disableAutoLights,
        addExpeditionFlag,
        addFuelCanister,
        cycloneChaos,
        moreExplosionDamage,
        singleUseTractorBeam,
        disableThrusters,
        maxDirtAccumulation,
        addShipWarpCore,
        repairTimeMultiplier,
        airDragMultiplier,
        addShipClock,
        enableStunDamage,
        enableRepairConfirmation,
        shipGravityFix,
        enableRemovableGravityCrystal,
        randomHullDamage,
        randomComponentDamage,
        enableFragileShip,
        faultyHeatRegulators,
        addErnesto,
        repairLimit,
        extraEjectButtons,
        preventSystemFailure,
        addShipCurtain,
        addRepairWrench,
        funnySounds,
        alwaysAllowLockOn,
        shipWarpCoreComponent,
        disableShipMedkit,
        addRadio,
        disableFluidPrevention,
        disableHazardPrevention,
        prolongDigestion,
        unlimitedItems,
        noiseMultiplier,
        waterDamage,
        sandDamage,
        disableMinimapMarkers,
        scoutPhotoMode,
    }

    private void Awake()
    {
        Instance = this;
        HarmonyLib.Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    private void Start()
    {
        _shipEnhancementsBundle = AssetBundle.LoadFromFile(Path.Combine(ModHelper.Manifest.ModFolderPath, "assets/shipenhancements"));

        InitializeAchievements();
        InitializeQSB();
        InitializeNH();
        VanillaFixEnabled = ModHelper.Interaction.ModExists("JohnCorby.VanillaFix");
        ErnestoModListHandler.Initialize();
        SettingsPresets.InitializePresets();

        ProbeLauncherName = EnumUtils.Create<UITextType>("ScoutLauncher");
        SignalscopeName = EnumUtils.Create<UITextType>("Signalscope");
        WarpCoreName = EnumUtils.Create<UITextType>("WarpCore");
        PortableCampfireType = EnumUtils.Create<ItemType>("PortableCampfire");
        TetherHookType = EnumUtils.Create<ItemType>("TetherHook");
        PortableTractorBeamType = EnumUtils.Create<ItemType>("PortableTractorBeam");
        ExpeditionFlagType = EnumUtils.Create<ItemType>("ExpeditionFlag");
        FuelTankType = EnumUtils.Create<ItemType>("PortableFuelTank");
        GravityCrystalType = EnumUtils.Create<ItemType>("ShipGravityCrystal");
        RepairWrenchType = EnumUtils.Create<ItemType>("RepairWrench");
        RadioType = EnumUtils.Create<ItemType>("Radio");
        ShipSignalName = EnumUtils.Create<SignalName>("Ship");

        SEItemAudioController.Initialize();

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

            GlobalMessenger.AddListener("SuitUp", OnPlayerSuitUp);
            GlobalMessenger.AddListener("RemoveSuit", OnPlayerRemoveSuit);
            GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
            GlobalMessenger.AddListener("WakeUp", OnWakeUp);
            GlobalMessenger.AddListener("ShipHullDetached", OnShipHullDetached);
            oxygenDepleted = false;
            fuelDepleted = false;
            probeDestroyed = false;
            _shipDestroyed = false;
            anyPartDamaged = false;
            groundedByHornfels = false;
            shipIgniting = false;
            ShipRepairLimitController.SetRepairLimit(-1);
            ShipRepairLimitController.SetPartsRepaired(0);

            if (AchievementsAPI != null)
            {
                SEAchievementTracker.Reset();
            }

            PreShipInitialize?.Invoke();

            InitializeShip();

            GameObject th = GameObject.Find("TimberHearth_Body");
            if (th != null)
            {
                Transform slate = th.transform.Find("Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate");
                if (slate != null)
                {
                    DialogueBuilder.Make(slate.gameObject, "ConversationZone_RSci", "dialogue/Slate.xml", this);
                    if (!InMultiplayer || QSBAPI.GetIsHost())
                    {
                        if (_currentPreset == SettingsPresets.PresetName.Random)
                        {
                            slate.Find("ConversationZone_RSci").GetComponent<CharacterDialogueTree>().OnEndConversation += OnEndConversation;
                            _checkEndConversation = true;
                        }
                    }
                    else
                    {
                        WriteDebugMessage(QSBCompat.GetHostPreset());
                        if (QSBCompat.GetHostPreset() == SettingsPresets.PresetName.Random)
                        {
                            slate.Find("ConversationZone_RSci").GetComponent<CharacterDialogueTree>().OnEndConversation += OnEndConversation;
                            _checkEndConversation = true;
                        }
                    }
                }
            }

            PostShipInitialize.Invoke();
        };

        LoadManager.OnStartSceneLoad += (scene, loadScene) =>
        {
            if (scene == OWScene.TitleScreen)
            {
                if (!InMultiplayer || QSBAPI.GetIsHost())
                {
                    UpdateProperties();
                }
            }

            if (scene != OWScene.SolarSystem) return;

            GlobalMessenger.RemoveListener("SuitUp", OnPlayerSuitUp);
            GlobalMessenger.RemoveListener("RemoveSuit", OnPlayerRemoveSuit);
            GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
            GlobalMessenger.RemoveListener("WakeUp", OnWakeUp);
            GlobalMessenger.RemoveListener("ShipHullDetached", OnShipHullDetached);
            if ((float)Settings.spaceAngularDragMultiplier.GetProperty() > 0 || (float)Settings.atmosphereAngularDragMultiplier.GetProperty() > 0)
            {
                ShipFluidDetector detector = SELocator.GetShipDetector().GetComponent<ShipFluidDetector>();
                detector.OnEnterFluid -= OnEnterFluid;
                detector.OnExitFluid -= OnExitFluid;
            }
            if ((bool)Settings.enableAutoHatch.GetProperty())
            {
                GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
                GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
            }
            if ((bool)Settings.enableRepairConfirmation.GetProperty() || (bool)Settings.enableFragileShip.GetProperty())
            {
                foreach (ShipHull hull in SELocator.GetShipDamageController()._shipHulls)
                {
                    if (hull != null)
                    {
                        hull.OnDamaged -= ctx => CheckNoPartsDamaged();
                        hull.OnRepaired -= ctx => CheckNoPartsDamaged();
                    }
                }
                foreach (ShipComponent component in SELocator.GetShipDamageController()._shipComponents)
                {
                    if (component != null)
                    {
                        component.OnDamaged -= ctx => CheckNoPartsDamaged();
                        component.OnRepaired -= ctx => CheckNoPartsDamaged();
                    }
                }
            }
            if (_checkEndConversation)
            {
                GameObject th = GameObject.Find("TimberHearth_Body");
                if (th != null)
                {
                    Transform dialogue = th.transform.Find("Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate/ConversationZone_RSci");
                    if (dialogue != null)
                    {
                        dialogue.GetComponent<CharacterDialogueTree>().OnEndConversation -= OnEndConversation;
                    }
                }
                _checkEndConversation = false;
            }
            if ((bool)Settings.extraNoise.GetProperty())
            {
                GlobalMessenger.RemoveListener("StartShipIgnition", OnStartShipIgnition);
                GlobalMessenger.RemoveListener("CancelShipIgnition", OnStopShipIgnition);
                GlobalMessenger.RemoveListener("CompleteShipIgnition", OnStopShipIgnition);
            }
            if (AchievementsAPI != null)
            {
                SELocator.GetShipDamageController().OnShipComponentDamaged -= ctx => CheckAllPartsDamaged();
                SELocator.GetShipDamageController().OnShipHullDamaged -= ctx => CheckAllPartsDamaged();
            }
            if (NHAPI != null && _unsubFromBodyLoaded)
            {
                NHAPI.GetBodyLoadedEvent().RemoveListener(OnNHBodyLoaded);
                _unsubFromBodyLoaded = false;
            }
            if (NHAPI != null && _unsubFromSystemLoaded)
            {
                NHAPI.GetStarSystemLoadedEvent().RemoveListener(OnNHStarSystemLoaded);
                _unsubFromSystemLoaded = false;
            }
            if (NHAPI != null && _unsubFromShipSpawn)
            {
                NHAPI.GetStarSystemLoadedEvent().RemoveListener(SetCustomWarpDestination);
                _unsubFromShipSpawn = false;
            }

            if (!InMultiplayer || QSBAPI.GetIsHost())
            {
                UpdateProperties();

                if (InMultiplayer && QSBAPI.GetIsHost())
                {
                    foreach (uint id in PlayerIDs)
                    {
                        QSBCompat.SendSettingsData(id);
                    }
                }
            }

            _lastSuitOxygen = 0f;
            _shipLoaded = false;
            playerTether = null;
            InputLatencyController.OnUnloadScene();
        };
    }

    private void Update()
    {
        if (!_shipLoaded || LoadManager.GetCurrentScene() != OWScene.SolarSystem || _shipDestroyed) return;

        if (SELocator.GetShipBody().GetAngularVelocity().sqrMagnitude > maxSpinSpeed * maxSpinSpeed)
        {
            ShipOxygenTankComponent oxygenTank = SELocator.GetShipBody().GetComponentInChildren<ShipOxygenTankComponent>();
            if (oxygenTank.isDamaged)
            {
                oxygenTank._damageEffect._particleSystem.Stop();
                oxygenTank._damageEffect._particleAudioSource.Stop();
            }
            ShipFuelTankComponent fuelTank = SELocator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>();
            if (fuelTank.isDamaged)
            {
                fuelTank._damageEffect._particleSystem.Stop();
                fuelTank._damageEffect._particleAudioSource.Stop();
            }
            SELocator.GetShipBody().GetComponent<ShipDamageController>().Explode();

            if (!SEAchievementTracker.TorqueExplosion && AchievementsAPI != null)
            {
                SEAchievementTracker.TorqueExplosion = true;
                AchievementsAPI?.EarnAchievement("SHIPENHANCEMENTS.TORQUE_EXPLOSION");
            }
        }

        if (InMultiplayer && QSBAPI.GetIsHost())
        {
            _shipResourceSync?.Update();
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

            ShipOxygenTankComponent oxygenTank = SELocator.GetShipBody().GetComponentInChildren<ShipOxygenTankComponent>();
            if (oxygenTank.isDamaged)
            {
                oxygenTank._damageEffect._particleSystem.Stop();
                oxygenTank._damageEffect._particleAudioSource.Stop();
            }

            if (_disableAirWhenZeroOxygen)
            {
                OWTriggerVolume atmoVolume = SELocator.GetShipTransform().Find("Volumes/ShipAtmosphereVolume").GetComponent<OWTriggerVolume>();
                atmoVolume.SetTriggerActivation(false);
                _disableAirWhenZeroOxygen = false;
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

            ShipOxygenTankComponent oxygenTank = SELocator.GetShipBody().GetComponentInChildren<ShipOxygenTankComponent>();
            if (oxygenTank.isDamaged)
            {
                oxygenTank._damageEffect._particleSystem.Play();
                oxygenTank._damageEffect._particleAudioSource.Play();
            }
        }

        if (!fuelDepleted && SELocator.GetShipResources()._currentFuel <= 0f)
        {
            fuelDepleted = true;
            ShipFuelTankComponent fuelTank = SELocator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>();
            if (fuelTank.isDamaged)
            {
                fuelTank._damageEffect._particleSystem.Stop();
                fuelTank._damageEffect._particleAudioSource.Stop();
            }
            PlayerResources playerResources = SELocator.GetPlayerResources();
            if (playerResources.IsRefueling())
            {
                playerResources._isRefueling = false;
                NotificationManager.SharedInstance.UnpinNotification(playerResources._refuellingAndHealingNotification);
            }
            OnFuelDepleted?.Invoke();
        }
        else if (fuelDepleted && SELocator.GetShipResources()._currentFuel > 0f)
        {
            fuelDepleted = false;
            ShipFuelTankComponent fuelTank = SELocator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>();
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


        if (!SEAchievementTracker.DeadInTheWater && AchievementsAPI != null)
        {
            bool noFuel = !(!fuelDepleted || (bool)Settings.enableShipFuelTransfer.GetProperty()
                || (float)Settings.fuelDrainMultiplier.GetProperty() < 0f
                || (float)Settings.fuelTankDrainMultiplier.GetProperty() < 0f
                || (float)Settings.idleFuelConsumptionMultiplier.GetProperty() < 0f);
            bool noOxygen = (bool)Settings.disableShipOxygen.GetProperty()
                || !(!oxygenDepleted || (bool)Settings.shipOxygenRefill.GetProperty()
                || (float)Settings.oxygenDrainMultiplier.GetProperty() < 0f
                || (float)Settings.oxygenTankDrainMultiplier.GetProperty() < 0f);

            if (noFuel && noOxygen)
            {
                SEAchievementTracker.DeadInTheWater = true;
                AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.DEAD_IN_THE_WATER");
            }
        }
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
        if (!_shipLoaded || _shipDestroyed) return;

        if (InputLatencyController.ReadingSavedInputs && InputLatencyController.IsInputQueued)
        {
            InputLatencyController.ProcessSavedInputs();
        }
        else if ((float)Settings.shipInputLatency.GetProperty() > 0f)
        {
            InputLatencyController.ProcessInputs();
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
        AchievementsAPI = ModHelper.Interaction.ModExists("xen.AchievementTracker") ?
            ModHelper.Interaction.TryGetModApi<IAchievements>("xen.AchievementTracker")
            : null;

        if (AchievementsAPI != null)
        {
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.TORQUE_EXPLOSION", true, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.DEAD_IN_THE_WATER", true, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.FIRE_HAZARD", true, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.HOW_DID_WE_GET_HERE", false, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.HULK_SMASH", false, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.RGB_SETUP", false, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.BLACK_HOLE", true, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.BAD_INTERNET", false, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.SCOUT_LOST_CONNECTION", true, this);

            AchievementsAPI.RegisterTranslationsFromFiles(this, "translations");
        }
    }

    private void InitializeQSB()
    {
        bool qsbEnabled = ModHelper.Interaction.ModExists("Raicuparta.QuantumSpaceBuddies");
        if (qsbEnabled)
        {
            QSBAPI = ModHelper.Interaction.TryGetModApi<IQSBAPI>("Raicuparta.QuantumSpaceBuddies");
            QSBCompat = new QSBCompatibility(QSBAPI);
            var qsbAssembly = Assembly.LoadFrom(Path.Combine(ModHelper.Manifest.ModFolderPath, "ShipEnhancementsQSB.dll"));
            gameObject.AddComponent(qsbAssembly.GetType("ShipEnhancementsQSB.QSBInteraction", true));
            _shipResourceSync = new ShipResourceSyncManager(QSBCompat);

            QSBAPI.RegisterRequiredForAllPlayers(this);
        }
    }

    public void AssignQSBInterface(IQSBInteraction qsbInterface)
    {
        QSBInteraction = qsbInterface;
    }

    private void InitializeNH()
    {
        bool nhEnabled = ModHelper.Interaction.ModExists("xen.NewHorizons");
        if (nhEnabled)
        {
            NHAPI = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            var nhAssembly = Assembly.LoadFrom(Path.Combine(ModHelper.Manifest.ModFolderPath, "ShipEnhancementsNH.dll"));
            gameObject.AddComponent(nhAssembly.GetType("ShipEnhancementsNH.NHInteraction", true));
        }
    }

    public void AssignNHInterface(INHInteraction nhInterface)
    {
        NHInteraction = nhInterface;
    }

    private void InitializeShip()
    {
        WriteDebugMessage("Initialize Ship");

        SELocator.Initalize();
        ThrustIndicatorManager.Initialize();

        SELocator.GetShipBody().GetComponentInChildren<ShipCockpitController>()
            ._interactVolume.gameObject.AddComponent<FlightConsoleInteractController>();

        GameObject buttonConsole = LoadPrefab("Assets/ShipEnhancements/ButtonConsole.prefab");
        AssetBundleUtilities.ReplaceShaders(buttonConsole);
        Instantiate(buttonConsole, SELocator.GetShipBody().transform.Find("Module_Cockpit"));

        Material[] newMaterials =
        {
            (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_VillageCabin_Recolored_mat.mat"),
            (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_VillageMetal_Recolored_mat.mat"),
            (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_VillagePlanks_Recolored_mat.mat"),
            (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_CampsiteProps_Recolored_mat.mat"),
            (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_SignsDecal_Recolored_mat.mat"),
            (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_HEA_VillageCloth_Recolored_mat.mat"),
            (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_NOM_CopperOld_mat.mat"),
            (Material)_shipEnhancementsBundle.LoadAsset("Assets/ShipEnhancements/ShipInterior_NOM_Sandstone_mat.mat"),
        };
        Transform cockpitLight = SELocator.GetShipTransform().Find("Module_Cockpit/Lights_Cockpit/Pointlight_HEA_ShipCockpit");
        List<Material> materials = [.. cockpitLight.GetComponent<LightmapController>()._materials];
        materials.AddRange(newMaterials);
        cockpitLight.GetComponent<LightmapController>()._materials = [.. materials];

        _shipLoaded = true;
        UpdateSuitOxygen();

        if (AchievementsAPI != null)
        {
            SELocator.GetShipDamageController().OnShipComponentDamaged += ctx => CheckAllPartsDamaged();
            SELocator.GetShipDamageController().OnShipHullDamaged += ctx => CheckAllPartsDamaged();
        }

        if ((bool)Settings.disableHeadlights.GetProperty())
        {
            DisableHeadlights();
        }
        if ((bool)Settings.disableLandingCamera.GetProperty())
        {
            DisableLandingCamera();
        }
        bool coloredLights = (string)Settings.shipLightColor.GetProperty() != "Default";
        if ((bool)Settings.disableShipLights.GetProperty() || coloredLights)
        {
            Color lightColor = SettingsColors.GetLightingColor((string)Settings.shipLightColor.GetProperty());
            foreach (ElectricalSystem system in SELocator.GetShipBody().GetComponentsInChildren<ElectricalSystem>())
            {
                foreach (ElectricalComponent component in system._connectedComponents)
                {
                    if (component.gameObject.name.Contains("Pointlight") && component.TryGetComponent(out ShipLight light))
                    {
                        if ((bool)Settings.disableShipLights.GetProperty())
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
                            if ((string)Settings.shipLightColor.GetProperty() == "Rainbow")
                            {
                                light.gameObject.AddComponent<RainbowShipLight>();
                            }
                        }
                    }
                }
            }
            foreach (PulsingLight beacon in SELocator.GetShipBody().transform.Find("Module_Cabin/Lights_Cabin/ShipBeacon_Proxy").GetComponentsInChildren<PulsingLight>())
            {
                if ((bool)Settings.disableShipLights.GetProperty())
                {
                    PulsingLight.s_matPropBlock.SetColor(PulsingLight.s_propID_EmissionColor, beacon._initEmissionColor * 0f);
                    beacon._emissiveRenderer.SetPropertyBlock(PulsingLight.s_matPropBlock);
                    beacon.gameObject.SetActive(false);
                }
                else if (coloredLights)
                {
                    beacon.GetComponent<Light>().color = lightColor;
                    beacon._initEmissionColor = lightColor;
                    if ((string)Settings.shipLightColor.GetProperty() == "Rainbow")
                    {
                        beacon.gameObject.AddComponent<RainbowShipLight>();
                    }
                }
            }
        }
        if ((bool)Settings.disableShipOxygen.GetProperty())
        {
            SELocator.GetShipResources().SetOxygen(0f);
            oxygenDepleted = true;
            SELocator.GetShipTransform().Find("Module_Cockpit/Props_Cockpit/Props_HEA_ShipFoliage").gameObject.SetActive(false);
        }
        if ((bool)Settings.enableShipFuelTransfer.GetProperty())
        {
            GameObject transferVolume = LoadPrefab("Assets/ShipEnhancements/FuelTransferVolume.prefab");
            Instantiate(transferVolume, SELocator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>().transform);
        }
        if ((float)Settings.gravityMultiplier.GetProperty() != 1f && !(bool)Settings.disableGravityCrystal.GetProperty())
        {
            ShipDirectionalForceVolume shipGravity = SELocator.GetShipBody().GetComponentInChildren<ShipDirectionalForceVolume>();
            shipGravity._fieldMagnitude *= (float)Settings.gravityMultiplier.GetProperty();
        }
        if ((bool)Settings.enableAutoHatch.GetProperty() && !InMultiplayer)
        {
            GlobalMessenger.AddListener("EnterShip", OnEnterShip);
            GlobalMessenger.AddListener("ExitShip", OnExitShip);
            GameObject autoHatchController = LoadPrefab("Assets/ShipEnhancements/ExteriorHatchControls.prefab");
            Instantiate(autoHatchController, SELocator.GetShipBody().GetComponentInChildren<HatchController>().transform.parent);
        }
        if ((float)Settings.spaceAngularDragMultiplier.GetProperty() > 0 || (float)Settings.atmosphereAngularDragMultiplier.GetProperty() > 0)
        {
            ShipFluidDetector detector = SELocator.GetShipDetector().GetComponent<ShipFluidDetector>();
            detector.OnEnterFluid += OnEnterFluid;
            detector.OnExitFluid += OnExitFluid;
        }
        if ((string)Settings.gravityDirection.GetProperty() != "Down" && !(bool)Settings.disableGravityCrystal.GetProperty())
        {
            ShipDirectionalForceVolume shipGravity = SELocator.GetShipBody().GetComponentInChildren<ShipDirectionalForceVolume>();
            Vector3 direction = Vector3.down;
            switch ((string)Settings.gravityDirection.GetProperty())
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
        if ((bool)Settings.enableManualScoutRecall.GetProperty() || (bool)Settings.disableScoutRecall.GetProperty() || (bool)Settings.disableScoutLaunching.GetProperty())
        {
            ShipProbeLauncherEffects launcherEffects = SELocator.GetShipBody().GetComponentInChildren<PlayerProbeLauncher>()
                .gameObject.AddComponent<ShipProbeLauncherEffects>();
            if ((bool)Settings.enableManualScoutRecall.GetProperty())
            {
                GameObject probePickupVolume = LoadPrefab("Assets/ShipEnhancements/PlayerProbePickupVolume.prefab");
                Instantiate(probePickupVolume, SELocator.GetProbe().transform);
            }
            GameObject shipProbePickupVolume = LoadPrefab("Assets/ShipEnhancements/ShipProbePickupVolume.prefab");
            GameObject shipProbeVolume = Instantiate(shipProbePickupVolume, launcherEffects.transform);

            if ((bool)Settings.enableScoutLauncherComponent.GetProperty())
            {
                SELocator.GetShipBody().GetComponentInChildren<ProbeLauncherComponent>().SetProbeLauncherEffects(launcherEffects);
            }
        }
        if ((bool)Settings.disableScoutRecall.GetProperty() && (bool)Settings.disableScoutLaunching.GetProperty() && (bool)Settings.enableScoutLauncherComponent.GetProperty())
        {
            SELocator.GetProbeLauncherComponent()._repairReceiver.repairDistance = 0f;
            SELocator.GetProbeLauncherComponent()._damaged = true;
            SELocator.GetProbeLauncherComponent()._repairFraction = 0f;
            SELocator.GetProbeLauncherComponent().OnComponentDamaged();
        }
        if ((bool)Settings.addPortableCampfire.GetProperty())
        {
            Transform suppliesParent = SELocator.GetShipTransform().Find("Module_Supplies");
            GameObject portableCampfireSocket = LoadPrefab("Assets/ShipEnhancements/PortableCampfireSocket.prefab");
            PortableCampfireSocket campfireSocket = Instantiate(portableCampfireSocket, suppliesParent).GetComponent<PortableCampfireSocket>();
            /*GameObject portableCampfireItem = LoadPrefab("assets/ShipEnhancements/PortableCampfireItem.prefab");
            AssetBundleUtilities.ReplaceShaders(portableCampfireItem);
            PortableCampfireItem campfireItem = Instantiate(portableCampfireItem, suppliesParent).GetComponent<PortableCampfireItem>();
            campfireSocket.SetCampfireItem(campfireItem);*/
        }
        if (Settings.temperatureZonesAmount.GetProperty().ToString() != "None")
        {
            SELocator.GetShipBody().GetComponentInChildren<ShipFuelGauge>().gameObject.AddComponent<ShipTemperatureGauge>();
            GameObject hullTempDial = LoadPrefab("Assets/ShipEnhancements/ShipTempDial.prefab");
            Instantiate(hullTempDial, SELocator.GetShipTransform().Find("Module_Cockpit"));

            if (Settings.temperatureZonesAmount.GetProperty().ToString() == "Sun")
            {
                SpawnSunTemperatureZones();
            }
            else
            {
                AddTemperatureZones();
            }
        }
        if ((float)Settings.shipExplosionMultiplier.GetProperty() != 1f)
        {
            Transform effectsTransform = SELocator.GetShipTransform().Find("Effects");
            ExplosionController explosion = effectsTransform.GetComponentInChildren<ExplosionController>();

            if ((float)Settings.shipExplosionMultiplier.GetProperty() < 0f)
            {
                GameObject newExplosion = LoadPrefab("Assets/ShipEnhancements/BlackHoleExplosion.prefab");
                AssetBundleUtilities.ReplaceShaders(newExplosion);
                GameObject newExplosionObj = Instantiate(newExplosion, effectsTransform);
                SELocator.GetShipDamageController()._explosion = newExplosionObj.GetComponent<ExplosionController>();
                Destroy(explosion.gameObject);
            }
            else if ((float)Settings.shipExplosionMultiplier.GetProperty() > 0f)
            {
                SetupExplosion(effectsTransform, explosion);
            }
        }
        if ((float)Settings.shipBounciness.GetProperty() > 1f || (string)Settings.temperatureZonesAmount.GetProperty() != "None")
        {
            SELocator.GetShipTransform().gameObject.AddComponent<ModifiedShipHull>();
        }
        if ((bool)Settings.enablePersistentInput.GetProperty())
        {
            SELocator.GetShipBody().gameObject.AddComponent<ShipPersistentInput>();
        }
        if ((float)Settings.shipInputLatency.GetProperty() != 0f)
        {
            InputLatencyController.Initialize();
        }
        if ((bool)Settings.hotThrusters.GetProperty() || (string)Settings.thrusterColor.GetProperty() != "Default")
        {
            GameObject flameHazardVolume = LoadPrefab("Assets/ShipEnhancements/FlameHeatVolume.prefab");
            foreach (ThrusterFlameController flame in SELocator.GetShipTransform().GetComponentsInChildren<ThrusterFlameController>())
            {
                GameObject volume = Instantiate(flameHazardVolume, Vector3.zero, Quaternion.identity, flame.transform);
                volume.transform.localPosition = Vector3.zero;
                volume.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                if (!flame.enabled)
                {
                    flame.transform.localScale = Vector3.zero;
                }

                string color = (string)Settings.thrusterColor.GetProperty();

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
                    Color newColor = emissiveColor.Item1 * Mathf.Pow(2, emissiveColor.Item2);
                    newColor.a = alpha;
                    rend.material.SetColor("_Color", newColor);

                    Light light = flame.GetComponentInChildren<Light>();
                    light.color = emissiveColor.Item3;

                    ThrustIndicatorManager.SetColor(SettingsColors.GetIndicatorColor(color));
                }
            }
        }
        if ((float)Settings.reactorLifetimeMultiplier.GetProperty() != 1f)
        {
            ShipReactorComponent reactor = SELocator.GetShipTransform().GetComponentInChildren<ShipReactorComponent>();

            float multiplier = Mathf.Max((float)Settings.reactorLifetimeMultiplier.GetProperty(), 0f);
            reactor._minCountdown *= multiplier;
            reactor._maxCountdown *= multiplier;
        }

        SetHullColor();

        if ((bool)Settings.addTether.GetProperty())
        {
            /*GameObject hook = LoadPrefab("Assets/ShipEnhancements/TetherHook.prefab");
            AssetBundleUtilities.ReplaceShaders(hook);*/

            GameObject socketParent = Instantiate(LoadPrefab("Assets/ShipEnhancements/HookSocketParent.prefab"), SELocator.GetShipTransform());
            socketParent.transform.localPosition = Vector3.zero;
            /*foreach (TetherHookSocket socket in socketParent.GetComponentsInChildren<TetherHookSocket>())
            {
                GameObject hookItem = Instantiate(hook);
                socket.PlaceIntoSocket(hookItem.GetComponent<TetherHookItem>());
            }*/

            SELocator.GetPlayerBody().gameObject.AddComponent<TetherPromptController>();
        }
        if ((bool)Settings.extraEjectButtons.GetProperty())
        {
            GameObject suppliesButton = LoadPrefab("Assets/ShipEnhancements/SuppliesEjectButton.prefab");
            AssetBundleUtilities.ReplaceShaders(suppliesButton);
            Instantiate(suppliesButton, SELocator.GetShipTransform().Find("Module_Cabin"));

            GameObject engineButton = LoadPrefab("Assets/ShipEnhancements/EngineEjectButton.prefab");
            AssetBundleUtilities.ReplaceShaders(engineButton);
            Instantiate(engineButton, SELocator.GetShipTransform().Find("Module_Cabin"));

            GameObject landingGearButton = LoadPrefab("Assets/ShipEnhancements/LandingGearEjectButton.prefab");
            AssetBundleUtilities.ReplaceShaders(landingGearButton);
            Instantiate(landingGearButton, SELocator.GetShipTransform().Find("Module_Cabin"));
        }
        if ((bool)Settings.addShipSignal.GetProperty())
        {
            GameObject signal = LoadPrefab("Assets/ShipEnhancements/ShipSignal.prefab");
            Instantiate(signal, SELocator.GetShipTransform().GetComponentInChildren<ShipCockpitUI>()._sigScopeDish);

            SELocator.GetPlayerBody().GetComponentInChildren<Signalscope>().gameObject.AddComponent<ShipRemoteControl>();
        }
        bool physicsBounce = (float)Settings.shipBounciness.GetProperty() > 0f && (float)Settings.shipBounciness.GetProperty() <= 1f;
        if ((float)Settings.shipFriction.GetProperty() != 0.5f || physicsBounce)
        {
            bool both = (float)Settings.shipFriction.GetProperty() != 0.5f && physicsBounce;
            PhysicMaterial mat;
            if (both)
            {
                float friction;
                if ((float)Settings.shipFriction.GetProperty() < 0.5f)
                {
                    friction = Mathf.Lerp(0f, 0.6f, (float)Settings.shipFriction.GetProperty() * 2f);
                }
                else
                {
                    friction = Mathf.Lerp(0.6f, 1f, ((float)Settings.shipFriction.GetProperty() - 0.5f) * 2f);
                }

                mat = (PhysicMaterial)LoadAsset("Assets/ShipEnhancements/FrictionlessBouncyShip.physicMaterial");
                mat.dynamicFriction = friction;
                mat.staticFriction = friction;
                mat.bounciness = (float)Settings.shipBounciness.GetProperty();
            }
            else if (physicsBounce)
            {
                mat = (PhysicMaterial)LoadAsset("Assets/ShipEnhancements/BouncyShip.physicMaterial");
                mat.bounciness = (float)Settings.shipBounciness.GetProperty();
            }
            else
            {
                float friction;
                if ((float)Settings.shipFriction.GetProperty() < 0.5f)
                {
                    friction = Mathf.Lerp(0f, 0.6f, (float)Settings.shipFriction.GetProperty() * 2f);
                }
                else
                {
                    friction = Mathf.Lerp(0.6f, 1f, ((float)Settings.shipFriction.GetProperty() - 0.5f) * 2f);
                }

                mat = (PhysicMaterial)LoadAsset("Assets/ShipEnhancements/FrictionlessShip.physicMaterial");
                mat.dynamicFriction = friction;
                mat.staticFriction = friction;
            }

            foreach (Collider collider in SELocator.GetShipTransform().GetComponentsInChildren<Collider>(true))
            {
                collider.material = mat;
            }
        }
        if ((float)Settings.rustLevel.GetProperty() > 0f || ((float)Settings.dirtAccumulationTime.GetProperty() > 0f
            && (float)Settings.maxDirtAccumulation.GetProperty() > 0f))
        {
            GameObject rustController = LoadPrefab("Assets/ShipEnhancements/RustController.prefab");
            AssetBundleUtilities.ReplaceShaders(rustController);
            Instantiate(rustController, SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry"));
        }
        if ((bool)Settings.addPortableTractorBeam.GetProperty())
        {
            /*GameObject tractor = LoadPrefab("Assets/ShipEnhancements/PortableTractorBeamItem.prefab");
            AssetBundleUtilities.ReplaceShaders(tractor);
            GameObject tractorObj = Instantiate(tractor);*/
            GameObject tractorSocket = LoadPrefab("Assets/ShipEnhancements/PortableTractorBeamSocket.prefab");
            GameObject tractorSocketObj = Instantiate(tractorSocket, SELocator.GetShipTransform().Find("Module_Cabin"));
            //tractorSocketObj.GetComponent<PortableTractorBeamSocket>().PlaceIntoSocket(tractorObj.GetComponent<PortableTractorBeamItem>());
        }
        if ((bool)Settings.addExpeditionFlag.GetProperty())
        {
            SELocator.GetShipTransform().GetComponentInChildren<Minimap>().gameObject.AddComponent<MinimapFlagController>();
            SELocator.GetPlayerBody().GetComponentInChildren<Minimap>().gameObject.AddComponent<MinimapFlagController>();

            /*GameObject flag = LoadPrefab("Assets/ShipEnhancements/ExpeditionFlagItem.prefab");
            AssetBundleUtilities.ReplaceShaders(flag);
            GameObject flagObj = Instantiate(flag);*/
            GameObject flagSocket = LoadPrefab("Assets/ShipEnhancements/ExpeditionFlagSocket.prefab");
            GameObject flagSocketObj = Instantiate(flagSocket, SELocator.GetShipTransform().Find("Module_Cabin"));
            //flagSocketObj.GetComponent<ExpeditionFlagSocket>().PlaceIntoSocket(flagObj.GetComponent<ExpeditionFlagItem>());
        }
        if ((bool)Settings.addFuelCanister.GetProperty())
        {
            // Remove marshmallows from cabin to make room for canister
            MeshFilter rend = LoadPrefab("Assets/ShipEnhancements/CabinFuelTankReplacement.fbx").GetComponent<MeshFilter>();
            MeshFilter targetRend = SELocator.GetShipTransform()
                .Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Interior/Cabin_Interior 1/Cabin_Interior 1_MeshPart0")
                .GetComponent<MeshFilter>();
            targetRend.mesh = rend.mesh;

            Mesh shadowMesh = LoadPrefab("Assets/ShipEnhancements/AltShadowCasters/Shadowcaster_Cabin_NoMallows.fbx").GetComponent<MeshFilter>().mesh;
            SELocator.GetShipTransform().Find("Module_Cabin/Geo_Cabin/Shadowcaster_Cabin").GetComponent<MeshFilter>().mesh = shadowMesh;

            /*GameObject tank = LoadPrefab("Assets/ShipEnhancements/FuelTankItem.prefab");
            AssetBundleUtilities.ReplaceShaders(tank);
            GameObject tankObj = Instantiate(tank);*/
            GameObject tankSocket = LoadPrefab("Assets/ShipEnhancements/FuelTankSocket.prefab");
            GameObject tankSocketObj = Instantiate(tankSocket, SELocator.GetShipTransform().Find("Module_Cabin"));
            //tankSocketObj.GetComponent<FuelTankItemSocket>().PlaceIntoSocket(tankObj.GetComponent<FuelTankItem>());
        }
        if ((bool)Settings.singleUseTractorBeam.GetProperty())
        {
            SELocator.GetShipTransform().GetComponentInChildren<ShipTractorBeamSwitch>()._functional = false;
        }
        if ((bool)Settings.addShipWarpCore.GetProperty() && !(bool)Settings.shipWarpCoreComponent.GetProperty())
        {
            GameObject core = LoadPrefab("Assets/ShipEnhancements/ShipWarpCore.prefab");
            AssetBundleUtilities.ReplaceShaders(core);
            core.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = SELocator.GetShipBody().gameObject;
            GameObject coreObj = Instantiate(core, SELocator.GetShipTransform().Find("Module_Cockpit"));

            if (NHAPI == null && GameObject.Find("TimberHearth_Body"))
            {
                GameObject receiver = LoadPrefab("Assets/ShipEnhancements/ShipWarpReceiver.prefab");
                AssetBundleUtilities.ReplaceShaders(receiver);
                receiver.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = SELocator.GetShipBody().gameObject;
                GameObject receiverObj = Instantiate(receiver, GameObject.Find("TimberHearth_Body").transform);
                coreObj.GetComponent<ShipWarpCoreController>().SetReceiver(receiverObj.GetComponent<ShipWarpCoreReceiver>());
            }
            else if (NHAPI != null && !_unsubFromShipSpawn)
            {
                NHAPI.GetStarSystemLoadedEvent().AddListener(SetCustomWarpDestination);
                _unsubFromShipSpawn = true;
            }

            // add when NH assembly created

            /*ModHelper.Events.Unity.FireInNUpdates(() =>
            {
                if (GameObject.Find("TimberHearth_Body"))
                {
                    GameObject receiver = LoadPrefab("Assets/ShipEnhancements/ShipWarpReceiver.prefab");
                    AssetBundleUtilities.ReplaceShaders(receiver);
                    receiver.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = SELocator.GetShipBody().gameObject;
                    GameObject receiverObj = Instantiate(receiver, GameObject.Find("TimberHearth_Body").transform);
                    coreObj.GetComponent<ShipWarpCoreController>().SetReceiver(receiverObj.GetComponent<ShipWarpCoreReceiver>());
                }
                else if (false)
                {
                    WriteDebugMessage("Custom spawn");
                    GameObject receiver = LoadPrefab("Assets/ShipEnhancements/ShipWarpReceiver.prefab");
                    AssetBundleUtilities.ReplaceShaders(receiver);
                    receiver.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = SELocator.GetShipBody().gameObject;
                    ShipWarpCoreReceiver receiverObj = Instantiate(receiver.gameObject,
                        spawner.GetInitialSpawnPoint()._attachedBody?.transform).GetComponent<ShipWarpCoreReceiver>();
                    receiverObj.OnCustomSpawnPoint();
                    coreObj.GetComponent<ShipWarpCoreController>().SetReceiver(receiverObj);
                }
            }, 5);*/
        }
        if ((float)Settings.repairTimeMultiplier.GetProperty() != 1f
            && (float)Settings.repairTimeMultiplier.GetProperty() != 0f)
        {
            foreach (ShipComponent component in SELocator.GetShipTransform().GetComponentsInChildren<ShipComponent>())
            {
                component._repairTime *= (float)Settings.repairTimeMultiplier.GetProperty();
            }
            foreach (ShipHull hull in SELocator.GetShipTransform().GetComponentsInChildren<ShipHull>())
            {
                hull._repairTime *= (float)Settings.repairTimeMultiplier.GetProperty();
            }
        }
        if ((bool)Settings.addShipClock.GetProperty())
        {
            GameObject clock = LoadPrefab("Assets/ShipEnhancements/ShipClock.prefab");
            AssetBundleUtilities.ReplaceShaders(clock);
            Instantiate(clock, SELocator.GetShipTransform().Find("Module_Cockpit"));
        }
        if ((bool)Settings.enableRepairConfirmation.GetProperty() || (bool)Settings.enableFragileShip.GetProperty())
        {
            GameObject audio = LoadPrefab("Assets/ShipEnhancements/SystemOnlineAudio.prefab");
            Instantiate(audio, SELocator.GetShipTransform().Find("Audio_Ship"));

            foreach (ShipHull hull in SELocator.GetShipDamageController()._shipHulls)
            {
                if (hull != null)
                {
                    hull.OnDamaged += ctx => CheckNoPartsDamaged();
                    hull.OnRepaired += ctx => CheckNoPartsDamaged();
                }
            }
            foreach (ShipComponent component in SELocator.GetShipDamageController()._shipComponents)
            {
                if (component != null)
                {
                    component.OnDamaged += ctx => CheckNoPartsDamaged();
                    component.OnRepaired += ctx => CheckNoPartsDamaged();
                }
            }
        }
        if ((bool)Settings.enableRemovableGravityCrystal.GetProperty())
        {
            Transform crystalParent = SELocator.GetShipTransform().Find("Module_Engine/Geo_Engine/Engine_Tech_Interior");
            GameObject obj1 = crystalParent.Find("Props_NOM_GravityCrystal").gameObject;
            GameObject obj2 = crystalParent.Find("Props_NOM_GravityCrystal_Base").gameObject;

            GameObject crystal = LoadPrefab("Assets/ShipEnhancements/GravityCrystalItem.prefab");
            AssetBundleUtilities.ReplaceShaders(crystal);
            ShipGravityCrystalItem item = Instantiate(crystal).GetComponent<ShipGravityCrystalItem>();

            GameObject crystalSocket = LoadPrefab("Assets/ShipEnhancements/GravityCrystalSocket.prefab");
            AssetBundleUtilities.ReplaceShaders(crystalSocket);
            ShipGravityCrystalSocket socket = Instantiate(crystalSocket, SELocator.GetShipTransform().Find("Module_Engine")).GetComponent<ShipGravityCrystalSocket>();

            socket.AddComponentMeshes([obj1, obj2]);
            socket.PlaceIntoSocket(item);
        }
        if ((bool)Settings.extraNoise.GetProperty())
        {
            GlobalMessenger.AddListener("StartShipIgnition", OnStartShipIgnition);
            GlobalMessenger.AddListener("CancelShipIgnition", OnStopShipIgnition);
            GlobalMessenger.AddListener("CompleteShipIgnition", OnStopShipIgnition);
        }
        if ((bool)Settings.addErnesto.GetProperty())
        {
            GameObject ernesto = LoadPrefab("Assets/ShipEnhancements/Ernesto.prefab");
            AssetBundleUtilities.ReplaceShaders(ernesto);
            GameObject ernestoObj = Instantiate(ernesto, SELocator.GetShipBody().transform.Find("Module_Cockpit"));
            var font = (Font)Resources.Load(@"fonts\english - latin\HVD Fonts - BrandonGrotesque-Bold_Dynamic");
            if (font != null)
            {
                ernestoObj.GetComponentInChildren<UnityEngine.UI.Text>().font = font;
            }
            DialogueBuilder.FixCustomDialogue(ernestoObj, "ConversationZone");
        }
        if ((int)(float)Settings.repairLimit.GetProperty() >= 0)
        {
            ShipRepairLimitController.SetRepairLimit((int)(float)Settings.repairLimit.GetProperty());
        }
        if ((bool)Settings.addShipCurtain.GetProperty())
        {
            MeshFilter rend;
            if ((bool)Settings.addFuelCanister.GetProperty())
            {
                rend = LoadPrefab("Assets/ShipEnhancements/CurtainTankCabinReplacement.prefab").GetComponent<MeshFilter>();

                Mesh shadowMesh = LoadPrefab("Assets/ShipEnhancements/AltShadowCasters/Shadowcaster_Cabin_NoMallows.fbx").GetComponent<MeshFilter>().mesh;
                SELocator.GetShipTransform().Find("Module_Cabin/Geo_Cabin/Shadowcaster_Cabin").GetComponent<MeshFilter>().mesh = shadowMesh;
            }
            else
            {
                rend = LoadPrefab("Assets/ShipEnhancements/CurtainCabinReplacement.prefab").GetComponent<MeshFilter>();
            }
            MeshFilter targetRend = SELocator.GetShipTransform()
                .Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Interior/Cabin_Interior 1/Cabin_Interior 1_MeshPart0")
                .GetComponent<MeshFilter>();
            targetRend.mesh = rend.mesh;

            GameObject curtainObj = LoadPrefab("Assets/ShipEnhancements/ShipCurtains.prefab");
            AssetBundleUtilities.ReplaceShaders(curtainObj);
            Instantiate(curtainObj, SELocator.GetShipTransform().Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Interior"));
        }
        if ((bool)Settings.addRepairWrench.GetProperty())
        {
            GameObject wrenchSocketObj = LoadPrefab("Assets/ShipEnhancements/RepairWrenchSocket.prefab");
            RepairWrenchSocket wrenchSocket = Instantiate(wrenchSocketObj,
                SELocator.GetShipTransform().Find("Module_Cockpit")).GetComponent<RepairWrenchSocket>();

            /*GameObject wrenchObj = LoadPrefab("Assets/ShipEnhancements/RepairWrenchItem.prefab");
            AssetBundleUtilities.ReplaceShaders(wrenchObj);
            RepairWrenchItem wrench = Instantiate(wrenchObj).GetComponent<RepairWrenchItem>();
            wrenchSocket.PlaceIntoSocket(wrench);*/
        }
        if ((bool)Settings.addRadio.GetProperty())
        {
            GameObject radioSocketObj = LoadPrefab("Assets/ShipEnhancements/RadioItemSocket.prefab");
            RadioItemSocket radioSocket = Instantiate(radioSocketObj,
                SELocator.GetShipTransform().Find("Module_Cockpit")).GetComponent<RadioItemSocket>();

            /*GameObject radioObj = LoadPrefab("Assets/ShipEnhancements/RadioItem.prefab");
            AssetBundleUtilities.ReplaceShaders(radioObj);
            RadioItem radio = Instantiate(radioObj).GetComponent<RadioItem>();
            radioSocket.PlaceIntoSocket(radio);*/

            GameObject codeNotesObj = LoadPrefab("Assets/ShipEnhancements/CodeNotes.prefab");
            AssetBundleUtilities.ReplaceShaders(codeNotesObj);
            Instantiate(codeNotesObj, SELocator.GetShipTransform().Find("Module_Cockpit"));

            AddRadioCodeZones();
        }
        if ((bool)Settings.disableFluidPrevention.GetProperty())
        {
            GameObject[] stencils = SELocator.GetShipDamageController()._stencils;
            for (int j = 0; j < stencils.Length; j++)
            {
                stencils[j].SetActive(false);
            }

            Transform atmoVolume = SELocator.GetShipTransform().Find("Volumes/ShipAtmosphereVolume");
            atmoVolume.GetComponent<FluidVolume>().SetPriority(0);
        }
        if ((bool)Settings.disableRotationSpeedLimit.GetProperty())
        {
            SELocator.GetPlayerBody().GetComponentInChildren<PlayerCameraEffectController>().gameObject.AddComponent<PlayerTorpidityEffect>();
        }
        if ((float)Settings.waterDamage.GetProperty() > 0f
            || (float)Settings.sandDamage.GetProperty() > 0f)
        {
            GameObject fluidDamage = LoadPrefab("Assets/ShipEnhancements/ShipFluidDamageController.prefab");
            Instantiate(fluidDamage, SELocator.GetShipTransform());
        }
        if ((bool)Settings.disableMinimapMarkers.GetProperty())
        {
            Minimap playerMinimap = SELocator.GetPlayerBody().GetComponentInChildren<Minimap>();
            playerMinimap._playerTrailRenderer.enabled = false;
            playerMinimap._probeTrailRenderer.enabled = false;
            foreach (Renderer renderer in playerMinimap._minimapRenderersToSwitchOnOff)
            {
                if (renderer.transform.parent != playerMinimap._globeMeshTransform)
                {
                    renderer.enabled = false;
                }
            }

            Minimap shipMinimap = SELocator.GetShipBody().GetComponentInChildren<Minimap>();
            shipMinimap._playerTrailRenderer.enabled = false;
            shipMinimap._probeTrailRenderer.enabled = false;
            foreach (Renderer renderer in shipMinimap._minimapRenderersToSwitchOnOff)
            {
                renderer.enabled = false;
            }
        }

        SetDamageColors();

        engineOn = !(bool)Settings.addEngineSwitch.GetProperty();

        if (InMultiplayer && !QSBAPI.GetIsHost() && QSBCompat.NeverInitialized())
        {
            foreach (uint id in PlayerIDs)
            {
                QSBCompat.SendInitializedShip(id);
            }
        }

        ShipNotifications.Initialize();
        SELocator.LateInitialize();

        ModHelper.Events.Unity.RunWhen(() => Locator._shipBody != null, () =>
        {
            _shipLoaded = true;
            if ((bool)Settings.disableGravityCrystal.GetProperty())
            {
                DisableGravityCrystal();
            }
            if ((bool)Settings.enableJetpackRefuelDrain.GetProperty())
            {
                if (InMultiplayer)
                {
                    QSBInteraction.GetShipRecoveryPoint().AddComponent<ShipRefuelDrain>();
                }
                else
                {
                    SELocator.GetShipBody().GetComponentInChildren<PlayerRecoveryPoint>().gameObject.AddComponent<ShipRefuelDrain>();
                }
            }
            if ((bool)Settings.disableEjectButton.GetProperty())
            {
                SELocator.GetShipBody().GetComponentInChildren<ShipEjectionSystem>().GetComponent<InteractReceiver>().DisableInteraction();
                GameObject ejectButtonTape = LoadPrefab("Assets/ShipEnhancements/EjectButtonTape.prefab");
                AssetBundleUtilities.ReplaceShaders(ejectButtonTape);
                Instantiate(ejectButtonTape, SELocator.GetShipBody().transform.Find("Module_Cockpit/Geo_Cockpit"));
            }
            if ((bool)Settings.disableShipSuit.GetProperty())
            {
                SuitPickupVolume pickupVolume = SELocator.GetShipTransform().GetComponentInChildren<SuitPickupVolume>();
                pickupVolume._containsSuit = false;
                pickupVolume._allowSuitReturn = false;
                pickupVolume._interactVolume.EnableSingleInteraction(false, pickupVolume._pickupSuitCommandIndex);
                pickupVolume._suitGeometry.SetActive(false);
                pickupVolume._suitOWCollider.SetActivation(false);
                foreach (GameObject tool in pickupVolume._toolGeometry)
                {
                    tool.SetActive(false);
                }
            }
            if ((float)Settings.randomHullDamage.GetProperty() > 0f && (!InMultiplayer || QSBAPI.GetIsHost()))
            {
                float lerp = (float)Settings.randomHullDamage.GetProperty();
                List<ShipHull> hulls = [.. SELocator.GetShipDamageController()._shipHulls];
                int iterations = (int)(lerp * hulls.Count + 0.1f);
                for (int i = 0; i < iterations; i++)
                {
                    int index = UnityEngine.Random.Range(0, hulls.Count);
                    float damage = UnityEngine.Random.Range(Mathf.Lerp(0f, 0.4f, lerp), Mathf.Lerp(0f, 0.8f, lerp));
                    ShipHull hull = hulls[index];

                    hull._integrity -= damage;
                    if (!hull._damaged)
                    {
                        hull._damaged = true;

                        var eventDelegate = (MulticastDelegate)typeof(ShipHull).GetField("OnDamaged", BindingFlags.Instance
                            | BindingFlags.NonPublic | BindingFlags.Public).GetValue(hull);
                        if (eventDelegate != null)
                        {
                            foreach (var handler in eventDelegate.GetInvocationList())
                            {
                                handler.Method.Invoke(handler.Target, [hull]);
                            }
                        }
                    }
                    if (hull._damageEffect != null)
                    {
                        hull._damageEffect.SetEffectBlend(1f - hull._integrity);
                    }

                    // Just in case it does 1 or more damage somehow
                    if (hull.shipModule is ShipDetachableModule)
                    {
                        ShipDetachableModule module = hull.shipModule as ShipDetachableModule;
                        if (hull.integrity <= 0f && !module.isDetached)
                        {
                            module.Detach();
                        }
                    }
                    else if (hull.shipModule is ShipLandingModule)
                    {
                        ShipLandingModule module = hull.shipModule as ShipLandingModule;
                        if (hull.integrity <= 0f)
                        {
                            module.DetachAllLegs();
                        }
                    }

                    hulls.Remove(hull);
                    anyPartDamaged = true;
                }
            }
            if ((float)Settings.randomComponentDamage.GetProperty() > 0f && (!InMultiplayer || QSBAPI.GetIsHost()))
            {
                Type[] invalidTypes = [typeof(ShipReactorComponent), typeof(ShipFuelTankComponent)];
                List<ShipComponent> components = [];
                bool seenThruster = false;
                bool addedThruster = false;
                foreach (ShipComponent shipComponent in SELocator.GetShipDamageController()._shipComponents)
                {
                    if (invalidTypes.Contains(shipComponent.GetType()))
                    {
                        continue;
                    }

                    if (shipComponent is ShipThrusterComponent)
                    {
                        if (!addedThruster && (seenThruster || UnityEngine.Random.value < 0.5f))
                        {
                            addedThruster = true;
                            seenThruster = true;
                        }
                        else
                        {
                            seenThruster = true;
                            continue;
                        }
                    }

                    components.Add(shipComponent);
                }

                float lerp = (float)Settings.randomHullDamage.GetProperty();
                int iterations = (int)(lerp * components.Count + 0.1f);
                for (int i = 0; i < iterations; i++)
                {
                    int index = UnityEngine.Random.Range(0, components.Count);
                    components[index].SetDamaged(true);
                    components.RemoveAt(index);
                    anyPartDamaged = true;
                }
            }
            if ((bool)Settings.preventSystemFailure.GetProperty())
            {
                GameObject entrywayTriggersObj = LoadPrefab("Assets/ShipEnhancements/BreachEntryTriggers.prefab");
                OWTriggerVolume entrywayVol = Instantiate(entrywayTriggersObj, SELocator.GetShipTransform().Find("Volumes")).GetComponent<OWTriggerVolume>();

                PlayerSpawner spawner = GameObject.FindGameObjectWithTag("Player").GetRequiredComponent<PlayerSpawner>();
                SpawnPoint shipSpawn = spawner.GetSpawnPoint(SpawnLocation.Ship);
                List<OWTriggerVolume> shipTriggers = [.. shipSpawn._triggerVolumes];
                shipTriggers.Add(entrywayVol);
                shipSpawn._triggerVolumes = [.. shipTriggers];
            }
            if ((bool)Settings.disableMinimapMarkers.GetProperty())
            {
                Minimap playerMinimap = GameObject.Find("SecondaryGroup/HUD_Minimap/Minimap_Root").GetComponent<Minimap>();
                for (int i = 0; i < playerMinimap.transform.childCount; i++)
                {
                    if (playerMinimap.transform.GetChild(i).name.Contains("PlayerMarker"))
                    {
                        playerMinimap.transform.GetChild(i).gameObject.SetActive(false);
                    }
                }
            }
            if ((!InMultiplayer || QSBAPI.GetIsHost()) && (float)Settings.shipDamageSpeedMultiplier.GetProperty() < 0f)
            {
                SELocator.GetShipDamageController().Explode();
            }

            InitializeConditions();
        });
    }

    private static void SetupExplosion(Transform effectsTransform, ExplosionController explosion)
    {
        if ((float)Settings.shipExplosionMultiplier.GetProperty() > 30f)
        {
            GameObject supernova = LoadPrefab("Assets/ShipEnhancements/ExplosionSupernova.prefab");
            AssetBundleUtilities.ReplaceShaders(supernova);
            GameObject supernovaObj = Instantiate(supernova, SELocator.GetShipTransform().Find("Module_Engine"));
            supernovaObj.SetActive(false);
            return;
        }
        else
        {
            explosion._length *= ((float)Settings.shipExplosionMultiplier.GetProperty() * 0.75f) + 0.25f;
            explosion._forceVolume._acceleration *= ((float)Settings.shipExplosionMultiplier.GetProperty() * 0.25f) + 0.75f;
            explosion.transform.localScale *= (float)Settings.shipExplosionMultiplier.GetProperty();
            explosion.GetComponent<SphereCollider>().radius = 0.1f;
            OWAudioSource audio = effectsTransform.Find("ExplosionAudioSource").GetComponent<OWAudioSource>();
            audio.maxDistance *= ((float)Settings.shipExplosionMultiplier.GetProperty() * 0.1f) + 0.9f;
            AnimationCurve curve = audio.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
            Keyframe[] newKeys = new Keyframe[curve.keys.Length];
            for (int i = 0; i < curve.keys.Length; i++)
            {
                newKeys[i] = curve.keys[i];
                newKeys[i].value *= ((float)Settings.shipExplosionMultiplier.GetProperty() * 0.1f) + 0.9f;
            }
            AnimationCurve newCurve = new();
            foreach (Keyframe key in newKeys)
            {
                newCurve.AddKey(key);
            }
            audio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, newCurve);
        }

        if ((bool)Settings.moreExplosionDamage.GetProperty())
        {
            GameObject damage = LoadPrefab("Assets/ShipEnhancements/ExplosionDamage.prefab");
            GameObject damageObj = Instantiate(damage, explosion.transform);
            damageObj.transform.localPosition = Vector3.zero;
            damageObj.transform.localScale = Vector3.one;
            ExplosionDamage explosionDamage = damageObj.GetComponent<ExplosionDamage>();
            explosionDamage.damageShip = false;
            explosionDamage.damageFragment = true;
            explosionDamage.unparent = false;
        }
    }

    private void SpawnSunTemperatureZones()
    {
        if (NHAPI != null && !_unsubFromSystemLoaded)
        {
            NHAPI.GetStarSystemLoadedEvent().AddListener(OnNHStarSystemLoaded);
            _unsubFromSystemLoaded = true;
        }
        else
        {
            GameObject sun = GameObject.Find("Sun_Body");
            if (sun != null)
            {
                GameObject sunTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Sun.prefab");
                Instantiate(sunTempZone, sun.transform.Find("Sector_SUN/Volumes_SUN"));
                GameObject supernovaTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Supernova.prefab");
                Instantiate(supernovaTempZone, sun.GetComponentInChildren<SupernovaEffectController>().transform);
            }
        }
    }

    private void AddTemperatureZones()
    {
        /*GameObject sun = GameObject.Find("Sun_Body");
        if (sun != null)
        {
            GameObject sunTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Sun.prefab");
            Instantiate(sunTempZone, sun.transform.Find("Sector_SUN/Volumes_SUN"));
            GameObject supernovaTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Supernova.prefab");
            Instantiate(supernovaTempZone, sun.GetComponentInChildren<SupernovaEffectController>().transform);
        }*/

        SpawnSunTemperatureZones();

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

        GameObject escapePodDimension = GameObject.Find("DB_EscapePodDimension_Body");
        if (escapePodDimension != null)
        {
            GameObject podDimensionTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_EscapePodDimension.prefab");
            Instantiate(podDimensionTempZone, escapePodDimension.transform.Find("Sector_EscapePodDimension"));
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

        GameObject brambleIsland = GameObject.Find("BrambleIsland_Body");
        if (brambleIsland != null)
        {
            GameObject brambleIslandTempZones = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_BrambleIsland.prefab");
            Instantiate(brambleIslandTempZones, brambleIsland.transform.Find("Sector_BrambleIsland"));
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
            GameObject thTempZone3 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_TimberHearthSurface.prefab");
            Instantiate(thTempZone3, th.transform.Find("Sector_TH"));
        }

        GameObject moon = GameObject.Find("Moon_Body");
        if (moon != null)
        {
            GameObject moonTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_AttlerockCrater.prefab");
            Instantiate(moonTempZone, moon.transform.Find("Sector_THM"));
        }

        GameObject ct = GameObject.Find("CaveTwin_Body");
        if (ct != null)
        {
            GameObject ctTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_CaveTwinHot.prefab");
            Instantiate(ctTempZone1, ct.transform.Find("Sector_CaveTwin"));
            GameObject ctTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_CaveTwinCold.prefab");
            Instantiate(ctTempZone2, ct.transform.Find("Sector_CaveTwin"));
        }

        GameObject whs = GameObject.Find("WhiteholeStationSuperstructure_Body");
        if (whs != null)
        {
            GameObject whsTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_WhiteHoleStation.prefab");
            Instantiate(whsTempZone, whs.transform);
        }

        Campfire[] campfires = FindObjectsOfType<Campfire>();
        if (campfires.Length > 0)
        {
            GameObject campfireTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Campfire.prefab");
            foreach (Campfire fire in campfires)
            {
                Instantiate(campfireTempZone, fire.transform.parent);
            }
        }
    }

    private void AddRadioCodeZones()
    {
        GameObject et = GameObject.Find("CaveTwin_Body");
        if (et != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_NomaiMeditation.prefab");
            Instantiate(zone, et.transform.Find("Sector_CaveTwin"));
        }

        GameObject th = GameObject.Find("TimberHearth_Body");
        if (th != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_HearthsShadow.prefab");
            Instantiate(zone, th.transform.Find("Sector_TH"));
        }

        GameObject ss = GameObject.Find("SunStation_Body");
        if (ss != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_NoTimeForCaution.prefab");
            Instantiate(zone, ss.transform.Find("Sector_SunStation"));
        }

        GameObject co = GameObject.Find("Comet_Body");
        if (co != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_RiversEndTimes.prefab");
            Instantiate(zone, co.transform.Find("Sector_CO"));
        }

        GameObject qm = GameObject.Find("QuantumMoon_Body");
        if (qm != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_LastDreamOfHome.prefab");
            Instantiate(zone, qm.transform.Find("Sector_QuantumMoon/State_EYE"));
        }

        GameObject vessel = GameObject.Find("DB_VesselDimension_Body");
        if (vessel != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_OlderThanTheUniverse.prefab");
            Instantiate(zone, vessel.transform.Find("Sector_VesselDimension"));
        }

        GameObject rw = GameObject.Find("RingWorld_Body");
        if (rw != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_ElegyForTheRings.prefab");
            Instantiate(zone, rw.transform.Find("Sector_RingInterior/Sector_Zone1/Sector_DreamFireHouse_Zone1"));
            Instantiate(zone, rw.transform.Find("Sector_RingInterior/Sector_Zone2/Sector_DreamFireLighthouse_Zone2_AnimRoot/Volumes_DreamFireLighthouse_Zone2"));
            Instantiate(zone, rw.transform.Find("Sector_RingInterior/Sector_Zone3/Sector_HiddenGorge/Sector_DreamFireHouse_Zone3"));
        }

        GameObject sun = GameObject.Find("Sun_Body");
        if (sun != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_TheSpiritOfWater.prefab");
            Instantiate(zone, sun.transform.Find("Sector_SUN/Volumes_SUN/SupernovaVolume"));
        }

        if (NHAPI != null)
        {
            NHAPI.GetBodyLoadedEvent().AddListener(OnNHBodyLoaded);
            _unsubFromBodyLoaded = true;
        }
    }

    private void OnNHBodyLoaded(string name)
    {
        if ((bool)Settings.addRadio.GetProperty() && name == "Egg Star")
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_Doom.prefab");
            Instantiate(zone, NHAPI.GetPlanet("Egg Star").transform);
        }
    }

    private void OnNHStarSystemLoaded(string name)
    {
        if ((string)Settings.temperatureZonesAmount.GetProperty() != "None")
        {
            GameObject sunTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Sun.prefab");

            SunController[] suns = FindObjectsOfType<SunController>();
            foreach (SunController sun in suns)
            {
                ShipEnhancements.WriteDebugMessage("sun found: " + sun.gameObject.name);
                if (sun.GetComponentInChildren<HeatHazardVolume>() && !sun.GetComponentInChildren<TemperatureZone>())
                {
                    ShipEnhancements.WriteDebugMessage("sun can support temp zone");
                    TemperatureZone zone = Instantiate(sunTempZone, sun.GetComponentInChildren<Sector>().transform).GetComponent<TemperatureZone>();
                    zone.transform.localPosition = Vector3.zero;
                    float sunScale = sun.GetComponentInChildren<TessellatedSphereRenderer>().transform.localScale.magnitude / 2;
                    zone.SetProperties(100f, sunScale * 2.25f, sunScale, false, 0f, 0f);
                }
            }

            NHInteraction.AddTempZoneToNHSuns(sunTempZone);
        }
    }

    private void SetCustomWarpDestination(string name)
    {
        if (name != "SolarSystem" || !GameObject.Find("TimberHearth_Body"))
        {
            (Transform transform, Vector3 offset) spawn = NHInteraction.GetShipSpawnPoint();
            if (spawn.transform == null) return;

            GameObject receiver = LoadPrefab("Assets/ShipEnhancements/ShipWarpReceiver.prefab");
            AssetBundleUtilities.ReplaceShaders(receiver);
            receiver.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = SELocator.GetShipBody().gameObject;
            ShipWarpCoreReceiver receiverObj = Instantiate(receiver, spawn.transform).GetComponent<ShipWarpCoreReceiver>();
            receiverObj.transform.localPosition = Vector3.zero;
            receiverObj.transform.localRotation = Quaternion.identity;
            receiverObj.OnCustomSpawnPoint();

            ShipWarpCoreController core = SELocator.GetShipTransform().GetComponentInChildren<ShipWarpCoreController>();
            core.SetReceiver(receiverObj);
        }
    }

    private void DisableHeadlights()
    {
        ShipHeadlightComponent headlightComponent = SELocator.GetShipBody().GetComponentInChildren<ShipHeadlightComponent>();
        headlightComponent._repairReceiver.repairDistance = 0f;
        headlightComponent._damaged = true;
        headlightComponent._repairFraction = 0f;
        headlightComponent.OnComponentDamaged();

        SELocator.GetShipBody().GetComponentInChildren<ShipCockpitController>()._externalLightsOn = false;
    }

    private void DisableGravityCrystal()
    {
        ShipGravityComponent gravityComponent = SELocator.GetShipBody().GetComponentInChildren<ShipGravityComponent>();
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
        ShipCameraComponent cameraComponent = SELocator.GetShipBody().GetComponentInChildren<ShipCameraComponent>();
        cameraComponent._repairReceiver.repairDistance = 0f;
        cameraComponent._damaged = true;
        cameraComponent._repairFraction = 0f;
        cameraComponent._landingCamera.SetDamaged(true);
    }

    private void SetHullColor()
    {
        MeshRenderer suppliesRenderer = SELocator.GetShipTransform().
            Find("Module_Supplies/Geo_Supplies/Supplies_Geometry/Supplies_Interior").GetComponent<MeshRenderer>();
        Material inSharedMat = suppliesRenderer.sharedMaterials[0];

        Transform buttonPanel = SELocator.GetShipTransform().GetComponentInChildren<CockpitButtonPanel>().transform;
        MeshRenderer buttonPanelRenderer = buttonPanel.Find("Panel/PanelBody.001").GetComponent<MeshRenderer>();
        Material inSharedMat2 = buttonPanelRenderer.sharedMaterials[0];
        if ((string)Settings.interiorHullColor.GetProperty() != "Default")
        {

            if ((string)Settings.interiorHullColor.GetProperty() == "Rainbow")
            {
                if (SELocator.GetShipTransform().TryGetComponent(out RainbowShipHull rainbowHull))
                {
                    rainbowHull.AddSharedMaterial(inSharedMat);
                    rainbowHull.AddSharedMaterial(inSharedMat2);
                }
                else
                {
                    rainbowHull = SELocator.GetShipTransform().gameObject.AddComponent<RainbowShipHull>();
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

        MeshRenderer cabinRenderer = SELocator.GetShipTransform().
            Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Exterior").GetComponent<MeshRenderer>();
        Material outSharedMat = cabinRenderer.sharedMaterials[3];
        if ((string)Settings.exteriorHullColor.GetProperty() != "Default")
        {
            if ((string)Settings.exteriorHullColor.GetProperty() == "Rainbow")
            {
                if (SELocator.GetShipTransform().TryGetComponent(out RainbowShipHull rainbowHull))
                {
                    rainbowHull.AddSharedMaterial(outSharedMat);
                }
                else
                {
                    rainbowHull = SELocator.GetShipTransform().gameObject.AddComponent<RainbowShipHull>();
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

    private void SetDamageColors()
    {
        string color = (string)Settings.damageIndicatorColor.GetProperty();
        if (color != "Default")
        {
            if (color == "Rainbow")
            {
                SELocator.GetShipTransform().gameObject.AddComponent<RainbowShipDamage>();
                return;
            }

            var damageScreenMat = SELocator.GetShipTransform().Find("Module_Cockpit/Systems_Cockpit/ShipCockpitUI/DamageScreen/HUD_ShipDamageDisplay")
                .GetComponent<MeshRenderer>().material;
            var masterAlarmMat = SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Interior/Cockpit_Interior_Chassis")
                .GetComponent<MeshRenderer>().sharedMaterials[6];
            var masterAlarmLight = SELocator.GetShipTransform().Find("Module_Cabin/Lights_Cabin/PointLight_HEA_MasterAlarm").GetComponent<Light>();

            var (newHull, newComponent, newAlarm, newAlarmLit, newLight) = SettingsColors.GetDamageColor(color);

            damageScreenMat.SetColor("_DamagedHullFill", newHull);
            damageScreenMat.SetColor("_DamagedComponentFill", newComponent);

            if (color != "Outer Wilds Beta")
            {
                masterAlarmMat.SetColor("_Color", newAlarm);
                SELocator.GetShipTransform().GetComponentInChildren<ShipCockpitUI>()._damageLightColor = newAlarmLit;
                masterAlarmLight.color = newLight;

                foreach (DamageEffect effect in SELocator.GetShipTransform().GetComponentsInChildren<DamageEffect>())
                {
                    if (effect._damageLight)
                    {
                        effect._damageLight.GetLight().color = newLight;
                    }
                    if (effect._damageLightRenderer)
                    {
                        effect._damageLightRenderer.SetColor(newAlarm);
                        effect._damageLightRendererColor = newAlarmLit;
                    }
                }
            }
        }
    }

    private void InitializeConditions()
    {
        if ((bool)Settings.enableManualScoutRecall.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_MANUAL_RECALL_ENALBED", true);
        }
        if ((float)Settings.rustLevel.GetProperty() > 0f)
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_COCKPIT_RUST", true);
            if ((float)Settings.rustLevel.GetProperty() > 0.75f)
            {
                DialogueConditionManager.SharedInstance.SetConditionState("SE_MAX_COCKPIT_RUST", true);
            }
        }
        if ((string)Settings.temperatureZonesAmount.GetProperty() != "None")
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_TEMPERATURE_ENABLED", true);
        }
        if ((string)Settings.disableThrusters.GetProperty() == "Backward"
            || (string)Settings.disableThrusters.GetProperty() == "All Except Forward")
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_RETRO_ROCKETS_DISABLED", true);
        }
        if (_currentPreset == SettingsPresets.PresetName.Random)
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_USING_RANDOM_PRESET", true);
        }
        if ((bool)Settings.enableThrustModulator.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_THRUST_MODULATOR_ENABLED", true);
        }
        if ((bool)Settings.enablePersistentInput.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_PERSISTENT_INPUT_ENABLED", true);
        }
        if ((bool)Settings.addShipSignal.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_SHIP_SIGNAL_ENABLED", true);
        }
        if ((bool)Settings.addTether.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_TETHER_HOOKS_ENABLED", true);
        }
        if ((bool)Settings.addExpeditionFlag.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_EXPEDITION_FLAG_ENABLED", true);
        }
        if ((bool)Settings.addShipWarpCore.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_WARP_CORE_ENABLED", true);
        }
        if ((bool)Settings.addRadio.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_RADIO_ENABLED", true);
        }
    }

    #endregion

    #region Events

    private void OnPlayerSuitUp()
    {
        if (SELocator.GetPlayerBody().GetComponent<PlayerResources>()._currentOxygen < _lastSuitOxygen)
        {
            SELocator.GetPlayerBody().GetComponent<PlayerResources>()._currentOxygen = _lastSuitOxygen;
        }
    }

    private void OnPlayerRemoveSuit()
    {
        UpdateSuitOxygen();
    }

    private void OnEnterFluid(FluidVolume fluid)
    {
        float dragMultiplier = Mathf.Max((float)Settings.atmosphereAngularDragMultiplier.GetProperty(), 0f);
        SELocator.GetShipBody()._rigidbody.angularDrag = 0.94f * dragMultiplier;
        SELocator.GetShipBody().GetComponent<ShipThrusterModel>()._angularDrag = 0.94f * dragMultiplier;
    }

    private void OnExitFluid(FluidVolume fluid)
    {
        if (SELocator.GetShipDetector().GetComponent<ShipFluidDetector>()._activeVolumes.Count == 0)
        {
            float dragMultiplier = Mathf.Max((float)Settings.spaceAngularDragMultiplier.GetProperty(), 0f);
            SELocator.GetShipBody()._rigidbody.angularDrag = 0.94f * dragMultiplier;
            SELocator.GetShipBody().GetComponent<ShipThrusterModel>()._angularDrag = 0.94f * dragMultiplier;
        }
    }

    private void OnEnterShip()
    {
        HatchController hatchController = SELocator.GetShipBody().GetComponentInChildren<HatchController>();
        hatchController._interactVolume.EnableInteraction();
        hatchController.GetComponent<SphereShape>().radius = 1f;
        hatchController.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        hatchController.transform.parent.GetComponentInChildren<AutoHatchController>().DisableInteraction();
    }

    private void OnExitShip()
    {
        HatchController hatchController = SELocator.GetShipBody().GetComponentInChildren<HatchController>();
        hatchController._interactVolume.DisableInteraction();
        hatchController.GetComponent<SphereShape>().radius = 3.5f;
        hatchController.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
    }

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
        SELocator.GetShipBody().SetCenterOfMass(SELocator.GetShipBody().GetWorldCenterOfMass());
    }

    private void OnWakeUp()
    {
        bool allRainbow = (string)Settings.interiorHullColor.GetProperty() == "Rainbow"
            && (string)Settings.exteriorHullColor.GetProperty() == "Rainbow"
            && (string)Settings.shipLightColor.GetProperty() == "Rainbow"
            && (string)Settings.thrusterColor.GetProperty() == "Rainbow"
            && (string)Settings.damageIndicatorColor.GetProperty() == "Rainbow";
        if (AchievementsAPI != null && !AchievementsAPI.HasAchievement("SHIPENHANCEMENTS.RGB_SETUP") && allRainbow)
        {
            AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.RGB_SETUP");
        }
    }

    private void CheckAllPartsDamaged()
    {
        if (AchievementsAPI == null || SEAchievementTracker.HowDidWeGetHere || SELocator.GetShipDamageController().IsSystemFailed()) return;

        bool allDamaged = true;
        foreach (ShipComponent comp in SELocator.GetShipDamageController()._shipComponents)
        {
            if (!comp.isDamaged)
            {
                allDamaged = false;
                break;
            }
        }
        if (allDamaged)
        {
            foreach (ShipHull hull in SELocator.GetShipDamageController()._shipHulls)
            {
                if (!hull.isDamaged)
                {
                    allDamaged = false;
                    break;
                }
            }
        }
        if (allDamaged)
        {
            SEAchievementTracker.HowDidWeGetHere = true;
            AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.HOW_DID_WE_GET_HERE");
        }
    }

    private void CheckNoPartsDamaged()
    {
        if (_shipDestroyed) return;

        bool anyDamaged = false;

        foreach (ShipHull hull in SELocator.GetShipDamageController()._shipHulls)
        {
            if (hull != null && hull.isDamaged)
            {
                anyDamaged = true;
            }
        }
        if (!anyDamaged)
        {
            foreach (ShipComponent component in SELocator.GetShipDamageController()._shipComponents)
            {
                if (component != null && component.isDamaged && component._repairReceiver.repairDistance > 0)
                {
                    anyDamaged = true;
                }
            }
        }

        if ((bool)Settings.enableRepairConfirmation.GetProperty() && !anyDamaged)
        {
            SELocator.GetShipTransform().Find("Audio_Ship/SystemOnlineAudio(Clone)")?.GetComponent<OWAudioSource>().PlayOneShot(AudioType.TH_ZeroGTrainingAllRepaired, 1f);
        }
        if ((bool)Settings.enableFragileShip.GetProperty())
        {
            anyPartDamaged = anyDamaged;
        }
    }

    private void OnEndConversation()
    {
        if (DialogueConditionManager.SharedInstance.GetConditionState("SE_GROUNDED_BY_HORNFELS"))
        {
            groundedByHornfels = true;
            GameObject th = GameObject.Find("TimberHearth_Body");
            if (th != null)
            {
                LaunchElevatorController elevator = th.GetComponentInChildren<LaunchElevatorController>();
                elevator._launchElevator._interactVolume.ChangePrompt("Grounded by Hornfels");
                elevator._launchElevator._interactVolume.SetKeyCommandVisible(false);
            }
        }
    }

    private void OnStartShipIgnition()
    {
        shipIgniting = true;
    }

    private void OnStopShipIgnition()
    {
        shipIgniting = false;
    }

    private void OnShipHullDetached()
    {
        _disableAirWhenZeroOxygen = true;
    }

    #endregion

    #region Properties

    public void UpdateSuitOxygen()
    {
        _lastSuitOxygen = SELocator.GetPlayerBody().GetComponent<PlayerResources>()._currentOxygen;
    }

    public bool IsShipInOxygen()
    {
        return !_shipDestroyed && SELocator.GetShipOxygenDetector() != null && SELocator.GetShipOxygenDetector().GetDetectOxygen()
            && !SELocator.GetShipDetector().GetComponent<ShipFluidDetector>().InFluidType(FluidVolume.Type.WATER);
    }

    public void SetGravityLandingGearEnabled(bool enabled)
    {
        OnGravityLandingGearSwitch?.Invoke(enabled);
    }

    public void SetGravityLandingGearInverted(bool inverted)
    {
        OnGravityLandingGearInverted?.Invoke(inverted);
    }

    public void SetThrustModulatorLevel(int level)
    {
        ThrustModulatorLevel = level;
    }

    public void SetEngineOn(bool state)
    {
        engineOn = state;
        OnEngineStateChanged?.Invoke(state);
    }

    public SettingsPresets.PresetName GetCurrentPreset()
    {
        return _currentPreset;
    }

    #endregion

    public static void WriteDebugMessage(object msg, bool warning = false, bool error = false)
    {
        msg ??= "null";

        if (warning)
        {
            Instance?.ModHelper?.Console?.WriteLine(msg.ToString(), MessageType.Warning);
        }
        else if (error)
        {
            Instance?.ModHelper?.Console?.WriteLine(msg.ToString(), MessageType.Error);
        }
        else
        {
            Instance?.ModHelper?.Console?.WriteLine(msg.ToString());
        }
    }

    public static void LogMessage(object msg, bool warning = false, bool error = false)
    {
        msg ??= "null";

        if (warning)
        {
            Instance?.ModHelper?.Console?.WriteLine(msg.ToString(), MessageType.Warning);
        }
        else if (error)
        {
            Instance?.ModHelper?.Console?.WriteLine(msg.ToString(), MessageType.Error);
        }
        else
        {
            Instance?.ModHelper?.Console?.WriteLine(msg.ToString());
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
