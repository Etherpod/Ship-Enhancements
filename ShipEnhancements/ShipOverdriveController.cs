using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShipEnhancements;

public class ShipOverdriveController : MonoBehaviour
{
    ThrusterFlameColorSwapper _flameColorSwapper;
    ShipAudioController _audioController;
    private bool _charging = false;

    private void Start()
    {
        _flameColorSwapper = GetComponent<ThrusterFlameColorSwapper>();
        _audioController = Locator.GetShipTransform().GetComponentInChildren<ShipAudioController>();

        List<Renderer> renderers = [];
        List<Light> lights = [];
        foreach (ThrusterFlameController flame in Locator.GetShipTransform().GetComponentsInChildren<ThrusterFlameController>())
        {
            renderers.Add(flame.GetComponentInChildren<MeshRenderer>());
            lights.Add(flame.GetComponentInChildren<Light>());
        }
        _flameColorSwapper._thrusterRenderers = [.. renderers];
        _flameColorSwapper._thrusterLights = [.. lights];
    }

    private void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame && !_charging)
        {
            StartCoroutine(OverdriveDelay());
        }
    }

    private void Overdrive()
    {
        _flameColorSwapper.SetFlameColor(true);
        Locator.GetShipTransform().GetComponentInChildren<ShipReactorComponent>().SetDamaged(true);
        Locator.GetShipBody().AddImpulse(Locator.GetShipTransform().forward * 500f);
    }

    private IEnumerator OverdriveDelay()
    {
        _charging = true;
        Locator.GetPlayerAudioController()._oneShotSource.PlayOneShot(AudioType.NomaiTimeLoopClose);
        yield return new WaitForSeconds(3f);
        _charging = false;
        Locator.GetPlayerAudioController()._oneShotSource.PlayOneShot(AudioType.EyeBigBang);
        Overdrive();
    }
}
