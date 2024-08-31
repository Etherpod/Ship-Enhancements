using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShipEnhancements;

public class ShipOverdriveController : MonoBehaviour
{
    private Renderer[] _thrusterRenderers;
    private Light[] _thrusterLights;
    private bool _charging = false;
    private bool _onCooldown = false;
    private readonly float _cooldownLength = 8f;
    private float _cooldownT;
    private Color _defaultColor;
    private Color _overdriveColor;
    private readonly float _thrustMultiplier = 6f;
    private ShipReactorComponent _reactor;

    public bool Charging { get { return _charging; } }
    public bool OnCooldown { get { return _onCooldown; } }
    public float ThrustMultiplier
    {
        get
        {
            return Mathf.Lerp(1f, _thrustMultiplier, _cooldownT);
        }
    }

    private void Awake()
    {
        _reactor = Locator.GetShipTransform().GetComponentInChildren<ShipReactorComponent>();
        GlobalMessenger.AddListener("ShipSystemFailure", StopAllCoroutines);
        ShipEnhancements.Instance.OnFuelDepleted += StopAllCoroutines;
    }

    private void Start()
    {
        List<Renderer> renderers = [];
        List<Light> lights = [];
        foreach (ThrusterFlameController flame in Locator.GetShipTransform().GetComponentsInChildren<ThrusterFlameController>())
        {
            renderers.Add(flame.GetComponentInChildren<MeshRenderer>());
            lights.Add(flame.GetComponentInChildren<Light>());
        }
        _thrusterRenderers = [.. renderers];
        _thrusterLights = [.. lights];
        _defaultColor = _thrusterRenderers[0].material.GetColor("_Color");
        Material overdriveMat = (Material)ShipEnhancements.LoadAsset("Assets/ShipEnhancements/Effects_HEA_ThrusterFlames_Overdrive_mat.mat");
        _overdriveColor = overdriveMat.GetColor("_Color");
    }

    private void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame && !_charging && !_onCooldown)
        {
            StartCoroutine(OverdriveDelay());
        }
        if (_onCooldown)
        {
            if (_cooldownT > 0f)
            {
                foreach (Renderer renderer in _thrusterRenderers)
                {
                    renderer.material.SetColor("_Color", Color.Lerp(_defaultColor, _overdriveColor, _cooldownT));
                }
                _cooldownT -= Time.deltaTime / _cooldownLength;
            }
            else
            {
                _onCooldown = false;
            }
        }
    }

    private void Overdrive()
    {
        if (!_reactor.isDamaged)
        {
            Locator.GetPlayerAudioController()._oneShotSource.PlayOneShot(AudioType.EyeBigBang);
            _reactor.SetDamaged(true);
        }
        else
        {
            SELocator.GetShipDamageController().Explode();
            return;
        }
        Locator.GetShipBody().AddImpulse(Locator.GetShipTransform().forward * 500f);
        SELocator.GetShipResources().DrainFuel(150f);
        ShipElectricalComponent electrical = SELocator.GetShipDamageController()._shipElectricalComponent;
        electrical._electricalSystem.Disrupt(electrical._disruptionLength);
        _cooldownT = 1f;
        _onCooldown = true;
    }

    private IEnumerator OverdriveDelay()
    {
        _charging = true;
        Locator.GetPlayerAudioController()._oneShotSource.PlayOneShot(AudioType.NomaiTimeLoopClose);
        yield return new WaitForSeconds(3f);
        _charging = false;
        Overdrive();
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", StopAllCoroutines);
        ShipEnhancements.Instance.OnFuelDepleted -= StopAllCoroutines;
    }
}
