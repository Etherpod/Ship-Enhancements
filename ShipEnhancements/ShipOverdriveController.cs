using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipOverdriveController : ElectricalComponent
{
    [SerializeField]
    private OverdriveButton _primeButton;
    [SerializeField]
    private OverdriveButton _activateButton;
    [SerializeField]
    private OWAudioSource _audioSource;

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
    private ElectricalSystem _electricalSystem;
    private CockpitButtonPanel _buttonPanel;
    private bool _wasDisrupted = false;
    private int _focusedButtons;
    private bool _focused = false;
    private bool _wasInFreeLook = false;
    private float _buttonResetTime = 2.5f;
    private float _resetStartTime;
    private bool _onResetTimer = false;

    public bool Charging { get { return _charging; } }
    public bool OnCooldown { get { return _onCooldown; } }
    public float ThrustMultiplier
    {
        get
        {
            return Mathf.Lerp(1f, _thrustMultiplier, _cooldownT);
        }
    }

    public override void Awake()
    {
        if (!(bool)enableThrustModulator.GetProperty())
        {
            return;
        }

        base.Awake();
        _buttonPanel = GetComponentInParent<CockpitButtonPanel>();
        _reactor = Locator.GetShipTransform().GetComponentInChildren<ShipReactorComponent>();
        GlobalMessenger.AddListener("ShipSystemFailure", InterruptOverdrive);
        ShipEnhancements.Instance.OnFuelDepleted += InterruptOverdrive;

        _electricalSystem = Locator.GetShipTransform()
            .Find("Module_Cockpit/Systems_Cockpit/FlightControlsElectricalSystem")
            .GetComponent<ElectricalSystem>();
        List<ElectricalComponent> componentList = [.. _electricalSystem._connectedComponents];
        componentList.Add(this);
        _electricalSystem._connectedComponents = [.. componentList];
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
        _primeButton.SetButtonOn(false);
        _activateButton.SetButtonActive(false);
    }

    private void Update()
    {
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
        if (_electricalSystem.IsDisrupted() != _wasDisrupted)
        {
            _wasDisrupted = _electricalSystem.IsDisrupted();
            _primeButton.OnDisruptedEvent(_wasDisrupted);
            _activateButton.OnDisruptedEvent(_wasDisrupted);
        }
        if (OWInput.IsPressed(InputLibrary.freeLook) != _wasInFreeLook)
        {
            _wasInFreeLook = OWInput.IsPressed(InputLibrary.freeLook);
            if (!_wasInFreeLook && !_charging)
            {
                StopAllCoroutines();
                _primeButton.SetButtonOn(false);
                _activateButton.SetButtonActive(false);
            }
        }
        if (_onResetTimer && !_charging)
        {
            if (Time.time > _resetStartTime + _buttonResetTime)
            {
                _onResetTimer = false;
                _primeButton.SetButtonOn(false);
                _activateButton.SetButtonActive(false);
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
        _primeButton.SetButtonOn(false);
        _activateButton.SetButtonActive(false);
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

    private IEnumerator DisableSafetiesDelay()
    {
        yield return new WaitForSeconds(0.4f);
        _activateButton.SetButtonActive(true);
        _resetStartTime = Time.time;
        _onResetTimer = true;
    }

    private void InterruptOverdrive()
    {
        if (_charging)
        {
            StopAllCoroutines();
            _audioSource.Stop();
            _charging = false;
        }
    }

    public void OnPressInteract(bool isPrimeButton, bool isButtonOn)
    {
        if (isPrimeButton)
        {
            if (_activateButton.IsOn() && !isButtonOn)
            {
                InterruptOverdrive();
                _activateButton.SetButtonActive(false);
            }
            else if (_activateButton.IsActive() && !isButtonOn)
            {
                _activateButton.SetButtonActive(false);
            }
            else if (!_activateButton.IsActive() && !isButtonOn)
            {
                StopAllCoroutines();
            }
            else if (!_activateButton.IsOn() && isButtonOn)
            {
                StartCoroutine(DisableSafetiesDelay());
            }
        }
        else
        {
            if (_primeButton.IsOn())
            {
                StartCoroutine(OverdriveDelay());
            }
        }
    }

    public override void SetPowered(bool powered)
    {
        if (!(bool)enableThrustModulator.GetProperty()) return;
        base.SetPowered(powered);
        _primeButton.SetPowered(powered, _electricalSystem.IsDisrupted());
        _activateButton.SetPowered(powered, _electricalSystem.IsDisrupted());
        if (!_electricalSystem.IsDisrupted())
        {
            InterruptOverdrive();
        }
    }

    public void PlayButtonAudio(AudioClip audio, float volume)
    {
        _audioSource.pitch = Random.Range(0.9f, 1.1f);
        _audioSource.PlayOneShot(audio, volume);
    }

    public void UpdateFocusedButtons(bool add)
    {
        _focusedButtons = Mathf.Max(_focusedButtons + (add ? 1 : -1), 0);
        if (_focused != _focusedButtons > 0)
        {
            _focused = _focusedButtons > 0;
            _buttonPanel.UpdateFocusedButtons(_focused);
        }
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", InterruptOverdrive);
        ShipEnhancements.Instance.OnFuelDepleted -= InterruptOverdrive;
    }
}
