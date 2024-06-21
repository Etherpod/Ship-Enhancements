using OWML.Common;
using OWML.ModHelper;
using System.Collections;
using UnityEngine;

namespace ShipEnhancements;
public class ShipEnhancements : ModBehaviour
{
    public static ShipEnhancements Instance;
    public bool HeadlightsDisabled { get; private set; }

    private bool _gravityCrystalDisabled;
    private bool _ejectButtonDisabled;
    private bool _headlightsDisabled;
    private bool _landingCameraDisabled;
    private bool _shipLightsDisabled;

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

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;
            HeadlightsDisabled = _headlightsDisabled;
            StartCoroutine(WaitForShip());
        };

        LoadManager.OnStartSceneLoad += (scene, loadScene) =>
        {
            if (scene != OWScene.SolarSystem) return;
        };
    }

    private IEnumerator WaitForShip()
    {
        yield return new WaitUntil(() => Locator._shipBody != null);
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
            ElectricalComponent[] lightComponents = Locator.GetShipBody().GetComponentInChildren<ShipHeadlightComponent>()._electricalSystem._connectedComponents;
            foreach (ElectricalComponent light in lightComponents)
            {
                light.GetComponent<ShipLight>().SetDamaged(true);
            }
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
    }
}
