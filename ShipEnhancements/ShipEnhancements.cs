using OWML.Common;
using OWML.ModHelper;
using System.Collections;
using UnityEngine;

namespace ShipEnhancements;
public class ShipEnhancements : ModBehaviour
{
    public static ShipEnhancements Instance;
    public bool oxygenDepleted;

    public bool HeadlightsDisabled { get; private set; }
    public bool LandingCameraDisabled { get; private set; }
    public float OxygenDrainMultiplier { get; private set; }
    public float FuelDrainMultiplier { get; private set; }
    public float DamageMultiplier { get; private set; }
    public float DamageSpeedMultiplier { get; private set; }

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

    private float _lastSuitOxygen;

    private void Awake()
    {
        Instance = this;
        HarmonyLib.Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly());
    }

    private void Start()
    {
        _gravityCrystalDisabled = ModHelper.Config.GetSettingsValue<bool>("disableGravityCrystal");
        _ejectButtonDisabled = ModHelper.Config.GetSettingsValue<bool>("disableEjectButton");
        _headlightsDisabled = ModHelper.Config.GetSettingsValue<bool>("disableHeadlights");
        _landingCameraDisabled = ModHelper.Config.GetSettingsValue<bool>("disableLandingCamera");
        _shipLightsDisabled = ModHelper.Config.GetSettingsValue<bool>("disableShipLights");
        _oxygenDrainMultiplier = ModHelper.Config.GetSettingsValue<float>("oxygenDrainMultiplier");
        _fuelDrainMultiplier = ModHelper.Config.GetSettingsValue<float>("fuelDrainMultiplier");
        _damageMultiplier = ModHelper.Config.GetSettingsValue<float>("shipDamageMultiplier");
        _damageSpeedMultiplier = ModHelper.Config.GetSettingsValue<float>("shipDamageSpeedMultiplier");


        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

            GlobalMessenger.AddListener("SuitUp", OnPlayerSuitUp);
            GlobalMessenger.AddListener("RemoveSuit", OnPlayerRemoveSuit);
            oxygenDepleted = false;
            HeadlightsDisabled = _headlightsDisabled;
            LandingCameraDisabled = _landingCameraDisabled;
            OxygenDrainMultiplier = _oxygenDrainMultiplier;
            FuelDrainMultiplier = _fuelDrainMultiplier;
            DamageMultiplier = _damageMultiplier;
            DamageSpeedMultiplier = _damageSpeedMultiplier;
            StartCoroutine(InitializeShip());
        };

        LoadManager.OnStartSceneLoad += (scene, loadScene) =>
        {
            if (scene != OWScene.SolarSystem) return;

            GlobalMessenger.RemoveListener("SuitUp", OnPlayerSuitUp);
            GlobalMessenger.RemoveListener("RemoveSuit", OnPlayerRemoveSuit);
            _lastSuitOxygen = 0f;
        };
    }

    private void Update()
    {
        if (LoadManager.GetCurrentScene() != OWScene.SolarSystem) return;

        if (!oxygenDepleted && Locator.GetShipBody().GetComponent<ShipResources>().GetOxygen() <= 0)
        {
            oxygenDepleted = true;
            if (PlayerState.AtFlightConsole() && PlayerState.IsWearingSuit())
            {
                Locator.GetPlayerSuit().PutOnHelmet();
            }
            Locator.GetShipBody().GetComponentInChildren<OxygenVolume>().OnEffectVolumeExit(Locator.GetPlayerDetector());
        }
    }

    private IEnumerator InitializeShip()
    {
        yield return new WaitUntil(() => Locator._shipBody != null);

        _lastSuitOxygen = Locator.GetPlayerBody().GetComponent<PlayerResources>()._currentOxygen;

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
    }

    private static void DisableHeadlights()
    {
        ShipHeadlightComponent headlightComponent = Locator.GetShipBody().GetComponentInChildren<ShipHeadlightComponent>();
        headlightComponent._persistentCollider = true;
        headlightComponent._repairReceiver.DisableCollider();
        headlightComponent._damaged = true;
        headlightComponent._repairFraction = 0f;
        headlightComponent.OnComponentDamaged();
    }

    private void DisableGravityCrystal()
    {
        ShipGravityComponent gravityComponent = Locator.GetShipBody().GetComponentInChildren<ShipGravityComponent>();
        gravityComponent._persistentCollider = true;
        gravityComponent._repairReceiver.DisableCollider();
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
        cameraComponent._persistentCollider = true;
        cameraComponent._repairReceiver.DisableCollider();
        cameraComponent._damaged = true;
        cameraComponent._repairFraction = 0f;
        cameraComponent._landingCamera.SetPowered(false);
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
        _lastSuitOxygen = Locator.GetPlayerBody().GetComponent<PlayerResources>()._currentOxygen;
    }

    public static void WriteDebugMessage(object msg)
    {
        Instance.ModHelper.Console.WriteLine(msg.ToString());
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
    }
}
