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
using OWML.ModHelper.Menus.NewMenuSystem;
using Newtonsoft.Json.Linq;
using UnityEngine.InputSystem;
using System.Globalization;
using UnityEngine.UI;
using Newtonsoft.Json;
using ShipEnhancements.Models.Json;

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
    public bool disableHeadlights;

    public static IAchievements AchievementsAPI;
    public static IQSBAPI QSBAPI;
    public static QSBCompatibility QSBCompat;
    public static IQSBInteraction QSBInteraction;

    public static INewHorizons NHAPI;
    public static INHInteraction NHInteraction;
    public static ThemeManager ThemeManager;

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
    public float ThrustModulatorFactor => ThrustModulatorLevel / 5f;
    public AudioClip ShipHorn { get; private set; }
    public List<Settings> HiddenSettings { get; private set; } = [];

    private SettingsPresets.PresetName _currentPreset = (SettingsPresets.PresetName)(-1);
    private bool _advancedColors = false;

    public GameObject DebugObjects { get; private set; }

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
    private ShipDetachableLeg _frontLeg = null;
    private List<OWAudioSource> _shipAudioToChange = [];

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
        enableEnhancedAutopilot,
        shipInputLatency,
        addEngineSwitch,
        idleFuelConsumptionMultiplier,
        shipLightColorOptions,
        shipLightColor1,
        shipLightColor2,
        shipLightColor3,
        shipLightColorBlend,
        hotThrusters,
        extraNoise,
        interiorHullColorOptions,
        interiorHullColor1,
        interiorHullColor2,
        interiorHullColor3,
        interiorHullColorBlend,
        exteriorHullColorOptions,
        exteriorHullColor1,
        exteriorHullColor2,
        exteriorHullColor3,
        exteriorHullColorBlend,
        addTether,
        disableDamageIndicators,
        addShipSignal,
        reactorLifetimeMultiplier,
        shipFriction,
        enableSignalscopeComponent,
        rustLevel,
        dirtAccumulationTime,
        thrusterColorOptions,
        thrusterColor1,
        thrusterColor2,
        thrusterColor3,
        thrusterColorBlend,
        disableSeatbelt,
        addPortableTractorBeam,
        disableShipSuit,
        indicatorColorOptions,
        indicatorColor1,
        indicatorColor2,
        indicatorColor3,
        indicatorColorBlend,
        disableAutoLights,
        addExpeditionFlag,
        addFuelCanister,
        cycloneChaos,
        moreExplosionDamage,
        singleUseTractorBeam,
        disableThrusters,
        maxDirtAccumulation,
        shipWarpCoreType,
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
        fixShipThrustIndicator,
        enableAutoAlign,
        shipHornType,
        randomIterations,
        randomDifficulty,
        disableHatch,
        splitLockOn,
        enableColorBlending,
    }

    private string[] startupMessages =
    {
        "Did you know that two opposite sides of a 6-sided dice will always add up to 7?",
        "Did you know that \"dreamt\" is the only word in the English language that ends with \"mt\"?",
        "Did you know a group of hippos is called a \"bloat\"?",
        "Did you know that apple seeds contain cyanide?",
        "Did you know Jupiter is twice as massive as every other planet in the Solar System combined?",
        "Did you know the tiny dot used in the letters \"i\" and \"j\" is called a \"tittle\"?",
        "Did you know that the infinity sign is called a \"lemniscate\"?",
        "\"Spoonfeed\" is the longest word in the English language that has all of its letters in reverse alphabetical order.",
        "\"Schoolmaster\" uses the exact same letters as \"the classroom\".",
        "The first mod ever made for Outer Wilds was NomaiVR.",
        "No te preocupes, no cambiaste el idioma a español.",
        "There are more hydrogen atoms in a single molecule of water than there are stars in the entire Solar System.",
        "Ernesto is watching.",
        "A group of penguins is called a \"waddle\".",
        "A group of ferrets is called a \"business\".",
        "The word \"orange\" was first used to describe a tree.",
        "The mitochondria is the powerhouse of the cell.",
        "1 gram of uranium is about 20 billion calories.",
        "Minimalism is made up by Big Small to sell more less.",
        "Bigimalism is made up by Big Big in order to sell more more.",
        "If you were to consume one gram of sodium, you would explode.",
        "Can you know happiness if you have never known sadness?",
        "Do you think you're naturally a good person?",
        "Which came first, the chicken or the egg?",
        "Has your favorite color changed since 10 years ago?",
        "Is the past a real thing, or is it an illusion made up by your brain?",
        "Did you know moss can be male or female?",
        "If \"color\" is light being reflected off of an object, what color is a mirror?",
        "Did you know the eyes of a spider see in different ways?",
        "Did you find Outer Wilds, or did Outer Wilds find you?",
        "What's your favorite Outer Wilds mod?",
        "Did you know Ernesto has a dedicated wiki page?",
        "Where did Geswaldo go?",
        "Did you know there's a Discord server for modding Outer Wilds?",
        "Did you get all 12 achievements for Ship Enhancements?",
        "Did you know dolphins give each other names?",
        "If you hold jump and repeatedly press shift, you can perform the universal sign of peace.",
        "You can get extra height with your jetpack by jumping right before using it.",
        "If you want a fun challenge, try completing the DLC without turning on your flashlight.",
        "Have you tried the mod General Enhancements?",
        "Have you tried the mod Moar Marshmallows?",
        "Have you tried the mod Camera Shake?",
        "If you don't think Outer Wilds is scary enough, consider downloading Ernesto Chase.",
        "Did you know you can export your current mod list as a file in the Outer Wilds Mod Manager?",
        "Did you know the Outer Wilds Mod Manager has color themes you can pick?",
        "Have you ever heard of Half a Man Videos? They make YouTube videos about Outer Wilds.",
        "Have you tried using negative numbers in the mod settings?",
        "Did you find all of the secret codes for the radio?",
        "If you punch yourself and it hurts, does that make you weak or strong?",
        "Why did the chicken cross the road?",
        "This statement is a lie.",
        "Did you know Outer Wilds isn't scientifically accurate? This is because in Outer Wilds the planets are round, which doesn't match real life as the Earth is in fact-"
    };

    private (string blendType, string suffix, Func<int, int, bool> canShow)[] _customSettingNames =
    [
        ("Time", "1", (index, num) => index == 1),
        ("Time", "2", (index, num) => index == 2),
        ("Time", "3", (index, num) => index == 3),
        ("Temperature", "(Hot)", (index, num) => index == 1),
        ("Temperature", "(Default)", (index, num) => index != 1 && index != num),
        ("Temperature", "(Cold)", (index, num) => index == num),
        ("Ship Temperature", "(Hot)", (index, num) => index == 1),
        ("Ship Temperature", "(Default)", (index, num) => index != 1 && index != num),
        ("Ship Temperature", "(Cold)", (index, num) => index == num),
        ("Reactor State", "(Default)", (index, num) => index == num - 2),
        ("Reactor State", "(Damaged)", (index, num) => index == num - 1),
        ("Reactor State", "(Critical)", (index, num) => index == num),
        ("Ship Damage %", "(No Damage)", (index, num) => index == num - 2),
        ("Ship Damage %", "(Low Damage)", (index, num) => index == num - 1),
        ("Ship Damage %", "(High Damage)", (index, num) => index == num),
        ("Fuel", "(Max Fuel)", (index, num) => index == 1),
        ("Fuel", "(Low Fuel)", (index, num) => index != 1 && index == num - 1),
        ("Fuel", "(No Fuel)", (index, num) => index == num),
        ("Oxygen", "(Max Oxygen)", (index, num) => index == 1),
        ("Oxygen", "(Low Oxygen)", (index, num) => index != 1 && index == num - 1),
        ("Oxygen", "(No Oxygen)", (index, num) => index == num),
        ("Velocity", "(Positive)", (index, num) => index == 1),
        ("Velocity", "(Matched)", (index, num) => index != 1 && index != num),
        ("Velocity", "(Negative)", (index, num) => index == num),
        ("Gravity", "(Zero Gravity)", (index, num) => index == num - 2),
        ("Gravity", "(Low Gravity)", (index, num) => index == num - 1),
        ("Gravity", "(High Gravity)", (index, num) => index == num),
    ];

    private Dictionary<string, string> _customTooltips = new()
    {
        { "Time", "Time mode blends between colors over a set amount of time." },
        { "Temperature", "Temperature mode blends between colors based on the ship's temperature." },
        { "Ship Temperature", "Ship Temperature mode blends between colors based on the ship's internal temperature." },
        { "Reactor State", "Reactor State mode changes the color if the reactor is damaged or is about to explode." },
        { "Ship Damage %", "Ship Damage % mode blends between colors based on how many parts of the ship are damaged." },
        { "Fuel", "Fuel mode blends between colors based on the amount of fuel left in the ship." },
        { "Oxygen", "Oxygen mode blends between colors based on the amount of oxygen left in the ship." },
        { "Velocity", "Velocity mode blends between colors based on how fast you're moving towards your current lock-on target." },
        { "Gravity", "Gravity mode blends between colors based on how high the gravity is." },
    };

    private Dictionary<string, string> _stemToSuffix = new()
    {
        {"shipLight", "Light Color"},
        {"interiorHull", "Interior Color"},
        {"exteriorHull", "Exterior Color"},
        {"thruster", "Thruster Color"},
        {"indicator", "Indicator Color"}
    };

    private void Awake()
    {
        Instance = this;
        HarmonyLib.Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    private void Start()
    {
        _shipEnhancementsBundle = AssetBundle.LoadFromFile(Path.Combine(ModHelper.Manifest.ModFolderPath, "assets/shipenhancements"));
        ThemeManager = new ThemeManager("ShipEnhancements.Data.themes.json");

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

        PrintStartupMessage();

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

            // Alt never works the first time I press it
            // Hopefully this doesn't break any compatibility
            InputLibrary.freeLook.ConsumeInput();

            GlobalMessenger.AddListener("SuitUp", OnPlayerSuitUp);
            GlobalMessenger.AddListener("RemoveSuit", OnPlayerRemoveSuit);
            GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
            GlobalMessenger.AddListener("WakeUp", OnWakeUp);
            GlobalMessenger.AddListener("ShipHullDetached", OnShipHullDetached);
            GlobalMessenger.AddListener("EnterShip", OnEnterShip);
            GlobalMessenger.AddListener("ExitShip", OnExitShip);
            oxygenDepleted = false;
            fuelDepleted = false;
            probeDestroyed = false;
            _shipDestroyed = false;
            anyPartDamaged = false;
            groundedByHornfels = false;
            shipIgniting = false;
            ThrustModulatorLevel = 5;
            ShipRepairLimitController.SetRepairLimit(-1);
            ShipRepairLimitController.SetPartsRepaired(0);
            ErnestoDetectiveController.Initialize();

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
            GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
            GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
            if ((float)Settings.spaceAngularDragMultiplier.GetProperty() > 0 || (float)Settings.atmosphereAngularDragMultiplier.GetProperty() > 0)
            {
                ShipFluidDetector detector = SELocator.GetShipDetector().GetComponent<ShipFluidDetector>();
                detector.OnEnterFluid -= OnEnterFluid;
                detector.OnExitFluid -= OnExitFluid;
            }
            /*if ((bool)Settings.enableAutoHatch.GetProperty() && !InMultiplayer && !(bool)Settings.disableHatch.GetProperty())
            {
                GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
                GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
            }*/
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
            if (_frontLeg != null)
            {
                _frontLeg.OnLegDetach -= OnFrontLegDetached;
                _frontLeg = null;
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
            _shipAudioToChange.Clear();
            InputLatencyController.OnUnloadScene();
        };
    }

    private void PrintStartupMessage()
    {
        ModHelper.Console.WriteLine("Ship Enhancements is loaded!", MessageType.Success);

        System.Random rand = new System.Random();
        int index = rand.Next(0, startupMessages.Length);
        ModHelper.Console.WriteLine(startupMessages[index], MessageType.Info);
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
            ErnestoDetectiveController.ItWasExplosion(fromTorque: true);
            SELocator.GetShipDamageController().Explode();

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
        List<Settings> settingsToRandomize = [];

        if (ModHelper.Config.GetSettingsValue<string>("preset") == "Random")
        {
            var data = LoadData();

            if (data.inclusive != null && data.exclusive != null)
            {
                if (data.inclusive.Length == 0)
                {
                    settingsToRandomize.AddRange(allSettings);
                }

                var configSettings = ModHelper.Config.Settings.Keys.ToList();
                List<string> separators = ["Disable Ship Parts", "Adjust Ship Functions", "Add Ship Functions", "Decoration", "Quality of Life"];

                foreach (string setting in data.inclusive)
                {
                    if (setting.IsEnum<Settings>())
                    {
                        var theSetting = setting.AsEnum<Settings>();
                        if (!settingsToRandomize.Contains(theSetting) && !data.exclusive.Contains(setting))
                        {
                            settingsToRandomize.Add(theSetting);
                        }
                    }
                    else if (separators.Contains(setting))
                    {
                        int start = configSettings.IndexOf(setting);
                        int end = configSettings.Count;
                        if (setting != separators[separators.Count - 1])
                        {
                            end = configSettings.IndexOf(separators[separators.IndexOf(setting) + 1]);
                        }

                        if (start > 0 && end > 0)
                        {
                            for (int i = start; i < end; i++)
                            {
                                if (configSettings[i].IsEnum<Settings>())
                                {
                                    var theSetting = configSettings[i].AsEnum<Settings>();
                                    if (!settingsToRandomize.Contains(theSetting) && !data.exclusive.Contains(setting))
                                    {
                                        settingsToRandomize.Add(theSetting);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (string setting in data.exclusive)
                {
                    if (setting.IsEnum<Settings>())
                    {
                        var theSetting = setting.AsEnum<Settings>();
                        if (settingsToRandomize.Contains(theSetting) && !data.inclusive.Contains(setting))
                        {
                            settingsToRandomize.Remove(theSetting);
                        }
                    }
                    else if (separators.Contains(setting))
                    {
                        int start = configSettings.IndexOf(setting);
                        int end = configSettings.Count;
                        if (setting != separators[separators.Count - 1])
                        {
                            end = configSettings.IndexOf(separators[separators.IndexOf(setting) + 1]);
                        }

                        if (start > 0 && end > 0)
                        {
                            for (int i = start; i < end; i++)
                            {
                                if (configSettings[i].IsEnum<Settings>())
                                {
                                    var theSetting = configSettings[i].AsEnum<Settings>();
                                    if (settingsToRandomize.Contains(theSetting) && !data.inclusive.Contains(setting))
                                    {
                                        settingsToRandomize.Remove(theSetting);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                settingsToRandomize.AddRange(allSettings);
            }

            float total = 0f;
            foreach (var setting in allSettings)
            {
                if (settingsToRandomize.Contains(setting))
                {
                    setting.SetProperty(ModHelper.DefaultConfig.GetSettingsValue<object>(setting.GetName()));
                }
                else
                {
                    setting.SetProperty(ModHelper.Config.GetSettingsValue<object>(setting.GetName()));
                }

                if (settingsToRandomize.Contains(setting) && SettingsPresets.RandomSettings.ContainsKey(setting.GetName()))
                {
                    total += SettingsPresets.RandomSettings[setting.GetName()].GetRandomChance();
                }
            }

            int iterations = Mathf.FloorToInt(
                Mathf.Lerp(Mathf.Min(2f, settingsToRandomize.Count), settingsToRandomize.Count, (float)Settings.randomIterations.GetValue()));

            for (int j = 0; j < iterations; j++)
            {
                float rand = UnityEngine.Random.Range(0f, total);
                float sum = 0;
                for (int k = 0; k < settingsToRandomize.Count; k++)
                {
                    if (SettingsPresets.RandomSettings.ContainsKey(settingsToRandomize[k].GetName()))
                    {
                        sum += SettingsPresets.RandomSettings[settingsToRandomize[k].GetName()].GetRandomChance();
                        if (rand < sum)
                        {
                            settingsToRandomize[k].SetProperty(SettingsPresets.RandomSettings[settingsToRandomize[k].GetName()]
                                .GetRandomValue(true));
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            foreach (Settings setting in allSettings)
            {
                setting.SetProperty(ModHelper.Config.GetSettingsValue<object>(setting.GetName()));
            }
        }
    }

    private (string[] inclusive, string[] exclusive) LoadData()
    {
        var data = JsonConvert.DeserializeObject<RandomizerSettingsJson>(
            File.ReadAllText(Path.Combine(ModHelper.Manifest.ModFolderPath, "RandomizerSettings.json"))
        );

        if (data is null)
        {
            LogMessage("Couldn't load RandomizerSettings.json! Did you make a typo?", warning: true);
            return (null, null);
        }

        return (data.InclusiveSettings, data.ExclusiveSettings);
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
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.ANGLERFISH_KILL", true, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.FIRE_HAZARD", true, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.HOW_DID_WE_GET_HERE", false, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.HULK_SMASH", false, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.RGB_SETUP", false, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.SATELLITE", false, this);
            AchievementsAPI.RegisterAchievement("SHIPENHANCEMENTS.SUPERHOTSHOT", false, this);
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

        var debugObjectsPrefab = LoadPrefab("Assets/ShipEnhancements/DebugObjects.prefab");
        DebugObjects = Instantiate(debugObjectsPrefab, SELocator.GetShipBody().transform);

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

        MeshRenderer chassisRenderer = SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Interior/Cockpit_Interior_Chassis")
            .GetComponent<MeshRenderer>();
        Texture2D blackTex = (Texture2D)LoadAsset("Assets/ShipEnhancements/Black_d.png");
        chassisRenderer.sharedMaterials[6].SetTexture("_OcclusionMap", blackTex);
        chassisRenderer.sharedMaterials[6].SetFloat("_OcclusionStrength", 0.95f);

        if ((bool)Settings.enableScoutLauncherComponent.GetProperty()
            || (string)Settings.shipWarpCoreType.GetProperty() == "Component")
        {
            Transform damageScreen = SELocator.GetShipTransform().Find("Module_Cockpit/Systems_Cockpit/ShipCockpitUI/DamageScreen/HUD_ShipDamageDisplay");
            if ((bool)Settings.enableScoutLauncherComponent.GetProperty())
            {
                GameObject scoutDamage = LoadPrefab("Assets/ShipEnhancements/HUD_ShipDamageDisplay_Scout.prefab");
                scoutDamage.GetComponent<MeshRenderer>().material = damageScreen.GetComponent<MeshRenderer>().material;
                Instantiate(scoutDamage, damageScreen.parent);
            }
            if ((string)Settings.shipWarpCoreType.GetProperty() == "Component")
            {
                GameObject warpDamage = LoadPrefab("Assets/ShipEnhancements/HUD_ShipDamageDisplay_Warp.prefab");
                warpDamage.GetComponent<MeshRenderer>().material = damageScreen.GetComponent<MeshRenderer>().material;
                Instantiate(warpDamage, damageScreen.parent);
            }
        }

        SetUpShipAudio();

        foreach (OWAudioSource audio in _shipAudioToChange)
        {
            audio.spatialBlend = 1f;
        }

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
        else
        {
            _frontLeg = SELocator.GetShipTransform().Find("Module_LandingGear/LandingGear_Front")
                .GetComponent<ShipDetachableLeg>();
            _frontLeg.OnLegDetach += OnFrontLegDetached;
        }

        bool coloredLights = (string)Settings.shipLightColor1.GetProperty() != "Default";
        bool blendingLights = ((bool)Settings.enableColorBlending.GetProperty()
            && int.Parse((string)Settings.shipLightColorOptions.GetProperty()) > 1)
            || (string)Settings.shipLightColor1.GetProperty() == "Rainbow";

        if ((bool)Settings.disableShipLights.GetProperty() || coloredLights || blendingLights)
        {
            Color lightColor = Color.white;
            if (coloredLights && !blendingLights)
            {
                lightColor = ThemeManager.GetLightTheme(
                    (string)Settings.shipLightColor1.GetProperty())
                    .LightColor / 255f;
            }

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
                        else if (blendingLights)
                        {
                            light.gameObject.AddComponent<ShipLightBlendController>();
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
                else if (blendingLights)
                {
                    beacon.gameObject.AddComponent<ShipLightBlendController>();
                }
                else if (coloredLights)
                {
                    beacon.GetComponent<Light>().color = lightColor;
                    beacon._initEmissionColor = lightColor;
                    if ((string)Settings.shipLightColor1.GetProperty() == "Rainbow")
                    {
                        beacon.gameObject.AddComponent<ShipLightBlendController>();
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
        if ((bool)Settings.enableAutoHatch.GetProperty() && !InMultiplayer && !(bool)Settings.disableHatch.GetProperty())
        {
            /*GlobalMessenger.AddListener("EnterShip", OnEnterShip);
            GlobalMessenger.AddListener("ExitShip", OnExitShip);*/
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
        /*if ((bool)Settings.disableScoutRecall.GetProperty() 
            && (bool)Settings.disableScoutLaunching.GetProperty()
            && !(bool)Settings.scoutPhotoMode.GetProperty()
            && (bool)Settings.enableScoutLauncherComponent.GetProperty())
        {
            SELocator.GetProbeLauncherComponent()._repairReceiver.repairDistance = 0f;
            SELocator.GetProbeLauncherComponent()._damaged = true;
            SELocator.GetProbeLauncherComponent()._repairFraction = 0f;
            SELocator.GetProbeLauncherComponent().OnComponentDamaged();
        }*/
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
        if ((bool)Settings.enableEnhancedAutopilot.GetProperty())
        {
            SELocator.GetShipBody().gameObject.AddComponent<ShipPersistentInput>();
            SELocator.GetShipBody().gameObject.AddComponent<PidAutopilot>();
        }
        if ((float)Settings.shipInputLatency.GetProperty() != 0f)
        {
            InputLatencyController.Initialize();
        }
        if ((bool)Settings.hotThrusters.GetProperty() || (bool)Settings.enableColorBlending.GetProperty()
            || (string)Settings.thrusterColor1.GetProperty() != "Default")
        {
            GameObject flameHazardVolume = LoadPrefab("Assets/ShipEnhancements/FlameHeatVolume.prefab");
            foreach (ThrusterFlameController flame in SELocator.GetShipTransform().GetComponentsInChildren<ThrusterFlameController>())
            {
                if ((bool)Settings.hotThrusters.GetProperty())
                {
                    GameObject volume = Instantiate(flameHazardVolume, Vector3.zero, Quaternion.identity, flame.transform);
                    volume.transform.localPosition = Vector3.zero;
                    volume.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    if (!flame.enabled)
                    {
                        flame.transform.localScale = Vector3.zero;
                    }
                }

                string color = (string)Settings.thrusterColor1.GetProperty();
                bool thrusterBlend = ((bool)Settings.enableColorBlending.GetProperty()
                    && int.Parse((string)Settings.thrusterColorOptions.GetProperty()) > 1)
                    || color == "Rainbow";

                if (thrusterBlend)
                {
                    flame.gameObject.AddComponent<ShipThrusterBlendController>();
                }
                else if (color != "Default")
                {
                    MeshRenderer rend = flame.GetComponent<MeshRenderer>();

                    ThrusterTheme thrusterColors = ThemeManager.GetThrusterTheme(color);
                    rend.material.SetTexture("_MainTex",
                        (Texture2D)LoadAsset("Assets/ShipEnhancements/ThrusterColors/"
                        + thrusterColors.ThrusterColor));

                    Color thrustColor = Color.white * Mathf.Pow(2, thrusterColors.ThrusterIntensity);
                    thrustColor.a = thrustColor.a = 0.5019608f;
                    rend.material.SetColor("_Color", thrustColor);

                    Light light = flame.GetComponentInChildren<Light>();
                    light.color = thrusterColors.ThrusterLight / 255f;

                    ThrustIndicatorManager.ApplyTheme(thrusterColors);
                }
            }
        }
        if ((float)Settings.reactorLifetimeMultiplier.GetProperty() != 1f)
        {
            ShipReactorComponent reactor = SELocator.GetShipDamageController()._shipReactorComponent;

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
            GameObject audio = LoadPrefab("Assets/ShipEnhancements/TetherAudioController.prefab");
            Instantiate(audio, SELocator.GetPlayerBody().transform);
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
        if ((string)Settings.shipWarpCoreType.GetProperty() == "Enabled")
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
            else
            {
                WaitForCustomSpawnLoaded();
            }
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
            OWAudioSource source = Instantiate(audio, SELocator.GetShipTransform().Find("Audio_Ship")).GetComponent<OWAudioSource>();
            AddShipAudioToChange(source);

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

            var bh = GameObject.Find("BrittleHollow_Body");
            if (bh != null)
            {
                var parent = bh.transform.Find("Sector_BH/Sector_OldSettlement/Fragment OldSettlement 5");
                var additions = LoadPrefab("Assets/ShipEnhancements/OldSettlementAdditions.prefab");
                AssetBundleUtilities.ReplaceShaders(additions);
                Instantiate(additions, parent);
            }
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
        if ((bool)Settings.enableAutoAlign.GetProperty())
        {
            SELocator.GetShipBody().gameObject.AddComponent<ShipAutoAlign>();
        }
        if ((string)Settings.shipHornType.GetProperty() != "None")
        {
            string type = (string)Settings.shipHornType.GetProperty();
            switch (type)
            {
                case "Default":
                    ShipHorn = LoadAudio("Assets/ShipEnhancements/AudioClip/ShipHorn_Newer.ogg");
                    break;
                case "Old":
                    ShipHorn = LoadAudio("Assets/ShipEnhancements/AudioClip/ShipHorn_Old.ogg");
                    break;
                case "Train":
                    ShipHorn = LoadAudio("Assets/ShipEnhancements/AudioClip/ShipHorn_Train.ogg");
                    break;
                case "Loud":
                    ShipHorn = LoadAudio("Assets/ShipEnhancements/AudioClip/ShipHorn_Blaring.ogg");
                    break;
                case "Short":
                    ShipHorn = LoadAudio("Assets/ShipEnhancements/AudioClip/ShipHorn_Jumpscare.ogg");
                    break;
                case "Clown":
                    ShipHorn = LoadAudio("Assets/ShipEnhancements/AudioClip/ShipHorn_Clown.ogg");
                    break;
                case "Annoying":
                    ShipHorn = LoadAudio("Assets/ShipEnhancements/AudioClip/ShipHorn_Goofy.ogg");
                    break;
            }

            Instantiate(LoadPrefab("Assets/ShipEnhancements/ShipHorn.prefab"), SELocator.GetShipTransform().Find("Audio_Ship"));
        }
        if ((bool)Settings.disableHatch.GetProperty())
        {
            SELocator.GetShipTransform().Find("Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Exterior/HatchPivot").gameObject.SetActive(false);
            SELocator.GetShipTransform().Find("Module_Cabin/Geo_Cabin/Cabin_Colliders_Back/Shared/Hatch_Collision_Open").gameObject.SetActive(false);
            HatchController hatch = SELocator.GetShipTransform().GetComponentInChildren<HatchController>();
            hatch._interactVolume.GetComponent<Shape>().enabled = false;
        }
        if ((string)Settings.disableThrusters.GetProperty() != "None")
        {
            ThrustAndAttitudeIndicator indicator = SELocator.GetShipTransform().GetComponentInChildren<ThrustAndAttitudeIndicator>();
            List<Light> lightsToDisable = [];
            switch ((string)Settings.disableThrusters.GetProperty())
            {
                case "Backward":
                    indicator._rendererForward.gameObject.SetActive(false);
                    lightsToDisable.AddRange(indicator._lightsForward);
                    break;
                case "Left-Right":
                    indicator._rendererLeft.gameObject.SetActive(false);
                    indicator._rendererRight.gameObject.SetActive(false);

                    lightsToDisable.AddRange(indicator._lightsLeft);
                    lightsToDisable.AddRange(indicator._lightsRight);
                    break;
                case "Up-Down":
                    indicator._rendererUp.gameObject.SetActive(false);
                    indicator._rendererDown.gameObject.SetActive(false);

                    lightsToDisable.AddRange(indicator._lightsUp);
                    lightsToDisable.AddRange(indicator._lightsDown);
                    break;
                case "All Except Forward":
                    indicator._rendererForward.gameObject.SetActive(false);
                    indicator._rendererLeft.gameObject.SetActive(false);
                    indicator._rendererRight.gameObject.SetActive(false);
                    indicator._rendererUp.gameObject.SetActive(false);
                    indicator._rendererDown.gameObject.SetActive(false);

                    lightsToDisable.AddRange(indicator._lightsForward);
                    lightsToDisable.AddRange(indicator._lightsLeft);
                    lightsToDisable.AddRange(indicator._lightsRight);
                    lightsToDisable.AddRange(indicator._lightsUp);
                    lightsToDisable.AddRange(indicator._lightsDown);
                    break;
            }

            foreach (Light light in lightsToDisable)
            {
                light.gameObject.SetActive(false);
            }
        }
        if (AchievementsAPI != null)
        {
            GameObject th = GameObject.Find("TimberHearth_Body");
            if (th != null)
            {
                GameObject satelliteObj = LoadPrefab("Assets/ShipEnhancements/SatelliteAchievement_Volume.prefab");
                Transform parent = th.transform.Find("Sector_TH/Sector_Village/Sector_Observatory");
                Instantiate(satelliteObj, parent);
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

                float lerp = (float)Settings.randomComponentDamage.GetProperty();
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

    private void SetupExplosion(Transform effectsTransform, ExplosionController explosion)
    {
        float multiplier = (float)Settings.shipExplosionMultiplier.GetProperty();

        if (multiplier >= 100f)
        {
            GameObject supernova = LoadPrefab("Assets/ShipEnhancements/ExplosionSupernova.prefab");
            AssetBundleUtilities.ReplaceShaders(supernova);
            GameObject supernovaObj = Instantiate(supernova, SELocator.GetShipTransform().Find("Module_Engine"));
            supernovaObj.SetActive(false);
            return;
        }
        else
        {
            explosion._length *= (multiplier * 0.75f) + 0.25f;
            explosion._forceVolume._acceleration *= (multiplier * 0.25f) + 0.75f;
            explosion.transform.localScale *= multiplier;
            explosion._lightRadius *= (multiplier * 0.75f) + 0.25f;
            explosion._lightIntensity *= Mathf.Min((multiplier * 0.01f) + 0.99f, 10f);
            explosion.GetComponent<SphereCollider>().radius = 0.1f;
            OWAudioSource audio = effectsTransform.Find("ExplosionAudioSource").GetComponent<OWAudioSource>();
            audio.maxDistance *= (multiplier * 0.5f) + 0.5f;
            AnimationCurve curve = audio.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
            Keyframe[] newKeys = new Keyframe[curve.keys.Length];
            for (int i = 0; i < curve.keys.Length; i++)
            {
                newKeys[i] = curve.keys[i];
                newKeys[i].value *= (multiplier * 0.5f) + 0.5f;
            }
            AnimationCurve newCurve = new();
            foreach (Keyframe key in newKeys)
            {
                newCurve.AddKey(key);
            }
            audio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, newCurve);

            if (multiplier > 1f)
            {
                AudioLowPassFilter lowPass = audio.gameObject.AddComponent<AudioLowPassFilter>();
                lowPass.cutoffFrequency = 25000f;
                AudioReverbFilter reverb = audio.gameObject.AddComponent<AudioReverbFilter>();
                reverb.reverbPreset = AudioReverbPreset.Quarry;
                reverb.reverbPreset = AudioReverbPreset.User;
                float lerp = Mathf.InverseLerp(1f, 50f, multiplier);
                reverb.reverbLevel = Mathf.Lerp(-1000f, 400f, lerp);
                reverb.decayTime = Mathf.Lerp(1.49f, 3f, lerp);
                reverb.roomHF = -4500f;

                if (multiplier >= 10f)
                {
                    Instantiate(LoadPrefab("Assets/ShipEnhancements/ShipExplosionExpandAudio.prefab"),
                        audio.transform).name = "ShipExplosionExpandAudio";
                }
            }
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
        string zones = (string)Settings.temperatureZonesAmount.GetProperty();
        bool hot = zones == "All" || zones == "Hot";
        bool cold = zones == "All" || zones == "Cold";

        if (hot)
        {
            SpawnSunTemperatureZones();

            GameObject vm = GameObject.Find("VolcanicMoon_Body");
            if (vm != null)
            {
                GameObject vmTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_VolcanicMoon.prefab");
                Instantiate(vmTempZone, vm.transform.Find("Sector_VM"));
            }

            GameObject gd = GameObject.Find("GiantsDeep_Body");
            if (gd != null)
            {
                GameObject gdTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_GiantsDeepCore.prefab");
                Instantiate(gdTempZone2, gd.transform.Find("Sector_GD/Sector_GDInterior"));
            }

            GameObject th = GameObject.Find("TimberHearth_Body");
            if (th != null)
            {
                GameObject thTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_TimberHearthCore.prefab");
                Instantiate(thTempZone2, th.transform.Find("Sector_TH"));
            }

            GameObject ct = GameObject.Find("CaveTwin_Body");
            if (ct != null)
            {
                GameObject ctTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_CaveTwinHot.prefab");
                Instantiate(ctTempZone1, ct.transform.Find("Sector_CaveTwin"));
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

        if (cold)
        {
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
                GameObject ctTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_CaveTwinCold.prefab");
                Instantiate(ctTempZone2, ct.transform.Find("Sector_CaveTwin"));
            }

            GameObject whs = GameObject.Find("WhiteholeStationSuperstructure_Body");
            if (whs != null)
            {
                GameObject whsTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_WhiteHoleStation.prefab");
                Instantiate(whsTempZone, whs.transform);
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

        if ((bool)Settings.addErnesto.GetProperty())
        {
            GameObject bh = GameObject.Find("BrittleHollow_Body");
            if (bh != null)
            {
                GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_0187.prefab");
                Instantiate(zone, bh.transform.Find("Sector_BH/Sector_OldSettlement/Fragment OldSettlement 5/Core_OldSettlement 5"));
            } 
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
            receiverObj.transform.localPosition = spawn.offset;
            receiverObj.transform.localRotation = Quaternion.identity;
            receiverObj.OnCustomSpawnPoint();

            ShipWarpCoreController core = SELocator.GetShipTransform().GetComponentInChildren<ShipWarpCoreController>();
            core.SetReceiver(receiverObj);
        }
        else
        {
            GameObject receiver = LoadPrefab("Assets/ShipEnhancements/ShipWarpReceiver.prefab");
            AssetBundleUtilities.ReplaceShaders(receiver);
            receiver.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = SELocator.GetShipBody().gameObject;
            GameObject receiverObj = Instantiate(receiver, GameObject.Find("TimberHearth_Body").transform);

            ShipWarpCoreController core = SELocator.GetShipTransform().GetComponentInChildren<ShipWarpCoreController>();
            core.SetReceiver(receiverObj.GetComponent<ShipWarpCoreReceiver>());
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

        disableHeadlights = true;
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
        string interior = (string)Settings.interiorHullColor1.GetProperty();
        string exterior = (string)Settings.exteriorHullColor1.GetProperty();

        if (!(bool)Settings.enableColorBlending.GetProperty()
            && interior == "Defualt" && exterior == "Default")
        {
            return;
        }

        MeshRenderer suppliesRenderer = SELocator.GetShipTransform().
            Find("Module_Supplies/Geo_Supplies/Supplies_Geometry/Supplies_Interior").GetComponent<MeshRenderer>();
        Material inSharedMat = suppliesRenderer.sharedMaterials[0];
        Material inSharedMat2 = inSharedMat;

        CockpitButtonPanel buttonPanel = SELocator.GetButtonPanel();
        if (buttonPanel != null)
        {
            MeshRenderer buttonPanelRenderer = buttonPanel.transform.Find("Panel/PanelBody.001").GetComponent<MeshRenderer>();
            inSharedMat2 = buttonPanelRenderer.sharedMaterials[0];
        }

        bool blendInterior = ((bool)Settings.enableColorBlending.GetProperty()
            && int.Parse((string)Settings.interiorHullColorOptions.GetProperty()) > 1)
            || interior == "Rainbow";

        if (blendInterior)
        {
            InteriorHullBlendController hullBlend = SELocator.GetShipBody()
                .gameObject.GetAddComponent<InteriorHullBlendController>();
            hullBlend.AddSharedMaterial(inSharedMat);
            hullBlend.AddSharedMaterial(inSharedMat2);
        }
        else if (interior != "Default")
        {
            Color color = ThemeManager.GetHullTheme(interior).HullColor / 255f;
            inSharedMat.SetColor("_Color", color);
            inSharedMat2.SetColor("_Color", color);
        }
        else
        {
            inSharedMat.SetColor("_Color", Color.white);
            inSharedMat2.SetColor("_Color", Color.white);
        }

        MeshRenderer cabinRenderer = SELocator.GetShipTransform().
            Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Exterior").GetComponent<MeshRenderer>();
        Material outSharedMat = cabinRenderer.sharedMaterials[3];

        bool blendExterior = ((bool)Settings.enableColorBlending.GetProperty()
            && int.Parse((string)Settings.exteriorHullColorOptions.GetProperty()) > 1)
            || exterior == "Rainbow";

        if (blendExterior)
        {
            ExteriorHullBlendController hullBlend = SELocator.GetShipBody()
                .gameObject.GetAddComponent<ExteriorHullBlendController>();
            hullBlend.AddSharedMaterial(outSharedMat);
        }
        else if (exterior != "Default")
        {
            Color color = ThemeManager.GetHullTheme(exterior).HullColor / 255f;
            outSharedMat.SetColor("_Color", color);
        }
        else
        {
            outSharedMat.SetColor("_Color", Color.white);
        }
    }

    private void SetDamageColors()
    {
        string color = (string)Settings.indicatorColor1.GetProperty();
        bool indicatorBlend = ((bool)Settings.enableColorBlending.GetProperty()
            && int.Parse((string)Settings.indicatorColorOptions.GetProperty()) > 1)
            || color == "Rainbow";

        if (indicatorBlend)
        {
            SELocator.GetShipTransform().gameObject.AddComponent<ShipIndicatorBlendController>();
        }
        else if (color != "Default")
        {
            var damageScreenMat = SELocator.GetShipTransform().Find("Module_Cockpit/Systems_Cockpit/ShipCockpitUI/DamageScreen/HUD_ShipDamageDisplay")
                .GetComponent<MeshRenderer>().material;
            var masterAlarmMat = SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Interior/Cockpit_Interior_Chassis")
                .GetComponent<MeshRenderer>().sharedMaterials[6];
            var masterAlarmLight = SELocator.GetShipTransform().Find("Module_Cabin/Lights_Cabin/PointLight_HEA_MasterAlarm").GetComponent<Light>();
            var reactorLight = SELocator.GetShipTransform().Find("Module_Engine/Systems_Engine/ReactorComponent/ReactorDamageLight").GetComponent<Light>();
            var reactorGlow = SELocator.GetShipTransform().Find("Module_Engine/Systems_Engine/ReactorComponent/Structure_HEA_PlayerShip_ReactorDamageDecal")
                .GetComponent<MeshRenderer>().material;

            DamageTheme theme = ThemeManager.GetDamageTheme(color);

            damageScreenMat.SetColor("_DamagedHullFill", theme.HullColor / 255f * Mathf.Pow(2, theme.HullIntensity));
            damageScreenMat.SetColor("_DamagedComponentFill", theme.CompColor / 255f * theme.CompIntensity);

            masterAlarmMat.SetColor("_Color", theme.AlarmColor / 255f);
            SELocator.GetShipTransform().GetComponentInChildren<ShipCockpitUI>()._damageLightColor = theme.AlarmLitColor / 255f
                * Mathf.Pow(2, theme.AlarmLitIntensity);
            masterAlarmLight.color = theme.IndicatorLight / 255f;

            Color reactorColor = theme.ReactorColor;
            reactorColor /= 191f;
            reactorColor.a = 1;
            reactorGlow.SetColor("_EmissionColor", reactorColor * Mathf.Pow(2, theme.ReactorIntensity));
            reactorLight.color = theme.ReactorLight / 255f;

            foreach (DamageEffect effect in SELocator.GetShipTransform().GetComponentsInChildren<DamageEffect>())
            {
                if (effect._damageLight)
                {
                    effect._damageLight.GetLight().color = theme.IndicatorLight / 255f;
                }
                if (effect._damageLightRenderer)
                {
                    effect._damageLightRendererColor = theme.AlarmLitColor / 255f * Mathf.Pow(2, theme.AlarmLitIntensity);
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
        if ((bool)Settings.enableEnhancedAutopilot.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_AUTOPILOT_CONTROLS_ENABLED", true);
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
        if ((string)Settings.shipWarpCoreType.GetProperty() != "Disabled")
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_WARP_CORE_ENABLED", true);
        }
        if ((bool)Settings.addRadio.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_RADIO_ENABLED", true);
        }
    }

    private void SetUpShipAudio()
    {
        ShipAudioController shipAudio = SELocator.GetShipTransform().GetComponentInChildren<ShipAudioController>();

        _shipAudioToChange.Add(shipAudio._cockpitSource);

        OWAudioSource alarmSource = shipAudio._alarmSource;
        shipAudio._shipElectrics._audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
            alarmSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        shipAudio._shipElectrics._audioSource.maxDistance = alarmSource.maxDistance;
        _shipAudioToChange.Add(shipAudio._shipElectrics._audioSource);

        GameObject toolRefObj = LoadPrefab("Assets/ShipEnhancements/ToolAudioTemplate.prefab");
        OWAudioSource toolRef = Instantiate(toolRefObj).GetComponent<OWAudioSource>();

        shipAudio._probeScreenSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
            toolRef.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        shipAudio._probeScreenSource.maxDistance = toolRef.maxDistance;
        shipAudio._signalscopeSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
            toolRef.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        shipAudio._signalscopeSource.maxDistance = toolRef.maxDistance;

        Destroy(toolRef.gameObject);
    }

    #endregion

    #region Events

    private void OnPlayerSuitUp()
    {
        if (SELocator.GetPlayerResources()._currentOxygen < _lastSuitOxygen)
        {
            SELocator.GetPlayerResources()._currentOxygen = _lastSuitOxygen;
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
        foreach (OWAudioSource audio in _shipAudioToChange)
        {
            audio.spatialBlend = 0f;
        }

        if ((bool)Settings.enableAutoHatch.GetProperty() && !InMultiplayer
            && !(bool)Settings.disableHatch.GetProperty())
        {
            HatchController hatchController = SELocator.GetShipBody().GetComponentInChildren<HatchController>();
            hatchController._interactVolume.EnableInteraction();
            hatchController.GetComponent<SphereShape>().radius = 1f;
            hatchController.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            hatchController.transform.parent.GetComponentInChildren<AutoHatchController>().DisableInteraction();
        }

        if ((bool)Settings.splitLockOn.GetProperty())
        {

        }
    }

    private void OnExitShip()
    {
        foreach (OWAudioSource audio in _shipAudioToChange)
        {
            audio.spatialBlend = 1f;
        }

        if ((bool)Settings.enableAutoHatch.GetProperty() && !InMultiplayer
            && !(bool)Settings.disableHatch.GetProperty())
        {
            HatchController hatchController = SELocator.GetShipBody().GetComponentInChildren<HatchController>();
            hatchController._interactVolume.DisableInteraction();
            hatchController.GetComponent<SphereShape>().radius = 3.5f;
            hatchController.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }
    }

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
        SELocator.GetShipBody().SetCenterOfMass(SELocator.GetShipBody().GetWorldCenterOfMass());
        if ((bool)Settings.fixShipThrustIndicator.GetProperty())
        {
            ThrustIndicatorManager.DisableIndicator();
        }
    }

    private void OnWakeUp()
    {
        bool allRainbow = !(bool)Settings.enableColorBlending.GetProperty()
            && (string)Settings.interiorHullColor1.GetProperty() == "Rainbow"
            && (string)Settings.exteriorHullColor1.GetProperty() == "Rainbow"
            && (string)Settings.shipLightColor1.GetProperty() == "Rainbow"
            && (string)Settings.thrusterColor1.GetProperty() == "Rainbow"
            && (string)Settings.indicatorColor1.GetProperty() == "Rainbow";
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

    private void OnFrontLegDetached(ShipDetachableLeg leg)
    {
        ShipCameraComponent cameraComponent = leg.GetComponentInChildren<ShipCameraComponent>();
        cameraComponent._repairReceiver.repairDistance = 0f;
        cameraComponent._damaged = true;
        cameraComponent._repairFraction = 0f;
        cameraComponent._landingCamera.SetDamaged(true);

        ShipHeadlightComponent headlightComponent = leg.GetComponentInChildren<ShipHeadlightComponent>();
        headlightComponent._repairReceiver.repairDistance = 0f;
        headlightComponent._damaged = true;
        headlightComponent._repairFraction = 0f;
        headlightComponent.OnComponentDamaged();
        FindObjectOfType<ShipCockpitController>()._externalLightsOn = false;

        disableHeadlights = true;
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

    public void WaitForCustomSpawnLoaded()
    {
        if (NHAPI != null && !_unsubFromShipSpawn)
        {
            NHAPI.GetStarSystemLoadedEvent().AddListener(SetCustomWarpDestination);
            _unsubFromShipSpawn = true;
        }
    }

    public SettingsPresets.PresetName GetCurrentPreset()
    {
        return _currentPreset;
    }

    public void AddShipAudioToChange(OWAudioSource audioSource)
    {
        _shipAudioToChange.Add(audioSource);
        audioSource.spatialBlend = PlayerState.IsInsideShip() ? 0f : 1f;
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

        // Detect preset change or init preset
        if (newPreset != _currentPreset || _currentPreset == (SettingsPresets.PresetName)(-1))
        {
            SettingsPresets.PresetName lastPreset = _currentPreset;
            _currentPreset = newPreset;
            config.SetSettingsValue("preset", _currentPreset.GetName());

            // Load saved settings if switching to Custom preset
            if ((_currentPreset == SettingsPresets.PresetName.Custom
                || _currentPreset == SettingsPresets.PresetName.Random)
                && lastPreset != (SettingsPresets.PresetName)(-1))
            {
                WriteDebugMessage("Load");
                SettingExtensions.LoadCustomSettings();
                foreach (Settings setting in allSettings)
                {
                    config.SetSettingsValue(setting.GetName(), setting.GetValue());
                }
            }
            // Save settings if switching off Custom preset
            else
            {
                if (lastPreset == SettingsPresets.PresetName.Custom
                    || lastPreset == SettingsPresets.PresetName.Random)
                {
                    WriteDebugMessage("Save");
                    SettingExtensions.SaveCustomSettings();
                }

                // Apply newly selected preset
                SettingsPresets.ApplyPreset(newPreset, config);
                foreach (Settings setting in allSettings)
                {
                    setting.SetValue(config.GetSettingsValue<object>(setting.GetName()));
                }
            }

            // Display changes
            RedrawSettingsMenu();
        }
        // Something other than a preset was changed
        else
        {
            var decoChanged = false;

            foreach (var pair in GetDecorationSettings())
            {
                var currentSetting = pair.Key.AsEnum<Settings>().GetValue();
                var newSetting = SettingExtensions.ConvertJValue(config.GetSettingsValue<object>(pair.Key));
                if (!currentSetting.Equals(newSetting))
                {
                    decoChanged = true;
                    break;
                }
            }

            var isCustom = false;

            foreach (Settings setting in allSettings)
            {
                // Detect changed settings
                setting.SetValue(config.GetSettingsValue<object>(setting.GetName()));

                // Check if the new values match the selected preset
                if (!isCustom && _currentPreset != SettingsPresets.PresetName.Custom && _currentPreset != SettingsPresets.PresetName.Random)
                {
                    isCustom = (_currentPreset.GetPresetSetting(setting.GetName()) != null
                        && !_currentPreset.GetPresetSetting(setting.GetName()).Equals(setting.GetValue()));
                }
            }

            // If the values no longer match the selected preset...
            if (isCustom)
            {
                // Switch to custom preset
                _currentPreset = SettingsPresets.PresetName.Custom;
                config.SetSettingsValue("preset", _currentPreset.GetName());
                foreach (Settings setting in allSettings)
                {
                    config.SetSettingsValue(setting.GetName(), setting.GetValue());
                }

                // Display changes
                RedrawSettingsMenu();
            }
            else if (decoChanged)
            {
                RedrawSettingsMenu(true);
            }
        }
    }

    private KeyValuePair<string, object>[] GetDecorationSettings()
    {
        int start = ModHelper.Config.Settings.Keys.ToList()
            .IndexOf("enableColorBlending");
        int end = ModHelper.Config.Settings.Keys.ToList()
            .IndexOf("indicatorColor3");

        var range = ModHelper.Config.Settings.ToList()
            .GetRange(start, end - start)
            .Where(pair =>
            !int.TryParse(pair.Key.Substring(pair.Key.Length - 1), out int i));
        return range.ToArray();
    }

    private void OnValueChanged(string name)
    {
        // Use later for more advanced settings menu reloading
    }

    public void RedrawSettingsMenu(bool decoration = false)
    {
        MenuManager menuManager = StartupPopupPatches.menuManager;
        IOptionsMenuManager OptionsMenuManager = menuManager.OptionsMenuManager;

        var menus = typeof(MenuManager).GetField("ModSettingsMenus", BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.Static).GetValue(menuManager)
            as List<(IModBehaviour behaviour, Menu modMenu)>;

        Menu newModTab = null;

        for (int i = 0; i < menus.Count; i++)
        {
            if ((object)menus[i].behaviour == this)
            {
                newModTab = menus[i].modMenu;
            }
        }

        if (newModTab == null) return;

        newModTab._menuOptions = [];

        Scrollbar scrollbar = newModTab.transform.Find("Scroll View/Scrollbar Vertical").GetComponent<Scrollbar>();
        float lastScrollValue = scrollbar.value;

        Transform settingsParent = newModTab.transform.Find("Scroll View/Viewport/Content");

        if (!DestroyExistingSettings(newModTab, settingsParent, decoration))
        {
            return;
        }

        if (!decoration)
        {
            OptionsMenuManager.AddSeparator(newModTab, true);
            OptionsMenuManager.CreateLabel(newModTab, "Any changes to the settings are applied on the next loop!");
            OptionsMenuManager.AddSeparator(newModTab, true);
        }

        bool blendEnabled = (bool)Settings.enableColorBlending.GetValue();

        int decoStartIndex = ModHelper.Config.Settings.Keys.ToList().IndexOf("enableColorBlending");
        int decoEndIndex = ModHelper.Config.Settings.Keys.ToList().IndexOf("indicatorColor3");

        int startIndex;
        int endIndex = ModHelper.Config.Settings.Count - 1;

        if (decoration)
        {
            startIndex = decoStartIndex;
        }
        else
        {
            startIndex = 0;
        }

        for (int i = startIndex; i <= endIndex; i++)
        {
            string name = ModHelper.Config.Settings.ElementAt(i).Key;

            if (ShouldHideSetting(i, name, decoration, decoStartIndex, decoEndIndex))
            {
                continue;
            }

            object setting = ModHelper.Config.Settings.ElementAt(i).Value;
            var settingType = GetSettingType(setting);
            var label = ModHelper.MenuTranslations.GetLocalizedString(name);
            var tooltip = "";

            var settingObject = setting as JObject;

            if (settingObject != default(JObject))
            {
                if (settingObject["dlcOnly"]?.ToObject<bool>() ?? false)
                {
                    if (EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.NotOwned)
                    {
                        continue;
                    }
                }

                if (settingObject["title"] != null)
                {
                    if (!SetCustomSettingName(ref label, name))
                    {
                        label = ModHelper.MenuTranslations.GetLocalizedString(settingObject["title"].ToString());
                    }
                }

                if (settingObject["tooltip"] != null)
                {
                    if (!SetCustomTooltip(ref tooltip, name))
                    {
                        tooltip = ModHelper.MenuTranslations.GetLocalizedString(settingObject["tooltip"].ToString());
                    }
                }
            }

            switch (settingType)
            {
                case SettingType.CHECKBOX:
                    var currentCheckboxValue = ModHelper.Config.GetSettingsValue<bool>(name);
                    var settingCheckbox = OptionsMenuManager.AddCheckboxInput(newModTab, label, tooltip, currentCheckboxValue);
                    settingCheckbox.ModSettingKey = name;
                    settingCheckbox.OnValueChanged += (bool newValue) =>
                    {
                        ModHelper.Config.SetSettingsValue(name, newValue);
                        ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
                        Configure(ModHelper.Config);
                        OnValueChanged(name);
                    };
                    break;
                case SettingType.TOGGLE:
                    var currentToggleValue = ModHelper.Config.GetSettingsValue<bool>(name);
                    var yes = settingObject["yes"].ToString();
                    var no = settingObject["no"].ToString();
                    var settingToggle = OptionsMenuManager.AddToggleInput(newModTab, label, yes, no, tooltip, currentToggleValue);
                    settingToggle.ModSettingKey = name;
                    settingToggle.OnValueChanged += (bool newValue) =>
                    {
                        ModHelper.Config.SetSettingsValue(name, newValue);
                        ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
                        Configure(ModHelper.Config);
                        OnValueChanged(name);
                    };
                    break;
                case SettingType.SELECTOR:
                    var currentSelectorValue = ModHelper.Config.GetSettingsValue<string>(name);
                    var options = settingObject["options"].ToArray().Select(x => x.ToString()).ToArray();
                    var currentSelectedIndex = Array.IndexOf(options, currentSelectorValue);
                    var settingSelector = OptionsMenuManager.AddSelectorInput(newModTab, label, options, tooltip, true, currentSelectedIndex);
                    settingSelector.ModSettingKey = name;
                    settingSelector.OnValueChanged += (int newIndex, string newSelection) =>
                    {
                        ModHelper.Config.SetSettingsValue(name, newSelection);
                        ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
                        Configure(ModHelper.Config);
                        OnValueChanged(name);
                    };
                    break;
                case SettingType.SEPARATOR:
                    OptionsMenuManager.AddSeparator(newModTab, true);
                    OptionsMenuManager.CreateLabel(newModTab, name);
                    OptionsMenuManager.AddSeparator(newModTab, false);
                    break;
                case SettingType.SLIDER:
                    var currentSliderValue = ModHelper.Config.GetSettingsValue<float>(name);
                    var lower = settingObject["min"].ToObject<float>();
                    var upper = settingObject["max"].ToObject<float>();
                    var settingSlider = OptionsMenuManager.AddSliderInput(newModTab, label, lower, upper, tooltip, currentSliderValue);
                    settingSlider.ModSettingKey = name;
                    settingSlider.OnValueChanged += (float newValue) =>
                    {
                        ModHelper.Config.SetSettingsValue(name, newValue);
                        ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
                        Configure(ModHelper.Config);
                        OnValueChanged(name);
                    };
                    break;
                case SettingType.TEXT:
                    var currentTextValue = ModHelper.Config.GetSettingsValue<string>(name);
                    var textInput = OptionsMenuManager.AddTextEntryInput(newModTab, label, currentTextValue, tooltip, false);
                    textInput.ModSettingKey = name;
                    textInput.OnConfirmEntry += () =>
                    {
                        var newValue = textInput.GetInputText();
                        ModHelper.Config.SetSettingsValue(name, newValue);
                        ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
                        Configure(ModHelper.Config);
                        textInput.SetText(newValue);
                        OnValueChanged(name);
                    };
                    break;
                case SettingType.NUMBER:
                    var currentValue = ModHelper.Config.GetSettingsValue<double>(name);
                    var numberInput = OptionsMenuManager.AddTextEntryInput(newModTab, label, currentValue.ToString(CultureInfo.CurrentCulture), tooltip, true);
                    numberInput.ModSettingKey = name;
                    numberInput.OnConfirmEntry += () =>
                    {
                        if (!string.IsNullOrEmpty(numberInput.GetInputText()))
                        {
                            var newValue = double.Parse(numberInput.GetInputText());
                            ModHelper.Config.SetSettingsValue(name, newValue);
                            ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
                            Configure(ModHelper.Config);
                            numberInput.SetText(newValue.ToString());
                            OnValueChanged(name);
                        }
                    };
                    break;
                default:
                    WriteDebugMessage($"Couldn't generate input for unkown input type {settingType}", error: true);
                    OptionsMenuManager.CreateLabel(newModTab, $"Unknown {settingType} : {name}");
                    break;
            }

            if (blendEnabled && i <= decoEndIndex)
            {
                string stem = name.Substring(0, name.Length - 6);
                if (_stemToSuffix.ContainsKey(stem) && name.Substring(name.Length - 1)
                    == (blendEnabled ? (string)(stem + "ColorOptions").AsEnum<Settings>().GetValue() : "1"))
                {
                    OptionsMenuManager.AddSeparator(newModTab, false);
                }
            }
        }


        if (newModTab._tooltipDisplay != null)
        {
            foreach (MenuOption option in newModTab.GetComponentsInChildren<MenuOption>(true))
            {
                option.SetTooltipDisplay(newModTab._tooltipDisplay);
            }
        }

        bool foundSelectable = false;
        newModTab._listSelectables = newModTab.GetComponentsInChildren<Selectable>(true);
        foreach (Selectable selectable in newModTab._listSelectables)
        {
            selectable.gameObject.GetAddComponent<Menu.MenuSelectHandler>().OnSelectableSelected += newModTab.OnMenuItemSelected;

            if (newModTab._lastSelected != null
                && selectable.gameObject.name == newModTab._lastSelected.gameObject.name)
            {
                SelectableAudioPlayer component = newModTab._selectOnActivate.GetComponent<SelectableAudioPlayer>();
                if (component != null)
                {
                    component.SilenceNextSelectEvent();
                }
                Locator.GetMenuInputModule().SelectOnNextUpdate(selectable);
                foundSelectable = true;
            }
        }

        if (!foundSelectable && newModTab._selectOnActivate != null)
        {
            SelectableAudioPlayer component = newModTab._selectOnActivate.GetComponent<SelectableAudioPlayer>();
            if (component != null)
            {
                component.SilenceNextSelectEvent();
            }
            Locator.GetMenuInputModule().SelectOnNextUpdate(newModTab._selectOnActivate);
            newModTab._lastSelected = newModTab._selectOnActivate;
        }

        if (newModTab._setMenuNavigationOnActivate)
        {
            Menu.SetVerticalNavigation(newModTab, newModTab._menuOptions);
        }

        ModHelper.Events.Unity.FireInNUpdates(() =>
        {
            scrollbar.value = lastScrollValue;
        }, 2);
    }

    private bool DestroyExistingSettings(Menu menu, Transform parent, bool decoration = false)
    {
        if (decoration)
        {
            int startIndex = -1;
            int endIndex = -1;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == "UIElement-Enable Color Blending")
                {
                    startIndex = i;
                }

                if (startIndex < 0)
                {
                    MenuOption option = parent.GetChild(i).GetComponentInChildren<MenuOption>();
                    if (option != null)
                    {
                        menu._menuOptions = menu._menuOptions.Add(option);
                    }
                }
                else
                {
                    //ShipEnhancements.WriteDebugMessage("Destroy " + parent.GetChild(i).name);
                    Destroy(parent.GetChild(i).gameObject);
                }

                if (parent.GetChild(i).name == "UIElement-Damage Indicator Color")
                {
                    endIndex = i;
                }
            }

            return startIndex >= 0;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            if (i < 2)
            {
                MenuOption option = parent.GetChild(i).GetComponentInChildren<MenuOption>();
                if (option != null)
                {
                    menu._menuOptions = menu._menuOptions.Add(option);
                }
            }
            else
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        return true;
    }

    private bool ShouldHideSetting(int currIndex, string name, bool onlyDecoration, int decoStartIndex, int decoEndIndex)
    {
        foreach (Settings hiddenSetting in HiddenSettings)
        {
            if (hiddenSetting.ToString() == name)
            {
                return true;
            }
        }

        if (!onlyDecoration)
        {
            if (_currentPreset != SettingsPresets.PresetName.Random)
            {
                if (name == "randomIterations" || name == "randomDifficulty")
                {
                    return true;
                }
            }
        }

        if (currIndex <= decoEndIndex)
        {
            if (currIndex > decoStartIndex && !(bool)Settings.enableColorBlending.GetValue()
                && (!int.TryParse(name.Substring(name.Length - 1), out int value) || value != 1))
            {
                return true;
            }

            if (name.Length >= 6)
            {
                string stem = name.Substring(0, name.Length - 6);
                if (_stemToSuffix.ContainsKey(stem))
                {
                    Settings numSetting = (stem + "ColorOptions").AsEnum<Settings>();
                    int num = int.Parse((string)numSetting.GetValue());
                    if (int.Parse(name.Substring(name.Length - 1)) > num)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool SetCustomSettingName(ref string label, string settingName)
    {
        if (!(bool)Settings.enableColorBlending.GetValue()) return false;

        string stem = settingName.Substring(0, settingName.Length - 6);
        if (!_stemToSuffix.ContainsKey(stem))
        {
            return false;
        }

        Settings numSetting = (stem + "ColorOptions").AsEnum<Settings>();
        int num = int.Parse((string)numSetting.GetValue());
        if (num == 1)
        {
            return false;
        }

        int index = int.Parse(settingName.Substring(settingName.Length - 1));
        Settings blendSetting = (stem + "ColorBlend").AsEnum<Settings>();
        string blend = (string)blendSetting.GetValue();

        var found = _customSettingNames.Where(tuple => tuple.blendType == blend 
            && tuple.canShow(index, num));

        if (found.Count() > 0)
        {
            label = _stemToSuffix[stem] + " " + found.First().suffix;
            return true;
        }

        return false;
    }

    private bool SetCustomTooltip(ref string tooltip, string settingName)
    {
        if (settingName == "preset")
        {
            if (_currentPreset == SettingsPresets.PresetName.VanillaPlus)
            {
                tooltip = "Vanilla Plus is the default preset. It turns everything off except for some Quality of Life features.";
            }
            else if (_currentPreset == SettingsPresets.PresetName.Minimal)
            {
                tooltip = "The Minimal preset disables anything related to the ship that you could consider useful.";
            }
            else if (_currentPreset == SettingsPresets.PresetName.Impossible)
            {
                tooltip = "The Impossible preset doesn't add or disable anything, but it changes the ship to be as annoying as possible.";
            }
            else if (_currentPreset == SettingsPresets.PresetName.NewStuff)
            {
                tooltip = "The New Stuff preset gives the ship a ton of new features that it doesn't normally have.";
            }
            else if (_currentPreset == SettingsPresets.PresetName.Pandemonium)
            {
                tooltip = "The Pandemonium preset just turns everything on. Good luck.";
            }
            else if (_currentPreset == SettingsPresets.PresetName.Random)
            {
                tooltip = "The Random preset randomizes the mod settings each loop. You can customize the randomizer by using the two sliders below or by using the RandomizerSettings.json file in the mod folder.";
            }
            else if (_currentPreset == SettingsPresets.PresetName.Custom)
            {
                tooltip = "No preset is selected. Customize your ship to your heart's desire.";
            }

            return true;
        }

        if (settingName.Substring(settingName.Length - 5, 5) != "Blend")
        {
            return false;
        }

        Settings blendSetting = settingName.AsEnum<Settings>();
        tooltip = _customTooltips[(string)blendSetting.GetValue()];
        return true;
    }

    private SettingType GetSettingType(object setting)
    {
        var settingObject = setting as JObject;

        if (setting is bool || (settingObject != null && settingObject["type"].ToString() == "toggle" && (settingObject["yes"] == null || settingObject["no"] == null)))
        {
            return SettingType.CHECKBOX;
        }
        else if (setting is string || (settingObject != null && settingObject["type"].ToString() == "text"))
        {
            return SettingType.TEXT;
        }
        else if (setting is int || setting is long || setting is float || setting is double || setting is decimal || (settingObject != null && settingObject["type"].ToString() == "number"))
        {
            return SettingType.NUMBER;
        }
        else if (settingObject != null && settingObject["type"].ToString() == "toggle")
        {
            return SettingType.TOGGLE;
        }
        else if (settingObject != null && settingObject["type"].ToString() == "selector")
        {
            return SettingType.SELECTOR;
        }
        else if (settingObject != null && settingObject["type"].ToString() == "slider")
        {
            return SettingType.SLIDER;
        }
        else if (settingObject != null && settingObject["type"].ToString() == "separator")
        {
            return SettingType.SEPARATOR;
        }

        WriteDebugMessage($"Couldn't work out setting type. Type:{setting.GetType().Name} SettingObjectType:{settingObject?["type"].ToString()}", error: true);
        return SettingType.NONE;
    }

    enum SettingType
    {
        NONE,
        CHECKBOX,
        TOGGLE,
        TEXT,
        NUMBER,
        SELECTOR,
        SLIDER,
        SEPARATOR
    }

    public override object GetApi()
    {
        return new ShipEnhancementsAPI();
    }
}
