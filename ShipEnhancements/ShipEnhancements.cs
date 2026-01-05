using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Data;
using OWML.Utils;
using System.Reflection;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.UI;
using Newtonsoft.Json;
using ShipEnhancements.Models.Json;
using ShipEnhancements.ModMenu;
using ShipEnhancements.Textures;
using ShipEnhancements.Utils;
using UnityEngine.Experimental.Rendering;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipEnhancements : ModBehaviour
{
    public delegate void SwitchEvent(bool enabled);
    public event SwitchEvent OnGravityLandingGearSwitch;
    public event SwitchEvent OnGravityLandingGearInverted;

    public delegate void ResourceEvent(string resource);
    public event ResourceEvent OnResourceDepleted;
    public event ResourceEvent OnResourceRestored;

    public delegate void EngineEvent(bool enabled);
    public event EngineEvent OnEngineStateChanged;

    public UnityEvent PreShipInitialize;
    public UnityEvent PostShipInitialize;

    public static ShipEnhancements Instance;
    public bool oxygenDepleted;
    public bool refillingOxygen;
    public bool fuelDepleted;
    public bool waterDepleted;
    public float levelOneSpinSpeed = 8f;
    public float levelTwoSpinSpeed = 16f;
    public float maxSpinSpeed = 24f;
    public bool probeDestroyed;
    public bool engineOn;
    public Tether playerTether;
    public bool anyPartDamaged;
    public bool groundedByHornfels;
    public bool shipIgniting;
    public bool disableShipHeadlights;
    public bool shipLoaded = false;

    public static IAchievements AchievementsAPI;
    public static IQSBAPI QSBAPI;
    public static QSBCompatibility QSBCompat;
    public static IQSBInteraction QSBInteraction;
    public static INewHorizons NHAPI;
    public static INHInteraction NHInteraction;
    public static IGEInteraction GEInteraction;
    public static ThemeManager ThemeManager;
    public static ExperimentalSettingsJson ExperimentalSettings;
    public static SaveDataJson SaveData;

    public static uint[] PlayerIDs
    {
        get
        {
            return QSBAPI.GetPlayerIDs().Where(id => id != QSBAPI.GetLocalPlayerID()).ToArray();
        }
    }

    private ShipResourceSyncManager _shipResourceSync;

    public static bool InMultiplayer => QSBAPI != null && QSBAPI.GetIsInMultiplayer();

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
    public ItemType ResourcePumpType { get; private set; }
    public SignalName ShipSignalName { get; private set; }
    public int ThrustModulatorLevel { get; private set; }
    public float ThrustModulatorFactor => ThrustModulatorLevel / 5f;
    public AudioClip ShipHorn { get; private set; }
    public List<AntiRiverVolume> AntiRiverVolumes { get; private set; } = [];

    public GameObject DebugObjects { get; private set; }

    public AssetBundle _shipEnhancementsBundle;
    private float _lastSuitOxygen;
    private bool _shipDestroyed;
    private bool _checkEndConversation = false;
    private bool _setupQSB = false;
    private bool _disableAirWhenZeroOxygen = false;
    private bool _unsubFromBodyLoaded = false;
    private bool _unsubFromSystemLoaded = false;
    private bool _unsubFromShipSpawn = false;
    private ShipDetachableLeg _frontLeg = null;
    private List<OWAudioSource> _shipAudioToChange = [];

    public Material textureBlendMat;
    
    private Material _defaultInteriorHullMat;
    private Material _defaultExteriorHullMat;
    private Material _defaultInteriorWoodMat;
    private Material _defaultExteriorWoodMat;
    private Material _defaultGlassMat;
    private Material _customGlassMat;
    private Material _defaultSEInteriorMat1;
    private Material _defaultSEInteriorMat2;

    private readonly Dictionary<Material, ShipTextureBlender> _textureBlenders = new();

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
        enableShipFuelTransfer,
        enableJetpackRefuelDrain,
        disableReferenceFrame,
        disableMapMarkers,
        gravityMultiplier,
        fuelTransferMultiplier,
        oxygenRefillMultiplier,
        temperatureResistanceMultiplier,
        enableAutoHatch,
        oxygenTankDrainMultiplier,
        fuelTankDrainMultiplier,
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
        enableRemovableGravityCrystal,
        randomHullDamage,
        randomComponentDamage,
        enableFragileShip,
        addErnesto,
        repairLimit,
        extraEjectButtons,
        preventSystemFailure,
        addShipCurtain,
        repairWrenchType,
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
        enableAutoAlign,
        shipHornType,
        randomIterations,
        randomDifficulty,
        disableHatch,
        splitLockOn,
        enableColorBlending,
        enableShipTemperature,
        temperatureDifficulty,
        passiveTemperatureGain,
        addResourcePump,
        addWaterTank,
        waterDrainMultiplier,
        addWaterCooling,
        enableReactorOverload,
        buttonsRequireFlightChair,
        enableQuantumShip,
        persistentShipState,
        enableGasLeak,
        interiorHullTexture,
        exteriorHullTexture,
        shipGlassTexture,
        interiorWoodTexture,
        interiorWoodColorOptions,
        interiorWoodColor1,
        interiorWoodColor2,
        interiorWoodColor3,
        interiorWoodColorBlend,
        exteriorWoodTexture,
        exteriorWoodColorOptions,
        exteriorWoodColor1,
        exteriorWoodColor2,
        exteriorWoodColor3,
        exteriorWoodColorBlend,
        fuelRegenerationMultiplier,
        disableSignalscopeBrackets,
        enableShipSignalscopeZoom,
        shipForceMultiplier,
        tractorBeamLengthMultiplier,
        shipPlantType,
        shipStringLights,
    }

    private readonly string[] startupMessages =
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
        "Did you know spiders see differently through each eye?",
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
        "Did you know Outer Wilds isn't scientifically accurate? This is because in Outer Wilds the planets are round, which doesn't match real life as the Earth is in fact-",
        "What's the hardest achievement to get in Outer Wilds?",
        "What's your favorite planet in Outer Wilds?",
        "Have you played the mod Nomai's Sky? It's aims to mimic the game No Man's Sky, a space exploration game with literal billions of procedurally generated galaxies to explore.",
        "Which Outer Wilds modding jam is your favorite?",
        "Which mod has your favorite Ernesto easter egg?",
        "Which traveler is your favorite?",
        "Contrary to common belief, licking uranium is actually very dangerous.",
        "The Grand Canyon is over a mile deep, and yet humans can be killed by a 10-foot drop. Isn't that crazy?",
        "What's your favorite species of tree?",
        "77 + 33 = 100",
        "Did you know that cheetahs can actually run faster than turtles?",
        "If you arranged elephants in a line from Earth to the Moon, most of them would die.",
        "If you were to place every ant on Earth into a bucket, it would fill the bucket.",
        "Did you know \"ample\" is one of the words in the English language to start with \"a\"?",
        "Have you played The Legend of Zelda: Breath of the Wild? Outer Wilds was partially inspried by that game.",
        "Instead of saying you had pizza for dinner, say you had aged organic milk tossed over seasoned tomato purée spread on baked whole wheat.",
        "Have you heard of the game Blue Prince? It's really cool. If you're into puzzles, I would suggest playing it or watching a playthrough.",
        "Have you heard of Aliensrock? They make YouTube videos on a lot of puzzle/strategy games.",
        "If your keyboard has a number pad, you can turn on \"num lock\" and hold Alt while pressing numbers to insert special symbols (™, °, ¼, etc).\nHere are some codes: 0153 (™), 0176 (°), 0149 (•), 0173 (empty character)",
        "Did you know the smallest angle of a triangle is always adjacent to the hypotenuse?",
        "At what age do you become \"old\"? 30? 50? 80?",
        "Have you heard of Kane Pixels? He popularized the \"found footage\" videos of The Backrooms."
    };

    private void Awake()
    {
        Instance = this;
        new HarmonyLib.Harmony("Etherpod.ShipEnhancements").PatchAll(Assembly.GetExecutingAssembly());

        if (true)
        {
            gameObject.AddComponent<PersistentShipState>();
        }
    }

    private void Start()
    {
        _shipEnhancementsBundle = AssetBundle.LoadFromFile(Path.Combine(ModHelper.Manifest.ModFolderPath, "assets/shipenhancements"));
        ThemeManager = new ThemeManager("ShipEnhancements.Data.themes.json");
        
        textureBlendMat = LoadMaterial("Assets/ShipEnhancements/ShipSkins/ShipTextureBlend.mat");

        InitializeAchievements();
        InitializeQSB();
        InitializeNH();
        InitializeGE();
        ModCompatibility.InitCompatibility();
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
        ResourcePumpType = EnumUtils.Create<ItemType>("ResourcePump");
        ShipSignalName = EnumUtils.Create<SignalName>("Ship");

        SEItemAudioController.Initialize();

        PrintStartupMessage();

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

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
            UpdateExperimentalSettings();

            SaveData = ModHelper.Storage.Load<SaveDataJson>("save.json");
            if (SaveData == null)
            {
                SaveData = new SaveDataJson();
                ModHelper.Storage.Save(SaveData, "save.json");
            }

            if (AchievementsAPI != null)
            {
                SEAchievementTracker.Reset();
            }

            new GameObject("SE_PatchHandler").AddComponent<PatchHandler>();

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
                        if (SEMenuManager.CurrentPreset == SettingsPresets.PresetName.Random)
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
            if ((float)spaceAngularDragMultiplier.GetProperty() > 0 || (float)atmosphereAngularDragMultiplier.GetProperty() > 0)
            {
                ShipFluidDetector detector = SELocator.GetShipDetector().GetComponent<ShipFluidDetector>();
                detector.OnEnterFluid -= OnEnterFluid;
                detector.OnExitFluid -= OnExitFluid;
            }
            /*if ((bool)enableAutoHatch.GetProperty() && !InMultiplayer && !(bool)disableHatch.GetProperty())
            {
                GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
                GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
            }*/
            if ((bool)enableRepairConfirmation.GetProperty() || (bool)enableFragileShip.GetProperty())
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
            if ((bool)extraNoise.GetProperty())
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
            /*if (NHAPI != null && _unsubFromBodyLoaded)
            {
                NHAPI.GetBodyLoadedEvent().RemoveListener(OnNHBodyLoaded);
                _unsubFromBodyLoaded = false;
            }
            if (NHAPI != null && _unsubFromSystemLoaded)
            {
                NHAPI.GetStarSystemLoadedEvent().RemoveListener(OnNHStarSystemLoaded);
                _unsubFromSystemLoaded = false;
            }*/
            if (NHAPI != null && _unsubFromShipSpawn)
            {
                NHAPI.GetStarSystemLoadedEvent().RemoveListener(SetCustomWarpDestination);
                _unsubFromShipSpawn = false;
            }

            CustomMatManager.ClearMaterials(true);

            bool skipSettings = GetComponent<PersistentShipState>()?.PreserveSettings ?? false;
            if ((!InMultiplayer || QSBAPI.GetIsHost()) && !skipSettings)
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
            shipLoaded = false;
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

    private void UpdateExperimentalSettings()
    {
        var data = JsonConvert.DeserializeObject<ExperimentalSettingsJson>(
            File.ReadAllText(Path.Combine(ModHelper.Manifest.ModFolderPath, "ExperimentalSettings.json"))
        );
        ExperimentalSettings = data;
        ShipEnhancements.WriteDebugMessage(data);
    }

    public static void UpdateSaveFile()
    {
        if (SaveData == null) SaveData = new();
        Instance.ModHelper.Storage.Save(SaveData, "save.json");
    }

    private void Update()
    {
        if (!shipLoaded || LoadManager.GetCurrentScene() != OWScene.SolarSystem || _shipDestroyed) return;

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

        if (!(bool)addEngineSwitch.GetProperty())
        {
            if ((float)idleFuelConsumptionMultiplier.GetProperty() != 0f)
            {
                SELocator.GetShipResources().DrainFuel((float)idleFuelConsumptionMultiplier.GetProperty() * Time.deltaTime / 2f);
            }

            if ((float)fuelRegenerationMultiplier.GetProperty() != 0f)
            {
                SELocator.GetShipResources().DrainFuel((float)idleFuelConsumptionMultiplier.GetProperty() * Time.deltaTime * 2.5f);
            }
        }

        if (!oxygenDepleted && SELocator.GetShipResources().GetOxygen() <= 0 && !((bool)shipOxygenRefill.GetProperty() && IsShipInOxygen()))
        {
            oxygenDepleted = true;

            ShipNotifications.OnOxygenDepleted();

            if (PlayerState.IsInsideShip())
            {
                if ((bool)keepHelmetOn.GetProperty() && PlayerState.IsWearingSuit() && !Locator.GetPlayerSuit().IsWearingHelmet())
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

            OnResourceDepleted?.Invoke("oxygen");
        }
        else if (oxygenDepleted && (SELocator.GetShipResources().GetOxygen() > 0 || ((bool)shipOxygenRefill.GetProperty() && IsShipInOxygen())))
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

            OnResourceRestored?.Invoke("oxygen");
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
            OnResourceDepleted?.Invoke("fuel");
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
            OnResourceRestored?.Invoke("fuel");
        }

        if ((bool)addWaterTank.GetProperty())
        {
            if (!waterDepleted && SELocator.GetShipWaterResource().GetWater() <= 0f)
            {
                waterDepleted = true;
                ShipNotifications.OnWaterDepleted();
                OnResourceDepleted?.Invoke("water");
            }
            else if (waterDepleted && SELocator.GetShipWaterResource().GetWater() > 0f)
            {
                waterDepleted = false;
                ShipNotifications.OnWaterRestored();
                OnResourceRestored?.Invoke("water");
            }
        }

        if ((bool)showWarningNotifications.GetProperty() && !_shipDestroyed)
        {
            ShipNotifications.UpdateNotifications();
        }

        if (!SEAchievementTracker.DeadInTheWater && AchievementsAPI != null)
        {
            bool noFuel = !(!fuelDepleted || (bool)enableShipFuelTransfer.GetProperty() ||
                (float)fuelDrainMultiplier.GetProperty() < 0f ||
                (float)fuelTankDrainMultiplier.GetProperty() < 0f ||
                (float)idleFuelConsumptionMultiplier.GetProperty() < 0f ||
                (float)fuelRegenerationMultiplier.GetProperty() > 0f);
            bool noOxygen = (bool)disableShipOxygen.GetProperty()
                || !(!oxygenDepleted || (bool)shipOxygenRefill.GetProperty()
                || (float)oxygenDrainMultiplier.GetProperty() < 0f
                || (float)oxygenTankDrainMultiplier.GetProperty() < 0f);

            if (noFuel && noOxygen)
            {
                SEAchievementTracker.DeadInTheWater = true;
                AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.DEAD_IN_THE_WATER");
            }
        }
    }

    private void LateUpdate()
    {
        if (!shipLoaded || LoadManager.GetCurrentScene() != OWScene.SolarSystem) return;

        if (!SELocator.GetPlayerResources()._refillingOxygen && refillingOxygen)
        {
            refillingOxygen = false;
        }
    }

    private void FixedUpdate()
    {
        if (!shipLoaded || _shipDestroyed) return;

        if (InputLatencyController.ReadingSavedInputs && InputLatencyController.IsInputQueued)
        {
            InputLatencyController.ProcessSavedInputs();
        }
        else if ((float)shipInputLatency.GetProperty() > 0f)
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
                    ShipEnhancements.WriteDebugMessage(setting.GetName() + " : " + SettingsPresets.RandomSettings[setting.GetName()].GetRandomChance());
                    total += SettingsPresets.RandomSettings[setting.GetName()].GetRandomChance();
                }
            }

            List<Settings> temp = new();
            temp.AddRange(settingsToRandomize);

            int iterations = Mathf.FloorToInt(
                Mathf.Lerp(Mathf.Min(2f, settingsToRandomize.Count), settingsToRandomize.Count, (float)randomIterations.GetValue()));

            for (int j = 0; j < iterations; j++)
            {
                float rand = UnityEngine.Random.Range(0f, total);
                float sum = 0;
                for (int k = 0; k < settingsToRandomize.Count; k++)
                {
                    if (SettingsPresets.RandomSettings.ContainsKey(settingsToRandomize[k].GetName()))
                    {
                        float add = SettingsPresets.RandomSettings[settingsToRandomize[k].GetName()].GetRandomChance();
                        sum += add;
                        if (rand <= sum)
                        {
                            //ShipEnhancements.WriteDebugMessage("randomizing " + settingsToRandomize[k].GetName());
                            settingsToRandomize[k].SetProperty(SettingsPresets.RandomSettings[settingsToRandomize[k].GetName()]
                                .GetRandomValue());
                            total -= add;
                            settingsToRandomize.RemoveAt(k);
                            break;
                        }
                    }
                }
            }

            foreach (var set in temp)
            {
                WriteDebugMessage("randomized " + set.GetName() + " to " + set.GetProperty());
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

            NHAPI.GetBodyLoadedEvent().AddListener(OnNHBodyLoaded);
            _unsubFromBodyLoaded = true;
            NHAPI.GetStarSystemLoadedEvent().AddListener(OnNHStarSystemLoaded);
            _unsubFromSystemLoaded = true;
        }
    }

    public void AssignNHInterface(INHInteraction nhInterface)
    {
        NHInteraction = nhInterface;
    }

    private void InitializeGE()
    {
        bool geEnabled = ModHelper.Interaction.ModExists("SBtT.GeneralEnhancements");
        if (geEnabled)
        {
            var geAssembly = Assembly.LoadFrom(Path.Combine(ModHelper.Manifest.ModFolderPath, "ShipEnhancementsGE.dll"));
            gameObject.AddComponent(geAssembly.GetType("ShipEnhancementsGE.GEInteraction", true));
        }
    }

    public void AssignGEInterface(IGEInteraction geInterface)
    {
        GEInteraction = geInterface;
    }

    private void InitializeShip()
    {
        WriteDebugMessage("Initialize Ship");

        SELocator.Initalize();
        ThrustIndicatorManager.Initialize();
        ShipRepairLimitController.Initialize();

        // DumpMats("D:/misc/files/mats_01.json");

        SELocator.GetShipBody().GetComponentInChildren<ShipCockpitController>()
            ._interactVolume.gameObject.AddComponent<FlightConsoleInteractController>();

        var debugObjectsPrefab = LoadPrefab("Assets/ShipEnhancements/DebugObjects.prefab");
        DebugObjects = CreateObject(debugObjectsPrefab, SELocator.GetShipBody().transform);

        GameObject buttonConsole = LoadPrefab("Assets/ShipEnhancements/ButtonConsole.prefab");
        CreateObject(buttonConsole, SELocator.GetShipBody().transform.Find("Module_Cockpit"));
        
        if (_defaultInteriorHullMat == null)
        {
            MeshRenderer suppliesRenderer = SELocator.GetShipTransform().
                Find("Module_Supplies/Geo_Supplies/Supplies_Geometry/Supplies_Interior").GetComponent<MeshRenderer>();
            _defaultInteriorHullMat = suppliesRenderer.sharedMaterials[0];
        }
        
        if (_defaultExteriorHullMat == null)
        {
            MeshRenderer cabinRenderer = SELocator.GetShipTransform().
                Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Exterior").GetComponent<MeshRenderer>();
            _defaultExteriorHullMat = cabinRenderer.sharedMaterials[3];
        }

        if (_defaultGlassMat == null)
        {
            MeshRenderer cockpitRenderer = SELocator.GetShipTransform()
                .Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior/CockpitExterior_GoldGlass")
                .GetComponent<MeshRenderer>();
            _defaultGlassMat = cockpitRenderer.sharedMaterial;
        }
        _customGlassMat = new Material(_defaultGlassMat);
        
        if (_defaultInteriorWoodMat == null)
        {
            MeshRenderer suppliesRenderer = SELocator.GetShipTransform().
                            Find("Module_Supplies/Geo_Supplies/Supplies_Geometry/Supplies_Interior").GetComponent<MeshRenderer>();
            _defaultInteriorWoodMat = suppliesRenderer.sharedMaterials[2];
        }
        
        if (_defaultExteriorWoodMat == null)
        {
            MeshRenderer cabinRenderer = SELocator.GetShipTransform().
                Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Exterior").GetComponent<MeshRenderer>();
            _defaultExteriorWoodMat = cabinRenderer.sharedMaterials[2];
        }

        if (_defaultSEInteriorMat1 == null)
        {
            _defaultSEInteriorMat1 = LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_VillageCabin_Recolored_mat.mat");
        }

        if (_defaultSEInteriorMat2 == null)
        {
            _defaultSEInteriorMat2 = LoadMaterial("Assets/ShipEnhancements/ShipInterior_SE_VillageCabin_mat.mat");
        }

        CustomMatManager.ClearMaterials(true);
        
        Material[] lightmapMaterials =
        {
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_SE_VillageCabin_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_VillageCabin_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_SE_VillageMetal_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_VillageMetal_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_VillagePlanks_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_SE_CampsiteProps_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_CampsiteProps_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_SE_SignsDecal_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_SignsDecal_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_VillageCloth_Recolored_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_NOM_CopperOld_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_NOM_Sandstone_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/CockpitWindowFrost_Material.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipInterior_HEA_WaterGaugeMetal_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipPlants/ShipInterior_Cactus_mat.mat"),
            LoadMaterial("Assets/ShipEnhancements/ShipPlants/ShipInterior_CactusFlower_mat.mat"),
            _customGlassMat,
        };

        foreach (var mat in lightmapMaterials)
        {
            LightmapManager.AddMaterial(mat);
        }
        
        /*Transform cockpitLight = SELocator.GetShipTransform().Find("Module_Cockpit/Lights_Cockpit/Pointlight_HEA_ShipCockpit");
        List<Material> materials = [.. cockpitLight.GetComponent<LightmapController>()._materials];
        materials.AddRange(newMaterials);
        cockpitLight.GetComponent<LightmapController>()._materials = [.. materials];

        Transform shipLogLight = SELocator.GetShipTransform().Find("Module_Cabin/Lights_Cabin/Pointlight_HEA_ShipCabin");
        List<Material> materials2 = [.. shipLogLight.GetComponent<LightmapController>()._materials];
        materials2.AddRange(newMaterials);
        shipLogLight.GetComponent<LightmapController>()._materials = [.. materials2];

        Transform suppliesLight = SELocator.GetShipTransform().Find("Module_Supplies/Lights_Supplies/Pointlight_HEA_ShipSupplies_Top");
        List<Material> materials3 = [.. suppliesLight.GetComponent<LightmapController>()._materials];
        materials3.AddRange(newMaterials);
        suppliesLight.GetComponent<LightmapController>()._materials = [.. materials3];*/

        MeshRenderer chassisRenderer = SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Interior/Cockpit_Interior_Chassis")
            .GetComponent<MeshRenderer>();
        Texture2D blackTex = LoadAsset<Texture2D>("Assets/ShipEnhancements/Black_d.png");
        chassisRenderer.sharedMaterials[6].SetTexture("_OcclusionMap", blackTex);
        chassisRenderer.sharedMaterials[6].SetFloat("_OcclusionStrength", 0.75f);

        if ((bool)enableScoutLauncherComponent.GetProperty()
            || (string)shipWarpCoreType.GetProperty() == "Component")
        {
            Transform damageScreen = SELocator.GetShipTransform().Find("Module_Cockpit/Systems_Cockpit/ShipCockpitUI/DamageScreen/HUD_ShipDamageDisplay");
            if ((bool)enableScoutLauncherComponent.GetProperty())
            {
                GameObject scoutDamage = LoadPrefab("Assets/ShipEnhancements/HUD_ShipDamageDisplay_Scout.prefab");
                scoutDamage.GetComponent<MeshRenderer>().material = damageScreen.GetComponent<MeshRenderer>().material;
                CreateObject(scoutDamage, damageScreen.parent);
            }
            if ((string)shipWarpCoreType.GetProperty() == "Component")
            {
                GameObject warpDamage = LoadPrefab("Assets/ShipEnhancements/HUD_ShipDamageDisplay_Warp.prefab");
                warpDamage.GetComponent<MeshRenderer>().material = damageScreen.GetComponent<MeshRenderer>().material;
                CreateObject(warpDamage, damageScreen.parent);
            }
        }
        
        SELocator.GetShipDamageController()._shipReactorComponent.gameObject.AddComponent<ReactorHeatController>();
        
        GameObject cockpitController = LoadPrefab("Assets/ShipEnhancements/CockpitEffectController.prefab");
        CreateObject(cockpitController, SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry"));

        AntiRiverVolumes.Clear();
        
        GameObject darkSideVol = LoadPrefab("Assets/ShipEnhancements/AntiRiverVolume_DarkSideDockingBay.prefab");
        GameObject lightSideVol = LoadPrefab("Assets/ShipEnhancements/AntiRiverVolume_LightSideDockingBay.prefab");
        AntiRiverVolumes.Add(CreateObject(darkSideVol,
            GameObject.Find("RingWorld_Body").transform
                .Find("Sector_RingWorld/Sector_DarkSideDockingBay/Volumes_DarkSideDockingBay"))
            .GetComponent<AntiRiverVolume>());
        AntiRiverVolumes.Add(CreateObject(lightSideVol,
            GameObject.Find("RingWorld_Body").transform
                .Find("Sector_RingWorld/Sector_LightSideDockingBay/Volumes_LightSideDockingBay"))
            .GetComponent<AntiRiverVolume>());

        SetUpShipLogSplashScreen();

        SetUpShipAudio();

        foreach (OWAudioSource audio in _shipAudioToChange)
        {
            audio.spatialBlend = 1f;
        }

        shipLoaded = true;
        UpdateSuitOxygen();

        if (AchievementsAPI != null)
        {
            SELocator.GetShipDamageController().OnShipComponentDamaged += ctx => CheckAllPartsDamaged();
            SELocator.GetShipDamageController().OnShipHullDamaged += ctx => CheckAllPartsDamaged();
        }

        if ((bool)disableHeadlights.GetProperty())
        {
            DisableHeadlights();
        }
        if ((bool)disableLandingCamera.GetProperty())
        {
            DisableLandingCamera();
        }
        else
        {
            _frontLeg = SELocator.GetShipTransform().Find("Module_LandingGear/LandingGear_Front")
                .GetComponent<ShipDetachableLeg>();
            _frontLeg.OnLegDetach += OnFrontLegDetached;
        }

        bool coloredLights = (string)shipLightColor1.GetProperty() != "Default";
        bool blendingLights = ((bool)enableColorBlending.GetProperty()
            && int.Parse((string)shipLightColorOptions.GetProperty()) > 1)
            || (string)shipLightColor1.GetProperty() == "Rainbow";

        if ((bool)disableShipLights.GetProperty() || coloredLights || blendingLights)
        {
            Color lightColor = Color.white;
            if (coloredLights && !blendingLights)
            {
                lightColor = ThemeManager.GetLightTheme(
                    (string)shipLightColor1.GetProperty())
                    .LightColor / 255f;
            }

            foreach (ElectricalSystem system in SELocator.GetShipBody().GetComponentsInChildren<ElectricalSystem>())
            {
                foreach (ElectricalComponent component in system._connectedComponents)
                {
                    if (component.gameObject.name.Contains("Pointlight") && component.TryGetComponent(out ShipLight light))
                    {
                        if ((bool)disableShipLights.GetProperty())
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
                if ((bool)disableShipLights.GetProperty())
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
                    if ((string)shipLightColor1.GetProperty() == "Rainbow")
                    {
                        beacon.gameObject.AddComponent<ShipLightBlendController>();
                    }
                }
            }
        }
        if ((bool)disableShipOxygen.GetProperty())
        {
            SELocator.GetShipResources().SetOxygen(0f);
            oxygenDepleted = true;
        }
        if ((bool)enableShipFuelTransfer.GetProperty())
        {
            GameObject transferVolume = LoadPrefab("Assets/ShipEnhancements/FuelTransferVolume.prefab");
            CreateObject(transferVolume, SELocator.GetShipBody().GetComponentInChildren<ShipFuelTankComponent>().transform);
        }
        if ((float)gravityMultiplier.GetProperty() != 1f && !(bool)disableGravityCrystal.GetProperty())
        {
            ShipDirectionalForceVolume shipGravity = SELocator.GetShipBody().GetComponentInChildren<ShipDirectionalForceVolume>();
            shipGravity._fieldMagnitude *= (float)gravityMultiplier.GetProperty();
        }
        if ((bool)enableAutoHatch.GetProperty() && !InMultiplayer && !(bool)disableHatch.GetProperty())
        {
            /*GlobalMessenger.AddListener("EnterShip", OnEnterShip);
            GlobalMessenger.AddListener("ExitShip", OnExitShip);*/
            GameObject autoHatchController = LoadPrefab("Assets/ShipEnhancements/ExteriorHatchControls.prefab");
            CreateObject(autoHatchController, SELocator.GetShipBody().GetComponentInChildren<HatchController>().transform.parent);
        }
        if ((float)spaceAngularDragMultiplier.GetProperty() > 0 || (float)atmosphereAngularDragMultiplier.GetProperty() > 0)
        {
            ShipFluidDetector detector = SELocator.GetShipDetector().GetComponent<ShipFluidDetector>();
            detector.OnEnterFluid += OnEnterFluid;
            detector.OnExitFluid += OnExitFluid;
        }
        if ((string)gravityDirection.GetProperty() != "Down" && !(bool)disableGravityCrystal.GetProperty())
        {
            ShipDirectionalForceVolume shipGravity = SELocator.GetShipBody().GetComponentInChildren<ShipDirectionalForceVolume>();
            Vector3 direction = Vector3.down;
            switch ((string)gravityDirection.GetProperty())
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
        if ((bool)enableManualScoutRecall.GetProperty() || (bool)disableScoutRecall.GetProperty() || (bool)disableScoutLaunching.GetProperty())
        {
            ShipProbeLauncherEffects launcherEffects = SELocator.GetShipBody().GetComponentInChildren<PlayerProbeLauncher>()
                .gameObject.AddComponent<ShipProbeLauncherEffects>();
            if ((bool)enableManualScoutRecall.GetProperty())
            {
                GameObject probePickupVolume = LoadPrefab("Assets/ShipEnhancements/PlayerProbePickupVolume.prefab");
                CreateObject(probePickupVolume, SELocator.GetProbe().transform);
            }
            GameObject shipProbePickupVolume = LoadPrefab("Assets/ShipEnhancements/ShipProbePickupVolume.prefab");
            GameObject shipProbeVolume = CreateObject(shipProbePickupVolume, launcherEffects.transform);

            if ((bool)enableScoutLauncherComponent.GetProperty())
            {
                SELocator.GetShipBody().GetComponentInChildren<ProbeLauncherComponent>().SetProbeLauncherEffects(launcherEffects);
            }
        }
        if ((bool)addPortableCampfire.GetProperty())
        {
            Transform suppliesParent = SELocator.GetShipTransform().Find("Module_Supplies");
            GameObject portableCampfireSocket = LoadPrefab("Assets/ShipEnhancements/PortableCampfireSocket.prefab");
            PortableCampfireSocket campfireSocket = CreateObject(portableCampfireSocket, suppliesParent).GetComponent<PortableCampfireSocket>();
        }
        if ((bool)enableShipTemperature.GetProperty())
        {
            SELocator.GetShipBody().GetComponentInChildren<ShipFuelGauge>().gameObject.AddComponent<ShipTemperatureGauge>();
            GameObject hullTempDial = LoadPrefab("Assets/ShipEnhancements/ShipTempDial.prefab");
            CreateObject(hullTempDial, SELocator.GetShipTransform().Find("Module_Cockpit"));

            if (temperatureZonesAmount.GetProperty().ToString() == "Sun")
            {
                SpawnSunTemperatureZones();
            }
            else
            {
                AddTemperatureZones();
            }
        }
        if ((float)shipExplosionMultiplier.GetProperty() != 1f)
        {
            Transform effectsTransform = SELocator.GetShipTransform().Find("Effects");
            ExplosionController explosion = effectsTransform.GetComponentInChildren<ExplosionController>();

            if ((float)shipExplosionMultiplier.GetProperty() < 0f)
            {
                GameObject newExplosion = LoadPrefab("Assets/ShipEnhancements/BlackHoleExplosion.prefab");
                GameObject newExplosionObj = CreateObject(newExplosion, effectsTransform);
                SELocator.GetShipDamageController()._explosion = newExplosionObj.GetComponent<ExplosionController>();
                Destroy(explosion.gameObject);
            }
            else if ((float)shipExplosionMultiplier.GetProperty() > 0f)
            {
                SetupExplosion(effectsTransform, explosion);
            }
        }
        if ((float)shipBounciness.GetProperty() > 1f || (bool)enableShipTemperature.GetProperty())
        {
            SELocator.GetShipTransform().gameObject.AddComponent<ModifiedShipHull>();
        }
        if ((bool)enableEnhancedAutopilot.GetProperty())
        {
            SELocator.GetShipBody().gameObject.AddComponent<ShipPersistentInput>();
            SELocator.GetShipBody().gameObject.AddComponent<PidAutopilot>();
        }
        if ((float)shipInputLatency.GetProperty() != 0f)
        {
            InputLatencyController.Initialize();
        }
        if ((bool)hotThrusters.GetProperty() || (bool)enableColorBlending.GetProperty()
            || (string)thrusterColor1.GetProperty() != "Default")
        {
            GameObject flameHazardVolume = LoadPrefab("Assets/ShipEnhancements/FlameHeatVolume.prefab");
            foreach (ThrusterFlameController flame in SELocator.GetShipTransform().GetComponentsInChildren<ThrusterFlameController>())
            {
                if ((bool)hotThrusters.GetProperty())
                {
                    GameObject volume = CreateObject(flameHazardVolume, Vector3.zero, Quaternion.identity, flame.transform);
                    volume.transform.localPosition = Vector3.zero;
                    volume.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    if (!flame.enabled)
                    {
                        flame.transform.localScale = Vector3.zero;
                    }
                }

                string color = (string)thrusterColor1.GetProperty();
                bool thrusterBlend = ((bool)enableColorBlending.GetProperty()
                    && int.Parse((string)thrusterColorOptions.GetProperty()) > 1)
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
                        LoadAsset<Texture2D>("Assets/ShipEnhancements/ThrusterColors/"
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
        if ((float)reactorLifetimeMultiplier.GetProperty() != 1f)
        {
            ShipReactorComponent reactor = SELocator.GetShipDamageController()._shipReactorComponent;

            float multiplier = Mathf.Max((float)reactorLifetimeMultiplier.GetProperty(), 0f);
            reactor._minCountdown *= multiplier;
            reactor._maxCountdown *= multiplier;
        }

        ApplyHullDecoration();
        SetGlassMaterial();
        SetShipPlantDecoration();
        SetStringLightDecoration();

        if ((bool)addTether.GetProperty())
        {
            /*GameObject hook = LoadPrefab("Assets/ShipEnhancements/TetherHook.prefab");
            AssetBundleUtilities.ReplaceShaders(hook);*/

            GameObject socketParent = CreateObject(LoadPrefab("Assets/ShipEnhancements/HookSocketParent.prefab"), SELocator.GetShipTransform());
            socketParent.transform.localPosition = Vector3.zero;
            /*foreach (TetherHookSocket socket in socketParent.GetComponentsInChildren<TetherHookSocket>())
            {
                GameObject hookItem = Instantiate(hook);
                socket.PlaceIntoSocket(hookItem.GetComponent<TetherHookItem>());
            }*/

            SELocator.GetPlayerBody().gameObject.AddComponent<TetherPromptController>();
            GameObject audio = LoadPrefab("Assets/ShipEnhancements/TetherAudioController.prefab");
            CreateObject(audio, SELocator.GetPlayerBody().transform);
        }
        if ((bool)extraEjectButtons.GetProperty())
        {
            GameObject suppliesButton = LoadPrefab("Assets/ShipEnhancements/SuppliesEjectButton.prefab");
            CreateObject(suppliesButton, SELocator.GetShipTransform().Find("Module_Cabin"));

            GameObject engineButton = LoadPrefab("Assets/ShipEnhancements/EngineEjectButton.prefab");
            CreateObject(engineButton, SELocator.GetShipTransform().Find("Module_Cabin"));

            GameObject landingGearButton = LoadPrefab("Assets/ShipEnhancements/LandingGearEjectButton.prefab");
            CreateObject(landingGearButton, SELocator.GetShipTransform().Find("Module_Cabin"));
        }
        if ((bool)addShipSignal.GetProperty())
        {
            GameObject signal = LoadPrefab("Assets/ShipEnhancements/ShipSignal.prefab");
            CreateObject(signal, SELocator.GetShipTransform().GetComponentInChildren<ShipCockpitUI>()._sigScopeDish);

            SELocator.GetPlayerBody().GetComponentInChildren<Signalscope>().gameObject.AddComponent<ShipRemoteControl>();
        }
        bool physicsBounce = (float)shipBounciness.GetProperty() > 0f && (float)shipBounciness.GetProperty() <= 1f;
        if ((float)shipFriction.GetProperty() != 0.5f || physicsBounce)
        {
            bool both = (float)shipFriction.GetProperty() != 0.5f && physicsBounce;
            PhysicMaterial mat;
            if (both)
            {
                float friction;
                if ((float)shipFriction.GetProperty() < 0.5f)
                {
                    friction = Mathf.Lerp(0f, 0.6f, (float)shipFriction.GetProperty() * 2f);
                }
                else
                {
                    friction = Mathf.Lerp(0.6f, 1f, ((float)shipFriction.GetProperty() - 0.5f) * 2f);
                }

                mat = LoadAsset<PhysicMaterial>("Assets/ShipEnhancements/FrictionlessBouncyShip.physicMaterial");
                mat.dynamicFriction = friction;
                mat.staticFriction = friction;
                mat.bounciness = (float)shipBounciness.GetProperty();
            }
            else if (physicsBounce)
            {
                mat = LoadAsset<PhysicMaterial>("Assets/ShipEnhancements/BouncyShip.physicMaterial");
                mat.bounciness = (float)shipBounciness.GetProperty();
            }
            else
            {
                float friction;
                if ((float)shipFriction.GetProperty() < 0.5f)
                {
                    friction = Mathf.Lerp(0f, 0.6f, (float)shipFriction.GetProperty() * 2f);
                }
                else
                {
                    friction = Mathf.Lerp(0.6f, 1f, ((float)shipFriction.GetProperty() - 0.5f) * 2f);
                }

                mat = LoadAsset<PhysicMaterial>("Assets/ShipEnhancements/FrictionlessShip.physicMaterial");
                mat.dynamicFriction = friction;
                mat.staticFriction = friction;
            }

            foreach (Collider collider in SELocator.GetShipTransform().GetComponentsInChildren<Collider>(true))
            {
                collider.material = mat;
            }
        }
        if ((bool)addPortableTractorBeam.GetProperty())
        {
            GameObject tractorSocket = LoadPrefab("Assets/ShipEnhancements/PortableTractorBeamSocket.prefab");
            GameObject tractorSocketObj = CreateObject(tractorSocket, SELocator.GetShipTransform().Find("Module_Cabin"));
        }
        if ((bool)addExpeditionFlag.GetProperty())
        {
            SELocator.GetShipTransform().GetComponentInChildren<Minimap>().gameObject.AddComponent<MinimapFlagController>();
            SELocator.GetPlayerBody().GetComponentInChildren<Minimap>().gameObject.AddComponent<MinimapFlagController>();

            GameObject flagSocket = LoadPrefab("Assets/ShipEnhancements/ExpeditionFlagSocket.prefab");
            GameObject flagSocketObj = CreateObject(flagSocket, SELocator.GetShipTransform().Find("Module_Cabin"));
        }
        if ((bool)addFuelCanister.GetProperty())
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
            GameObject tankSocketObj = CreateObject(tankSocket, SELocator.GetShipTransform().Find("Module_Cabin"));
            //tankSocketObj.GetComponent<FuelTankItemSocket>().PlaceIntoSocket(tankObj.GetComponent<FuelTankItem>());
        }
        if ((bool)singleUseTractorBeam.GetProperty())
        {
            SELocator.GetShipTransform().GetComponentInChildren<ShipTractorBeamSwitch>()._functional = false;
        }
        if ((string)shipWarpCoreType.GetProperty() == "Enabled")
        {
            GameObject core = LoadPrefab("Assets/ShipEnhancements/ShipWarpCore.prefab");
            core.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = SELocator.GetShipBody().gameObject;
            GameObject coreObj = CreateObject(core, SELocator.GetShipTransform().Find("Module_Cockpit"));

            if (NHAPI == null && GameObject.Find("TimberHearth_Body"))
            {
                GameObject receiver = LoadPrefab("Assets/ShipEnhancements/ShipWarpReceiver.prefab");
                receiver.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = SELocator.GetShipBody().gameObject;
                GameObject receiverObj = CreateObject(receiver, GameObject.Find("TimberHearth_Body").transform);
                coreObj.GetComponent<ShipWarpCoreController>().SetReceiver(receiverObj.GetComponent<ShipWarpCoreReceiver>());
            }
            else
            {
                WaitForCustomSpawnLoaded();
            }
        }
        if ((float)repairTimeMultiplier.GetProperty() != 1f
            && (float)repairTimeMultiplier.GetProperty() != 0f)
        {
            foreach (ShipComponent component in SELocator.GetShipTransform().GetComponentsInChildren<ShipComponent>())
            {
                component._repairTime *= (float)repairTimeMultiplier.GetProperty();
            }
            foreach (ShipHull hull in SELocator.GetShipTransform().GetComponentsInChildren<ShipHull>())
            {
                hull._repairTime *= (float)repairTimeMultiplier.GetProperty();
            }
        }
        if ((bool)addShipClock.GetProperty())
        {
            GameObject clock = LoadPrefab("Assets/ShipEnhancements/ShipClock.prefab");
            CreateObject(clock, SELocator.GetShipTransform().Find("Module_Cockpit"));
        }
        if ((bool)enableRepairConfirmation.GetProperty() || (bool)enableFragileShip.GetProperty())
        {
            GameObject audio = LoadPrefab("Assets/ShipEnhancements/SystemOnlineAudio.prefab");
            OWAudioSource source = CreateObject(audio, SELocator.GetShipTransform().Find("Audio_Ship")).GetComponent<OWAudioSource>();
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
        if ((bool)enableRemovableGravityCrystal.GetProperty())
        {
            Transform crystalParent = SELocator.GetShipTransform().Find("Module_Engine/Geo_Engine/Engine_Tech_Interior");
            GameObject obj1 = crystalParent.Find("Props_NOM_GravityCrystal").gameObject;
            GameObject obj2 = crystalParent.Find("Props_NOM_GravityCrystal_Base").gameObject;

            /*GameObject crystal = LoadPrefab("Assets/ShipEnhancements/GravityCrystalItem.prefab");
            AssetBundleUtilities.ReplaceShaders(crystal);
            ShipGravityCrystalItem item = Instantiate(crystal).GetComponent<ShipGravityCrystalItem>();*/

            GameObject crystalSocket = LoadPrefab("Assets/ShipEnhancements/GravityCrystalSocket.prefab");
            ShipGravityCrystalSocket socket = CreateObject(crystalSocket, SELocator.GetShipTransform().Find("Module_Engine")).GetComponent<ShipGravityCrystalSocket>();
            socket.AddComponentMeshes([obj1, obj2]);
            //socket.PlaceIntoSocket(item);
        }
        if ((bool)extraNoise.GetProperty())
        {
            GlobalMessenger.AddListener("StartShipIgnition", OnStartShipIgnition);
            GlobalMessenger.AddListener("CancelShipIgnition", OnStopShipIgnition);
            GlobalMessenger.AddListener("CompleteShipIgnition", OnStopShipIgnition);
        }
        if ((bool)addErnesto.GetProperty())
        {
            GameObject ernesto = LoadPrefab("Assets/ShipEnhancements/Ernesto.prefab");
            GameObject ernestoObj = CreateObject(ernesto, SELocator.GetShipBody().transform.Find("Module_Cockpit"));
            var font = (Font)Resources.Load(@"fonts\english - latin\HVD Fonts - BrandonGrotesque-Bold_Dynamic");
            if (font != null)
            {
                ernestoObj.GetComponentInChildren<UnityEngine.UI.Text>().font = font;
            }
            DialogueBuilder.FixCustomDialogue(ernestoObj, "ConversationZone");
            DialogueBuilder.FixCustomDialogue(ernestoObj, "ConversationZone (1)");

            var bh = GameObject.Find("BrittleHollow_Body");
            if (bh != null)
            {
                var parent = bh.transform.Find("Sector_BH/Sector_OldSettlement/Fragment OldSettlement 5");
                var additions = LoadPrefab("Assets/ShipEnhancements/OldSettlementAdditions.prefab");
                CreateObject(additions, parent);
            }
        }
        if ((int)(float)repairLimit.GetProperty() >= 0)
        {
            ShipRepairLimitController.SetRepairLimit((int)(float)repairLimit.GetProperty());
        }
        if ((bool)addShipCurtain.GetProperty())
        {
            MeshFilter rend;
            if ((bool)addFuelCanister.GetProperty())
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
            CreateObject(curtainObj, SELocator.GetShipTransform().Find("Module_Cabin/Geo_Cabin/Cabin_Geometry/Cabin_Interior"));
        }
        if ((string)repairWrenchType.GetProperty() != "Disabled")
        {
            GameObject wrenchSocketObj = LoadPrefab("Assets/ShipEnhancements/RepairWrenchSocket.prefab");
            RepairWrenchSocket wrenchSocket = CreateObject(wrenchSocketObj,
                SELocator.GetShipTransform().Find("Module_Cockpit")).GetComponent<RepairWrenchSocket>();
        }
        if ((bool)addRadio.GetProperty())
        {
            GameObject radioSocketObj = LoadPrefab("Assets/ShipEnhancements/RadioItemSocket.prefab");
            RadioItemSocket radioSocket = CreateObject(radioSocketObj,
                SELocator.GetShipTransform().Find("Module_Cockpit")).GetComponent<RadioItemSocket>();

            GameObject codeNotesObj = LoadPrefab("Assets/ShipEnhancements/CodeNotes.prefab");
            CreateObject(codeNotesObj, SELocator.GetShipTransform().Find("Module_Cockpit"));

            AddRadioCodeZones();
        }
        if ((bool)disableFluidPrevention.GetProperty())
        {
            GameObject[] stencils = SELocator.GetShipDamageController()._stencils;
            for (int j = 0; j < stencils.Length; j++)
            {
                stencils[j].SetActive(false);
            }

            Transform atmoVolume = SELocator.GetShipTransform().Find("Volumes/ShipAtmosphereVolume");
            atmoVolume.GetComponent<FluidVolume>().SetPriority(0);
        }
        if ((bool)disableRotationSpeedLimit.GetProperty())
        {
            SELocator.GetPlayerBody().GetComponentInChildren<PlayerCameraEffectController>().gameObject.AddComponent<PlayerTorpidityEffect>();
        }
        if ((float)waterDamage.GetProperty() > 0f
            || (float)sandDamage.GetProperty() > 0f
            || (float)cycloneChaos.GetProperty() > 0.7f)
        {
            GameObject fluidDamage = LoadPrefab("Assets/ShipEnhancements/ShipFluidDamageController.prefab");
            CreateObject(fluidDamage, SELocator.GetShipTransform());
        }
        if ((bool)disableMinimapMarkers.GetProperty())
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
        if ((bool)enableAutoAlign.GetProperty())
        {
            SELocator.GetShipBody().gameObject.AddComponent<ShipAutoAlign>();
        }
        if ((string)shipHornType.GetProperty() != "None")
        {
            string type = (string)shipHornType.GetProperty();
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

            CreateObject(LoadPrefab("Assets/ShipEnhancements/ShipHorn.prefab"), SELocator.GetShipTransform().Find("Audio_Ship"));
        }
        if ((bool)disableHatch.GetProperty())
        {
            SELocator.GetShipTransform().Find("Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Exterior/HatchPivot").gameObject.SetActive(false);
            SELocator.GetShipTransform().Find("Module_Cabin/Geo_Cabin/Cabin_Colliders_Back/Shared/Hatch_Collision_Open").gameObject.SetActive(false);
            HatchController hatch = SELocator.GetShipTransform().GetComponentInChildren<HatchController>();
            hatch._interactVolume.GetComponent<Shape>().enabled = false;
        }
        if ((string)disableThrusters.GetProperty() != "None")
        {
            ThrustAndAttitudeIndicator indicator = SELocator.GetShipTransform().GetComponentInChildren<ThrustAndAttitudeIndicator>();
            List<Light> lightsToDisable = [];
            switch ((string)disableThrusters.GetProperty())
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
        if ((bool)scoutPhotoMode.GetProperty())
        {
            var bracketUI = LoadPrefab("Assets/ShipEnhancements/ProbeBracketsDisplay.prefab");
            var launcherUI = SELocator.GetShipTransform().GetComponentInChildren<ProbeLauncherUI>();
            var imageObj = CreateObject(bracketUI, launcherUI.transform.parent).GetComponentInChildren<Image>();
            imageObj.enabled = false;
            launcherUI._bracketImage = imageObj;
        }
        if ((bool)addResourcePump.GetProperty())
        {
            GameObject pumpSocketObj = LoadPrefab("Assets/ShipEnhancements/ResourcePumpSocket.prefab");
            CreateObject(pumpSocketObj, SELocator.GetShipTransform().Find("Module_Cabin"));
        }
        if ((bool)addWaterTank.GetProperty())
        {
            SELocator.GetShipBody().gameObject.AddComponent<ShipWaterResource>();
            GameObject meterObj = LoadPrefab("Assets/ShipEnhancements/ShipWaterMeter.prefab");
            CreateObject(meterObj, SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit"));
        }
        if ((bool)addWaterCooling.GetProperty())
        {
            GameObject leverObj = LoadPrefab("Assets/ShipEnhancements/WaterCoolingLever.prefab");
            CreateObject(leverObj, SELocator.GetShipTransform().Find("Module_Cabin/Geo_Cabin"));
        }
        if ((bool)enableReactorOverload.GetProperty())
        {
            if ((bool)enableReactorOverload.GetProperty())
            {
                GameObject overloadObj = LoadPrefab("Assets/ShipEnhancements/ReactorOverloadInteract.prefab");
                CreateObject(overloadObj, SELocator.GetShipTransform().Find("Module_Engine"));
            }
        }

        if (AchievementsAPI != null)
        {
            GameObject th = GameObject.Find("TimberHearth_Body");
            if (th != null)
            {
                GameObject satelliteObj = LoadPrefab("Assets/ShipEnhancements/SatelliteAchievement_Volume.prefab");
                Transform parent = th.transform.Find("Sector_TH/Sector_Village/Sector_Observatory");
                CreateObject(satelliteObj, parent);
            }
        }
        if (ExperimentalSettings?.UltraQuantumShip ?? false)
        {
            CreateObject(LoadPrefab("Assets/ShipEnhancements/QuantumShipController.prefab"), SELocator.GetShipTransform());
        }
        else if ((bool)enableQuantumShip.GetProperty())
        {
            var qPrefab = LoadPrefab("Assets/ShipEnhancements/QuantumShipController.prefab");
            Instantiate(qPrefab.transform.GetChild(0), SELocator.GetShipTransform());
            var qShip = SELocator.GetShipBody().gameObject.AddComponent<SocketedQuantumShip>();
            qShip._maxSnapshotLockRange = 5000f;
            qShip._collapseOnStart = false;
            qShip._ignoreRetryQueue = true;
            qShip._alignWithGravity = false;
            qShip._alignWithSocket = true;
            qShip._randomYRotation = true;
            qShip._localOffset = new Vector3(0, 4, 0);
        }
        if ((bool)enableGasLeak.GetProperty())
        {
            RandomShipEffect();

            SELocator.GetShipBody().GetComponentInChildren<ShipAudioController>()
                .gameObject.AddComponent<SmokeDetectorChirp>();
        }
        if ((float)shipForceMultiplier.GetProperty() != 1f)
        {
            SELocator.GetShipDetector().GetComponent<AlignmentForceDetector>()._fieldMultiplier =
                (float)shipForceMultiplier.GetProperty();
        }
        if ((float)tractorBeamLengthMultiplier.GetProperty() != 1f)
        {
            var beamSwitch = SELocator.GetShipTransform().GetComponentInChildren<ShipTractorBeamSwitch>();
            var fluid = beamSwitch.GetComponentInChildren<TractorBeamFluid>();

            if ((float)tractorBeamLengthMultiplier.GetProperty() != 0f)
            {
                float oldHeight = fluid._height;
                fluid._height *= Mathf.Abs((float)tractorBeamLengthMultiplier.GetProperty());
                float diff = fluid._height - oldHeight;
            
                if ((float)tractorBeamLengthMultiplier.GetProperty() < 0f)
                {
                    fluid._reversed = true;
                
                    var col = beamSwitch.GetComponent<CapsuleCollider>();
                    col.height += diff;
                    col.center += new Vector3(0f, diff / 2, 0f);
                    col.gameObject.AddComponent<OWTriggerVolume>();
                }
            
                fluid.OnValidate();
            }

            if ((float)tractorBeamLengthMultiplier.GetProperty() <= 0f)
            {
                beamSwitch.DeactivateTractorBeam();
            }
        }

        SetDamageColors();

        engineOn = !(bool)addEngineSwitch.GetProperty();

        if (InMultiplayer && !QSBAPI.GetIsHost() && QSBCompat.NeverInitialized())
        {
            foreach (uint id in PlayerIDs)
            {
                QSBCompat.SendInitializedShip(id);
            }
        }

        SELocator.LateInitialize();
        ShipNotifications.Initialize();

        ModHelper.Events.Unity.RunWhen(() => Locator._shipBody != null, () =>
        {
            shipLoaded = true;
            if ((bool)disableGravityCrystal.GetProperty())
            {
                DisableGravityCrystal();
            }
            if ((bool)enableJetpackRefuelDrain.GetProperty())
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
            if ((bool)disableEjectButton.GetProperty())
            {
                SELocator.GetShipBody().GetComponentInChildren<ShipEjectionSystem>().GetComponent<InteractReceiver>().DisableInteraction();
                GameObject ejectButtonTape = LoadPrefab("Assets/ShipEnhancements/EjectButtonTape.prefab");
                CreateObject(ejectButtonTape, SELocator.GetShipBody().transform.Find("Module_Cockpit/Geo_Cockpit"));
            }
            if ((bool)disableShipSuit.GetProperty())
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
            if ((float)randomHullDamage.GetProperty() > 0f && (!InMultiplayer || QSBAPI.GetIsHost()))
            {
                float lerp = (float)randomHullDamage.GetProperty();
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
            if ((float)randomComponentDamage.GetProperty() > 0f && (!InMultiplayer || QSBAPI.GetIsHost()))
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

                float lerp = (float)randomComponentDamage.GetProperty();
                int iterations = (int)(lerp * components.Count + 0.1f);
                for (int i = 0; i < iterations; i++)
                {
                    int index = UnityEngine.Random.Range(0, components.Count);
                    components[index].SetDamaged(true);
                    components.RemoveAt(index);
                    anyPartDamaged = true;
                }
            }
            if ((bool)preventSystemFailure.GetProperty())
            {
                GameObject entrywayTriggersObj = LoadPrefab("Assets/ShipEnhancements/BreachEntryTriggers.prefab");
                OWTriggerVolume entrywayVol = CreateObject(entrywayTriggersObj, SELocator.GetShipTransform().Find("Volumes")).GetComponent<OWTriggerVolume>();

                PlayerSpawner spawner = GameObject.FindGameObjectWithTag("Player").GetRequiredComponent<PlayerSpawner>();
                SpawnPoint shipSpawn = spawner.GetSpawnPoint(SpawnLocation.Ship);
                List<OWTriggerVolume> shipTriggers = [.. shipSpawn._triggerVolumes];
                shipTriggers.Add(entrywayVol);
                shipSpawn._triggerVolumes = [.. shipTriggers];
            }
            if ((bool)disableMinimapMarkers.GetProperty())
            {
                GameObject playerMarker = GameObject.Find("SecondaryGroup/HUD_Minimap/Minimap_Root/PlayerMarker/Arrow");
                playerMarker.SetActive(false);
                GameObject geMarker = GameObject.Find("SecondaryGroup/HUD_Minimap/Minimap_Root/AboveGroundMarker/Arrow");
                geMarker?.SetActive(false);
            }
            if ((!InMultiplayer || QSBAPI.GetIsHost()) && (float)shipDamageSpeedMultiplier.GetProperty() < 0f)
            {
                SELocator.GetShipDamageController().Explode();
            }

            ShipRepairLimitController.RefreshRepairPrompt();
            InitializeConditions();
        });
    }

    private void SetupExplosion(Transform effectsTransform, ExplosionController explosion)
    {
        float multiplier = (float)shipExplosionMultiplier.GetProperty();

        if (multiplier >= 100f)
        {
            GameObject supernova = LoadPrefab("Assets/ShipEnhancements/ExplosionSupernova.prefab");
            GameObject supernovaObj = CreateObject(supernova, SELocator.GetShipTransform().Find("Module_Engine"));
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
                    CreateObject(LoadPrefab("Assets/ShipEnhancements/ShipExplosionExpandAudio.prefab"),
                        audio.transform).name = "ShipExplosionExpandAudio";
                }
            }
        }

        if ((bool)moreExplosionDamage.GetProperty())
        {
            GameObject damage = LoadPrefab("Assets/ShipEnhancements/ExplosionDamage.prefab");
            GameObject damageObj = CreateObject(damage, explosion.transform);
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
        if (NHAPI == null)
        {
            GameObject sun = GameObject.Find("Sun_Body");
            if (sun != null)
            {
                GameObject sunTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Sun.prefab");
                CreateObject(sunTempZone, sun.transform.Find("Sector_SUN/Volumes_SUN"));
                GameObject supernovaTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Supernova.prefab");
                CreateObject(supernovaTempZone, sun.GetComponentInChildren<SupernovaEffectController>().transform);
            }
        }
    }

    private void AddTemperatureZones()
    {
        string zones = (string)temperatureZonesAmount.GetProperty();
        bool hot = zones == "All" || zones == "Hot";
        bool cold = zones == "All" || zones == "Cold";

        GameObject ct = GameObject.Find("CaveTwin_Body");
        if (ct != null)
        {
            GameObject ctTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_CaveTwinHot.prefab");
            CreateObject(ctTempZone1, ct.transform.Find("Sector_CaveTwin"));
        }
        GameObject tt = GameObject.Find("TowerTwin_Body");
        if (tt != null)
        {
            GameObject ttTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_TowerTwinHot.prefab");
            CreateObject(ttTempZone1, tt.transform.Find("Sector_TowerTwin"));
        }

        if (hot)
        {
            SpawnSunTemperatureZones();

            GameObject vm = GameObject.Find("VolcanicMoon_Body");
            if (vm != null)
            {
                GameObject vmTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_VolcanicMoon.prefab");
                CreateObject(vmTempZone, vm.transform.Find("Sector_VM"));
            }

            GameObject gd = GameObject.Find("GiantsDeep_Body");
            if (gd != null)
            {
                GameObject gdTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_GiantsDeepCore.prefab");
                CreateObject(gdTempZone2, gd.transform.Find("Sector_GD/Sector_GDInterior"));
            }

            GameObject th = GameObject.Find("TimberHearth_Body");
            if (th != null)
            {
                GameObject thTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_TimberHearthCore.prefab");
                CreateObject(thTempZone2, th.transform.Find("Sector_TH"));
            }

            Campfire[] campfires = FindObjectsOfType<Campfire>();
            if (campfires.Length > 0)
            {
                GameObject campfireTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Campfire.prefab");
                foreach (Campfire fire in campfires)
                {
                    CreateObject(campfireTempZone, fire.transform.parent);
                }
            }
        }

        if (cold)
        {
            GameObject db = GameObject.Find("DarkBramble_Body");
            if (db != null)
            {
                GameObject dbTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_DarkBramble.prefab");
                CreateObject(dbTempZone, db.transform.Find("Sector_DB"));
            }

            GameObject escapePodDimension = GameObject.Find("DB_EscapePodDimension_Body");
            if (escapePodDimension != null)
            {
                GameObject podDimensionTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_EscapePodDimension.prefab");
                CreateObject(podDimensionTempZone, escapePodDimension.transform.Find("Sector_EscapePodDimension"));
            }

            GameObject comet = GameObject.Find("Comet_Body");
            if (comet != null)
            {
                GameObject cometTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_InterloperAtmosphere.prefab");
                CreateObject(cometTempZone1, comet.transform.Find("Sector_CO"));
                GameObject cometTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_InterloperDarkSide.prefab");
                CreateObject(cometTempZone2, comet.transform.Find("Sector_CO"));
            }

            GameObject gd = GameObject.Find("GiantsDeep_Body");
            if (gd != null)
            {
                GameObject gdTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_GiantsDeepOcean.prefab");
                CreateObject(gdTempZone1, gd.transform.Find("Sector_GD/Sector_GDInterior"));
            }

            GameObject brambleIsland = GameObject.Find("BrambleIsland_Body");
            if (brambleIsland != null)
            {
                GameObject brambleIslandTempZones = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_BrambleIsland.prefab");
                CreateObject(brambleIslandTempZones, brambleIsland.transform.Find("Sector_BrambleIsland"));
            }

            GameObject bh = GameObject.Find("BrittleHollow_Body");
            if (bh != null)
            {
                GameObject bhTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_BrittleHollowNorth.prefab");
                CreateObject(bhTempZone1, bh.transform.Find("Sector_BH"));
                GameObject bhTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_BrittleHollowSouth.prefab");
                CreateObject(bhTempZone2, bh.transform.Find("Sector_BH"));
            }

            GameObject th = GameObject.Find("TimberHearth_Body");
            if (th != null)
            {
                GameObject thTempZone1 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_TimberHearthGeyser.prefab");
                CreateObject(thTempZone1, th.transform.Find("Sector_TH"));
                GameObject thTempZone3 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_TimberHearthSurface.prefab");
                CreateObject(thTempZone3, th.transform.Find("Sector_TH"));

                if (ModCompatibility.ChristmasStory)
                {
                    GameObject thTempZone4 = LoadPrefab("Assets/ShipEnhancements/TZCustom/ChristmasStory_Village.prefab");
                    CreateObject(thTempZone4, th.transform.Find("Sector_TH/Sector_Village"));
                }
            }

            GameObject moon = GameObject.Find("Moon_Body");
            if (moon != null)
            {
                GameObject moonTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_AttlerockCrater.prefab");
                CreateObject(moonTempZone, moon.transform.Find("Sector_THM"));
            }

            if (ct != null)
            {
                GameObject ctTempZone2 = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_CaveTwinCold.prefab");
                CreateObject(ctTempZone2, ct.transform.Find("Sector_CaveTwin"));
            }

            GameObject whs = GameObject.Find("WhiteholeStationSuperstructure_Body");
            if (whs != null)
            {
                GameObject whsTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_WhiteHoleStation.prefab");
                CreateObject(whsTempZone, whs.transform);
            }

            GameObject qm = GameObject.Find("QuantumMoon_Body");
            if (qm != null)
            {
                Transform root = qm.transform.Find("Sector_QuantumMoon");
                GameObject zone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_QuantumMoon_HourglassTwins.prefab");
                CreateObject(zone, root.Find("State_HT"));
                zone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_QuantumMoon_DarkBramble.prefab");
                CreateObject(zone, root.Find("State_DB"));
                zone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_QuantumMoon_BrittleHollow.prefab");
                CreateObject(zone, root.Find("State_BH"));
            }
        }
    }

    private void AddRadioCodeZones()
    {
        GameObject et = GameObject.Find("CaveTwin_Body");
        if (et != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_NomaiMeditation.prefab");
            CreateObject(zone, et.transform.Find("Sector_CaveTwin"));
        }

        GameObject th = GameObject.Find("TimberHearth_Body");
        if (th != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_HearthsShadow.prefab");
            CreateObject(zone, th.transform.Find("Sector_TH"));
        }

        GameObject ss = GameObject.Find("SunStation_Body");
        if (ss != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_NoTimeForCaution.prefab");
            CreateObject(zone, ss.transform.Find("Sector_SunStation"));
        }

        GameObject co = GameObject.Find("Comet_Body");
        if (co != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_RiversEndTimes.prefab");
            CreateObject(zone, co.transform.Find("Sector_CO"));
        }

        GameObject qm = GameObject.Find("QuantumMoon_Body");
        if (qm != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_LastDreamOfHome.prefab");
            CreateObject(zone, qm.transform.Find("Sector_QuantumMoon/State_EYE"));
        }

        GameObject vessel = GameObject.Find("DB_VesselDimension_Body");
        if (vessel != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_OlderThanTheUniverse.prefab");
            CreateObject(zone, vessel.transform.Find("Sector_VesselDimension"));
        }

        GameObject rw = GameObject.Find("RingWorld_Body");
        if (rw != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_ElegyForTheRings.prefab");
            CreateObject(zone, rw.transform.Find("Sector_RingInterior/Sector_Zone1/Sector_DreamFireHouse_Zone1"));
            CreateObject(zone, rw.transform.Find("Sector_RingInterior/Sector_Zone2/Sector_DreamFireLighthouse_Zone2_AnimRoot/Volumes_DreamFireLighthouse_Zone2"));
            CreateObject(zone, rw.transform.Find("Sector_RingInterior/Sector_Zone3/Sector_HiddenGorge/Sector_DreamFireHouse_Zone3"));
        }

        GameObject sun = GameObject.Find("Sun_Body");
        if (sun != null)
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_TheSpiritOfWater.prefab");
            CreateObject(zone, sun.transform.Find("Sector_SUN/Volumes_SUN/SupernovaVolume"));
        }

        if ((bool)addErnesto.GetProperty())
        {
            GameObject bh = GameObject.Find("BrittleHollow_Body");
            if (bh != null)
            {
                GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_0187.prefab");
                CreateObject(zone, bh.transform.Find("Sector_BH/Sector_OldSettlement/Fragment OldSettlement 5/Core_OldSettlement 5"));
            }
        }
    }

    private void OnNHBodyLoaded(string name)
    {
        if ((bool)addRadio.GetProperty() && name == "Egg Star")
        {
            GameObject zone = LoadPrefab("Assets/ShipEnhancements/RadioCodeZone_Doom.prefab");
            CreateObject(zone, NHAPI.GetPlanet(name).transform);
        }
        if ((bool)enableShipTemperature.GetProperty())
        {
            GameObject zone = null;

            if (ModCompatibility.Evacuation)
            {
                if (name == "Twilight Frost")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/Evacuation_TwilightFrost.prefab");
                }
                else if (name == "Smoldering Gulch")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/Evacuation_SmolderingGulch.prefab");
                }
            }
            if (ModCompatibility.EchoHike)
            {
                if (name == "Echo Hike")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/EchoHike_FrozenSolace.prefab");
                }
            }
            if (ModCompatibility.AxiomsRefuge)
            {
                if (name == "Axiom")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/AxiomsRefuge_Axiom.prefab");
                }
                else if (name == "Aicale")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/AxiomsRefuge_Aicale.prefab");
                }
            }
            if (ModCompatibility.MisfiredJump)
            {
                if (name == "Scalding Abyss")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/MisfiredJump_ScaldingAbyss.prefab");
                }
            }
            if (ModCompatibility.TheStrangerTheyAre)
            {
                if (name == "Ringed Giant")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/TheStrangerTheyAre_RingedGiant.prefab");
                }
                else if (name == "Burning Bombardier")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/TheStrangerTheyAre_BurningBombardier.prefab");
                }
                else if (name == "Sizzling Sands")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/TheStrangerTheyAre_SizzlingSands.prefab");
                }
                else if (name == "Distant Enigma")
                {
                    Transform root = NHAPI.GetPlanet(name).transform;
                    if (root.Find("Sector-3") && !root.Find("Sector-3/TheStrangerTheyAre_DistantEnigma_ThinIce"))
                    {
                        zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/TheStrangerTheyAre_DistantEnigma_ThinIce.prefab");
                        CreateObject(zone, root.Find("Sector-3"));
                    }
                    else if (root.Find("Sector-2") && !root.Find("Sector-2/TheStrangerTheyAre_DistantEnigma_Water"))
                    {
                        zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/TheStrangerTheyAre_DistantEnigma_Water.prefab");
                        CreateObject(zone, root.Find("Sector-2"));
                    }
                    else if (!root.Find("Sector/TheStrangerTheyAre_DistantEnigma_ThickIce"))
                    {
                        zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/TheStrangerTheyAre_DistantEnigma_ThickIce.prefab");
                        CreateObject(zone, root.Find("Sector"));
                    }
                    return;
                }
                else if (name == "Velvet Vortex")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/TheStrangerTheyAre_VelvetVortex.prefab");
                }
            }
            if (ModCompatibility.Heliostudy)
            {
                if (name == "Walker_Jam5_Planet4")
                {
                    Transform root = NHAPI.GetPlanet(name).transform;
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/Heliostudy_GlacialAbyss!.prefab");
                    CreateObject(zone, root);
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/Heliostudy_GlacialAbyss!_Core.prefab");
                    CreateObject(zone, root);
                    return;
                }
                else if (name == "Walker_Jam5_Planet2")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/Heliostudy_TheBigOne.prefab");
                }
                else if (name == "Walker_Jam5_Planet3")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/Heliostudy_Daucus.prefab");
                }
                else if (name == "Walker_Jam5_Planet1")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/Heliostudy_ShatteredGeode.prefab");
                }
            }
            if (ModCompatibility.OnARail)
            {
                if (name == "Frost Car")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/OnARail_FrostCar.prefab");
                }
            }
            if (ModCompatibility.UnnamedMystery)
            {
                if (name == "Electrum")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/UnnamedMystery_Electrum.prefab");
                }
                else if (name == "Zephyria")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/UnnamedMystery_Zephyria.prefab");
                }
            }
            if (ModCompatibility.FretsQuest2)
            {
                if (name == "Frozen Homeworld")
                {
                    zone = LoadPrefab("Assets/ShipEnhancements/TZCustom/FretsQuest2_FrozenHomeworld.prefab");
                }
            }

            if (zone != null)
            {
                CreateObject(zone, NHAPI.GetPlanet(name).transform);
            }
        }
    }

    private void OnNHStarSystemLoaded(string name)
    {
        if ((bool)enableShipTemperature.GetProperty()
            && (!ModCompatibility.Evacuation || name != "2walker2.OogaBooga"))
        {
            GameObject sunTempZone = LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Sun.prefab");

            SunController[] suns = FindObjectsOfType<SunController>();
            foreach (SunController sun in suns)
            {
                ShipEnhancements.WriteDebugMessage("sun found: " + sun.gameObject.name);
                if (sun.GetComponentInChildren<HeatHazardVolume>() && !sun.GetComponentInChildren<TemperatureZone>())
                {
                    ShipEnhancements.WriteDebugMessage("sun can support temp zone");
                    TemperatureZone zone = CreateObject(sunTempZone, sun.GetComponentInChildren<Sector>().transform).GetComponent<TemperatureZone>();
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
            receiver.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = SELocator.GetShipBody().gameObject;
            ShipWarpCoreReceiver receiverObj = CreateObject(receiver, spawn.transform).GetComponent<ShipWarpCoreReceiver>();
            receiverObj.transform.localPosition = spawn.offset;
            receiverObj.transform.localRotation = Quaternion.identity;
            receiverObj.OnCustomSpawnPoint();

            ShipWarpCoreController core = SELocator.GetShipTransform().GetComponentInChildren<ShipWarpCoreController>();
            core.SetReceiver(receiverObj);
        }
        else
        {
            GameObject receiver = LoadPrefab("Assets/ShipEnhancements/ShipWarpReceiver.prefab");
            receiver.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = SELocator.GetShipBody().gameObject;
            GameObject receiverObj = CreateObject(receiver, GameObject.Find("TimberHearth_Body").transform);

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

        disableShipHeadlights = true;
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

    private void ApplyHullDecoration()
    {
        string interiorHull = (string)interiorHullColor1.GetProperty();
        string exteriorHull = (string)exteriorHullColor1.GetProperty();
        string interiorWood = (string)interiorWoodColor1.GetProperty();
        string exteriorWood = (string)exteriorWoodColor1.GetProperty();
        
        WriteDebugMessage("interior hull setting: " + interiorHull);
        WriteDebugMessage("exterior hull setting: " + exteriorHull);
        
        bool interiorHullTex = (string)interiorHullTexture.GetProperty() != "None";
        bool exteriorHullTex = (string)exteriorHullTexture.GetProperty() != "None";
        bool interiorWoodTex = (string)interiorWoodTexture.GetProperty() != "None";
        bool exteriorWoodTex = (string)exteriorWoodTexture.GetProperty() != "None";

        bool blendInteriorHull = ((bool)enableColorBlending.GetProperty()
            && int.Parse((string)interiorHullColorOptions.GetProperty()) > 1)
            || interiorHull == "Rainbow";
        bool blendExteriorHull = ((bool)enableColorBlending.GetProperty()
                && int.Parse((string)exteriorHullColorOptions.GetProperty()) > 1)
            || exteriorHull == "Rainbow";
        bool blendInteriorWood = ((bool)enableColorBlending.GetProperty()
                && int.Parse((string)interiorWoodColorOptions.GetProperty()) > 1)
            || interiorWood == "Rainbow";
        bool blendExteriorWood = ((bool)enableColorBlending.GetProperty()
                && int.Parse((string)exteriorWoodColorOptions.GetProperty()) > 1)
            || exteriorWood == "Rainbow";
        
        var customizeInteriorHull = blendInteriorHull || interiorHull != "Default" || interiorHullTex;
        var customizeExteriorHull = blendExteriorHull || exteriorHull != "Default" || exteriorHullTex;
        var customizeInteriorWood = blendInteriorWood || interiorWood != "Default" || interiorWoodTex;
        var customizeExteriorWood = blendExteriorWood || exteriorWood != "Default" || exteriorWoodTex;

        Material[] interiorMats =
        [
            _defaultInteriorHullMat,
            _defaultSEInteriorMat1,
            _defaultSEInteriorMat2
        ];

        foreach (var blender in _textureBlenders.Values) blender.Dispose();
        _textureBlenders.Clear();

        foreach (var mat in interiorMats)
        {
            AddBlender(mat, false);
        }
        AddBlender(_defaultExteriorHullMat, false);
        AddBlender(_defaultInteriorWoodMat, true);
        AddBlender(_defaultExteriorWoodMat, true);
        
        // DumpMats("D:/misc/files/mats_02.json");

        foreach (MeshRenderer rend in SELocator.GetShipTransform().GetComponentsInChildren<MeshRenderer>())
        {
            rend.sharedMaterials = rend.sharedMaterials.Select(mat =>
            {
                if (mat == null) return mat;

                var isCustom = new[]
                {
                    interiorMats.Contains(mat) && customizeInteriorHull,
                    mat == _defaultExteriorHullMat && customizeExteriorHull,
                    mat == _defaultInteriorWoodMat && customizeInteriorWood,
                    mat == _defaultExteriorWoodMat && customizeExteriorWood
                }.Any(b => b);
                if (isCustom) return _textureBlenders[mat].BlendedMaterial;

                return mat;
            }).ToArray();
        }
        
        // DumpMats("D:/misc/files/mats_03.json");

        var intHullTex = LoadCustomTexture(interiorHullTex, interiorHullTexture, true);
        var extHullTex = LoadCustomTexture(exteriorHullTex, exteriorHullTexture, true);
        var intWoodTex = LoadCustomTexture(interiorWoodTex, interiorWoodTexture, false);
        var extWoodTex = LoadCustomTexture(exteriorWoodTex, exteriorWoodTexture, false);
        
        var intHullBlendController = SELocator.GetShipBody().gameObject.GetAddComponent<InteriorHullBlendController>();
        var extHullBlendController = SELocator.GetShipBody().gameObject.GetAddComponent<ExteriorHullBlendController>();
        var intWoodBlendController = SELocator.GetShipBody().gameObject.GetAddComponent<InteriorWoodBlendController>();
        var extWoodBlendController = SELocator.GetShipBody().gameObject.GetAddComponent<ExteriorWoodBlendController>();
        
        foreach (var mat in interiorMats)
        {
            ConfigureBlender(
                mat,
                interiorHullTex,
                intHullTex,
                blendInteriorHull,
                intHullBlendController,
                interiorHull
            );
        }

        ConfigureBlender(
            _defaultExteriorHullMat,
            exteriorHullTex,
            extHullTex,
            blendExteriorHull,
            extHullBlendController,
            exteriorHull
        );
        
        ConfigureBlender(
            _defaultInteriorWoodMat,
            interiorWoodTex,
            intWoodTex,
            blendInteriorWood,
            intWoodBlendController,
            interiorWood
        );
        
        ConfigureBlender(
            _defaultExteriorWoodMat,
            exteriorWoodTex,
            extWoodTex,
            blendExteriorWood,
            extWoodBlendController,
            exteriorWood
        );
        
        foreach (var blender in _textureBlenders.Values)
        {
            // $"[Q6J] try to full update {blender.BlendedMaterial.name}".Log();
            blender.UpdateFullTexture();
        }
        
        // DumpMats("D:/misc/files/mats_04.json");
    }

    private void ConfigureBlender(
        Material material,
        bool textureCondition,
        ShipTextureInfo sourceTexture,
        bool blendCondition,
        ShipHullBlendController blendController,
        string themeName
    )
    {
        var blender = _textureBlenders[material];
        if (textureCondition)
            blender.SourceTexture = sourceTexture;
        else
            blender.SourceTexture = blender.BaseTexture;

        if (blendCondition)
            blendController.AddTextureBlender(blender);
        else if (themeName != "Default")
            blender.OverlayColor = ThemeManager.GetHullTheme(themeName).HullColor / 255f;
    }

    private void AddBlender(Material baseMaterial, bool isWood)
    {
        var woodZone = new Vector4(0, 0, 1, 1);
        var nonWoodZone = new Vector4(.5f, 0f, 1f, .5f);
        var exclusionZone = new Vector4(.9f, 0f, 1f, .2f);
        CustomMatManager.InitializeMaterial(baseMaterial);
        _textureBlenders[baseMaterial] = new ShipTextureBlender(
            textureBlendMat,
            baseMaterial,
            isWood ? woodZone : nonWoodZone,
            destExclusionZone: isWood ? null : exclusionZone
        );
    }

    private ShipTextureInfo LoadCustomTexture(bool condition, Settings textureSetting, bool hasGloss)
    {
        if (!condition) return null;

        return new ShipTextureInfo(
            ThemeManager.GetHullTexturePath((string)textureSetting.GetProperty()).path,
            hasGloss
        );
    }
    

    // private void SetHullTexture(ShipTextureBlender blender, HullTexturePath hullTexture)
    // {
    //     //UpdateHullMaterials(baseMat, ref customMat, false);
    //     
    //     Texture2D color = (Texture2D)LoadAsset(hullTexture.path + "_d.png");
    //     Texture2D normal = (Texture2D)LoadAsset(hullTexture.path + "_n.png");
    //     Texture2D smooth = (Texture2D)LoadAsset(hullTexture.path + "_s.png");
    //
    //     blender.SetTexture(color, normal, smooth, hullTexture.normalScale, hullTexture.smoothness);
    // }
    
    // private void SetWoodTexture(ShipTextureBlender blender, WoodTexturePath woodTexture)
    // {
    //     //UpdateHullMaterials(baseMat, ref customMat, false);
    //
    //     var texInfo = new ShipTextureInfo(woodTexture.path);
    //
    //     blender.SetTexture(color, normal, smooth, woodTexture.normalScale, woodTexture.smoothness);
    // }

    // private void UpdateHullMaterials(Material baseMat, ref Material customMat, bool reset)
    // {
    //     Material mat1 = reset ? customMat : baseMat;
    //     Material mat2 = reset ? baseMat : customMat;
    //     
    //     foreach (MeshRenderer rend in SELocator.GetShipTransform().GetComponentsInChildren<MeshRenderer>())
    //     {
    //         for (int i = 0; i < rend.sharedMaterials.Length; i++)
    //         {
    //             if (rend.sharedMaterials[i] == null) continue;
    //                 
    //             if (rend.sharedMaterials[i] == mat1)
    //             {
    //                 // use a list to change sharedMaterials because
    //                 // changing a single material doesn't work for some reason
    //                 List<Material> mats = new List<Material>();
    //                 mats.AddRange(rend.sharedMaterials);
    //                 mats[i] = mat2;
    //                 rend.sharedMaterials = mats.ToArray();
    //             }
    //         }
    //     }
    //
    //     if (reset)
    //     {
    //         customMat = new Material(baseMat);
    //     }
    // }

    private void SetGlassMaterial()
    {
        string tex = (string)shipGlassTexture.GetProperty();
        string[] paths =
        [
            "Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior/CockpitExterior_GoldGlass",
            "Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Exterior/CockpitExterior_Chassis",
            "Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Exterior/HatchPivot/Hatch_GoldGlass"
        ];

        if (tex == "None")
        {
            foreach (string child in paths)
            {
                MeshRenderer rend = SELocator.GetShipTransform().Find(child).GetComponent<MeshRenderer>();
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    if (rend.sharedMaterials[i] == null) continue;
                    
                    if (rend.sharedMaterials[i] == _customGlassMat)
                    {
                        List<Material> mats = new List<Material>();
                        mats.AddRange(rend.sharedMaterials);
                        mats[i] = _defaultGlassMat;
                        rend.sharedMaterials = mats.ToArray();
                    }
                }
            }
            
            _customGlassMat = new Material(_defaultGlassMat);
        }
        else
        {
            string path = ThemeManager.GetGlassMaterialPath((string)shipGlassTexture.GetProperty());
            Material newMat = LoadMaterial(path);
            _customGlassMat = new Material(newMat);
            
            foreach (string child in paths)
            {
                MeshRenderer rend = SELocator.GetShipTransform().Find(child).GetComponent<MeshRenderer>();
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    if (rend.sharedMaterials[i] == null) continue;
                    
                    if (rend.sharedMaterials[i] == _defaultGlassMat)
                    {
                        List<Material> mats = new List<Material>();
                        mats.AddRange(rend.sharedMaterials);
                        mats[i] = _customGlassMat;
                        rend.sharedMaterials = mats.ToArray();
                    }
                }
            }
        }
    }

    private void SetShipPlantDecoration()
    {
        string plantType = (string)shipPlantType.GetProperty();
        if (plantType == "Default") return;

        Transform parent = SELocator.GetShipTransform().Find("Module_Cockpit/Props_Cockpit");
        parent.Find("Props_HEA_ShipFoliage").gameObject.SetActive(false);
        
        if (plantType == "None") return;

        GameObject prefab = LoadPrefab(ThemeManager.GetPlantTypePath(plantType));
        CreateObject(prefab, parent);
    }

    private void SetStringLightDecoration()
    {
        string stringLights = (string)shipStringLights.GetProperty();
        if (stringLights == null) return;

        GameObject prefab = LoadPrefab(ThemeManager.GetStringLightPath(stringLights));
        Transform parent = CreateObject(prefab).transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            string path;
            if (child.name.Contains("Cabin"))
            {
                path = "Module_Cabin/Lights_Cabin";
            }
            else if (child.name.Contains("Supplies"))
            {
                path = "Module_Supplies/Lights_Supplies";
            }
            else if (child.name.Contains("Engine"))
            {
                path = "Module_Engine";
            }
            else
            {
                continue;
            }
            
            child.SetParent(SELocator.GetShipTransform().Find(path));
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
        }
        
        Destroy(parent.gameObject);
    }
    
    private void SetDamageColors()
    {
        string color = (string)indicatorColor1.GetProperty();
        bool indicatorBlend = ((bool)enableColorBlending.GetProperty()
            && int.Parse((string)indicatorColorOptions.GetProperty()) > 1)
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
        if ((bool)enableManualScoutRecall.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_MANUAL_RECALL_ENALBED", true);
        }
        if ((float)rustLevel.GetProperty() > 0f)
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_COCKPIT_RUST", true);
            if ((float)rustLevel.GetProperty() > 0.75f)
            {
                DialogueConditionManager.SharedInstance.SetConditionState("SE_MAX_COCKPIT_RUST", true);
            }
        }
        if ((bool)enableShipTemperature.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_TEMPERATURE_ENABLED", true);
        }
        if ((string)disableThrusters.GetProperty() == "Backward"
            || (string)disableThrusters.GetProperty() == "All Except Forward")
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_RETRO_ROCKETS_DISABLED", true);
        }
        if (SEMenuManager.CurrentPreset == SettingsPresets.PresetName.Random)
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_USING_RANDOM_PRESET", true);
        }
        if ((bool)enableThrustModulator.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_THRUST_MODULATOR_ENABLED", true);
        }
        if ((bool)enableEnhancedAutopilot.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_AUTOPILOT_CONTROLS_ENABLED", true);
        }
        if ((bool)addShipSignal.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_SHIP_SIGNAL_ENABLED", true);
        }
        if ((bool)addTether.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_TETHER_HOOKS_ENABLED", true);
        }
        if ((bool)addExpeditionFlag.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_EXPEDITION_FLAG_ENABLED", true);
        }
        if ((string)shipWarpCoreType.GetProperty() != "Disabled")
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_WARP_CORE_ENABLED", true);
        }
        if ((bool)addRadio.GetProperty())
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_RADIO_ENABLED", true);
        }
    }

    private void SetUpShipLogSplashScreen()
    {
        GameObject go = SELocator.GetShipBody().GetComponentInChildren<ShipLogSplashScreen>().gameObject;
        MeshRenderer rend = go.GetComponent<MeshRenderer>();

        Texture2D tex = null;
        
        List<string> files = [];
        files.AddRange(Directory.GetFiles(Path.Combine(ModHelper.Manifest.ModFolderPath, "ShipLogIcons"), 
            "*.jpg", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(Path.Combine(ModHelper.Manifest.ModFolderPath, "ShipLogIcons"),
            "*.png", SearchOption.AllDirectories));
        
        if (files.Count > 0)
        {
            byte[] fileData = File.ReadAllBytes(files[UnityEngine.Random.Range(0, files.Count)]);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }

        if (tex != null)
        {
            rend.sharedMaterial.SetTexture("_MainTex", tex);
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
        OWAudioSource toolRef = CreateObject(toolRefObj).GetComponent<OWAudioSource>();

        shipAudio._probeScreenSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
            toolRef.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        shipAudio._probeScreenSource.maxDistance = toolRef.maxDistance;

        shipAudio._signalscopeSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
            toolRef.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        shipAudio._signalscopeSource.maxDistance = toolRef.maxDistance;

        shipAudio._cockpitSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
            toolRef.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        shipAudio._cockpitSource.maxDistance = toolRef.maxDistance;

        Destroy(toolRef.gameObject);

        GameObject ejectAudioObj = LoadPrefab("Assets/ShipEnhancements/EjectAudio.prefab");
        CreateObject(ejectAudioObj, shipAudio.transform.Find("ShipInteriorAudio")).name = "EjectAudio";
    }

    private void RandomShipEffect(int lastNum = -1)
    {
        int numEffects = 6;
        int num;
        System.Random rand = new();
        if (lastNum > 0)
        {
            num = rand.Next(0, numEffects - 1);
            if (num >= lastNum)
            {
                num++;
            }
        }
        else
        {
            num = rand.Next(0, numEffects);
        }

        ShipEnhancements.WriteDebugMessage(num);

        if (num == 0)
        {
            Transform cockpitParent = SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Tech/Cockpit_Tech_Interior");
            cockpitParent.Find("OxygenPosterCanvas").gameObject.SetActive(false);
            cockpitParent.Find("EnjoyCanvas").gameObject.SetActive(false);

            Transform cabinParent = SELocator.GetShipTransform().Find("Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Interior");
            cabinParent.Find("Cabin_Poster_TextCanvases").gameObject.SetActive(false);

            Transform suppliesParent = SELocator.GetShipTransform().Find("Module_Supplies/Geo_Supplies/Supplies_Tech");
            suppliesParent.Find("Scout_Poster_TextCanvases").gameObject.SetActive(false);
        }
        else if (num == 1)
        {
            OWCamera cam = SELocator.GetShipTransform().GetComponentInChildren<LandingCamera>().owCamera;
            if (rand.NextDouble() < 0.5f)
            {
                cam.fieldOfView = 80f;
            }
            else
            {
                cam.fieldOfView = 120f;
            }
        }
        else if (num == 2)
        {
            Transform cockpitParent = SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Tech/Cockpit_Tech_Interior");
            MeshRenderer consoleRend = cockpitParent.Find("ConsoleScreen").GetComponent<MeshRenderer>();
            consoleRend.material.SetColor("_Color", new Color(0.35f, 0.27f, 0.52f));
            consoleRend.material.SetColor("_EmissionColor", new Color(0.45f, 0.39f, 0.8f));
        }
        else if (num == 3)
        {
            Minimap minimap = SELocator.GetShipTransform().GetComponentInChildren<Minimap>();
            minimap._globeMeshTransform.Find("Sphere").localRotation = Quaternion.Euler(0f, 180f, 180f);

            Transform north = minimap._globeMeshTransform.Find("PointLight_HUD_MiniMap_NorthPole");
            Transform south = minimap._globeMeshTransform.Find("PointLight_HUD_MiniMap_SouthPole");
            Vector3 temp = north.localPosition;
            north.localPosition = south.localPosition;
            south.localPosition = temp;

            north = minimap._globeMeshTransform.Find("MinimapNorthPoleLightBulb");
            south = minimap._globeMeshTransform.Find("MinimapSouthPoleLightBulb");
            temp = north.localPosition;
            north.localPosition = south.localPosition;
            south.localPosition = temp;
        }
        else if (num == 4)
        {
            foreach (var lightmap in SELocator.GetShipTransform().GetComponentsInChildren<LightmapController>())
            {
                ShipLight sl = lightmap.GetComponent<ShipLight>();
                if (sl != null)
                {
                    sl._baseIntensity /= 1.5f;
                    sl._baseEmission /= 1.5f;
                }
            }
        }
        else if (num == 5)
        {
            ShipOxygenTankComponent tank = SELocator.GetShipTransform().GetComponentInChildren<ShipOxygenTankComponent>();
            OWAudioSource source = tank._damageEffect._particleAudioSource;
            source._audioLibraryClip = AudioType.None;
            source._clipArrayLength = 0;
            source._clipArrayIndex = -1;
            source.clip = LoadAudio("Assets/ShipEnhancements/AudioClip/oxygen_leak_strange.ogg");
        }

        if (lastNum < 0 && UnityEngine.Random.value < 0.25f)
        {
            RandomShipEffect(num);
        }
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
        float dragMultiplier = Mathf.Max((float)atmosphereAngularDragMultiplier.GetProperty(), 0f);
        SELocator.GetShipBody()._rigidbody.angularDrag = 0.94f * dragMultiplier;
        SELocator.GetShipBody().GetComponent<ShipThrusterModel>()._angularDrag = 0.94f * dragMultiplier;
    }

    private void OnExitFluid(FluidVolume fluid)
    {
        if (SELocator.GetShipDetector().GetComponent<ShipFluidDetector>()._activeVolumes.Count == 0)
        {
            float dragMultiplier = Mathf.Max((float)spaceAngularDragMultiplier.GetProperty(), 0f);
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

        if ((bool)enableAutoHatch.GetProperty() && !InMultiplayer
            && !(bool)disableHatch.GetProperty())
        {
            HatchController hatchController = SELocator.GetShipBody().GetComponentInChildren<HatchController>();
            hatchController._interactVolume.EnableInteraction();
            hatchController.GetComponent<SphereShape>().radius = 1f;
            hatchController.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            hatchController.transform.parent.GetComponentInChildren<AutoHatchController>().DisableInteraction();
        }
    }

    private void OnExitShip()
    {
        foreach (OWAudioSource audio in _shipAudioToChange)
        {
            audio.spatialBlend = 1f;
        }

        if ((bool)enableAutoHatch.GetProperty() && !InMultiplayer
            && !(bool)disableHatch.GetProperty())
        {
            HatchController hatchController = SELocator.GetShipBody().GetComponentInChildren<HatchController>();
            hatchController._interactVolume.DisableInteraction();
            hatchController.GetComponent<SphereShape>().radius = 3.5f;
            hatchController.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }

        if ((float)tractorBeamLengthMultiplier.GetProperty() < 0f &&
            (bool)disableHatch.GetProperty())
        {
            ShipTractorBeamSwitch beamSwitch =
                SELocator.GetShipTransform().GetComponentInChildren<ShipTractorBeamSwitch>();
            if (beamSwitch.GetComponent<OWTriggerVolume>().IsTrackingObject(Locator.GetPlayerDetector()))
            {
                beamSwitch.ActivateTractorBeam();
            }
        }
    }

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
        SELocator.GetShipBody().SetCenterOfMass(SELocator.GetShipBody().GetWorldCenterOfMass());
        ThrustIndicatorManager.DisableIndicator();
    }

    private void OnWakeUp()
    {
        bool allRainbow = !(bool)enableColorBlending.GetProperty()
            && (string)interiorHullColor1.GetProperty() == "Rainbow"
            && (string)exteriorHullColor1.GetProperty() == "Rainbow"
            && (string)shipLightColor1.GetProperty() == "Rainbow"
            && (string)thrusterColor1.GetProperty() == "Rainbow"
            && (string)indicatorColor1.GetProperty() == "Rainbow";
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

        if ((bool)enableRepairConfirmation.GetProperty() && !anyDamaged)
        {
            SELocator.GetShipTransform().Find("Audio_Ship/SystemOnlineAudio")?.GetComponent<OWAudioSource>().PlayOneShot(AudioType.TH_ZeroGTrainingAllRepaired, 1f);
        }
        if ((bool)enableFragileShip.GetProperty())
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
        ShipCockpitController cockpitController = FindObjectOfType<ShipCockpitController>();
        cockpitController._externalLightsOn = false;

        disableShipHeadlights = true;
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

    public void AddShipAudioToChange(OWAudioSource audioSource)
    {
        _shipAudioToChange.Add(audioSource);
        audioSource.spatialBlend = PlayerState.IsInsideShip() ? 0f : 1f;
    }

    #endregion

    public static void WriteDebugMessage(object msg, bool warning = false, bool error = false)
    {
        LogMessage(msg, warning, error);
    }

    public static void LogMessage(object msg, bool warning = false, bool error = false)
    {
        var type = warning ? MessageType.Warning : error ? MessageType.Error : MessageType.Message;
        Instance?.ModHelper?.Console?.WriteLine(msg?.ToString() ?? "null", type);
    }

    public static GameObject LoadPrefab(string path)
    {
        var obj = (GameObject)Instance._shipEnhancementsBundle.LoadAsset(path);
        AssetBundleUtilities.ReplaceShaders(obj);
        return obj;
    }

    public static AudioClip LoadAudio(string path)
    {
        return (AudioClip)Instance._shipEnhancementsBundle.LoadAsset(path);
    }
    
    public static Material LoadMaterial(string path)
    {
        Material mat = (Material)Instance._shipEnhancementsBundle.LoadAsset(path);
        AssetBundleUtilities.ReplaceMaterialShader(mat);
        return mat;
    }

    public static T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        return Instance._shipEnhancementsBundle.LoadAsset<T>(path);
    }

    public static GameObject CreateObject(GameObject obj)
    {
        var clone = Instantiate(obj);
        clone.name = obj.name;
        return clone;
    }

    public static GameObject CreateObject(GameObject obj, Transform parent)
    {
        var clone = Instantiate(obj, parent);
        clone.name = obj.name;
        return clone;
    }

    public static GameObject CreateObject(GameObject obj, Vector3 position, Quaternion rotation)
    {
        return CreateObject(obj, position, rotation, null);
    }

    public static GameObject CreateObject(GameObject obj, Vector3 position, Quaternion rotation, Transform parent)
    {
        var clone = Instantiate(obj, position, rotation, parent);
        clone.name = obj.name;
        return clone;
    }

    public override void Configure(IModConfig config)
    {
        if (!SettingsPresets.Initialized())
        {
            return;
        }

        SEMenuManager.CurrentPreset = SettingsPresets.GetPresetFromConfig(config.GetSettingsValue<string>("preset"));
        var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];

        foreach (Settings setting in allSettings)
        {
            setting.SetValue(config.GetSettingsValue<object>(setting.GetName()));
        }
    }

    public override object GetApi()
    {
        return new ShipEnhancementsAPI();
    }

    public static void DumpMats(string filepath)
    {
        var q = new List<Dictionary<string, object>>();
        foreach (var r in SELocator.GetShipBody().GetComponentsInChildren<MeshRenderer>())
        {
            var p = new Dictionary<string, object>();
            q.Add(p);
            p.Add("GOID", r.gameObject.GetInstanceID());
            var ms = new List<Dictionary<string, object>>();
            p.Add("materials", ms);
            foreach (var m in r.sharedMaterials)
            {
                var md = new Dictionary<string, object>();
                ms.Add(md);
                md.Add("matID", m?.GetInstanceID());
                md.Add("hash", m?.GetHashCode());
                md.Add("name", m?.name);
            }
        }

        File.WriteAllText(filepath, JsonConvert.SerializeObject(q));
        
        WriteDebugMessage("[X0A] mats dumped");
    }
}
