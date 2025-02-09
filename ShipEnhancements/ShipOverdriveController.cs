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
    private OWAudioSource _panelAudioSource;
    [SerializeField]
    private OWAudioSource _shipAudioSource;

    private Renderer[] _thrusterRenderers;
    private Light[] _thrusterLights;
    private bool _charging = false;
    private bool _onCooldown = false;
    private readonly float _cooldownLength = 8f;
    private float _cooldownT;
    private Color _defaultColor;
    private Color _overdriveColor;
    private Color _indicatorColor = new Color(0.49853f, 0.38774f, 5.29f);
    private readonly float _thrustMultiplier = 6f;
    private ShipReactorComponent _reactor;
    private ElectricalSystem _electricalSystem;
    private CockpitButtonPanel _buttonPanel;
    private ThrustModulatorController _modulatorController;
    private bool _wasDisrupted = false;
    private int _focusedButtons;
    private bool _focused = false;
    private bool _wasInFreeLook = false;
    private float _buttonResetTime = 2.5f;
    private float _resetStartTime;
    private bool _onResetTimer = false;
    private bool _fuelDepleted = false;
    private bool _thrustersUsable = true;

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
        _reactor = SELocator.GetShipTransform().GetComponentInChildren<ShipReactorComponent>();
        _modulatorController = GetComponent<ThrustModulatorController>();
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        ShipEnhancements.Instance.OnFuelDepleted += OnFuelDepleted;
        ShipEnhancements.Instance.OnFuelRestored += OnFuelRestored;

        _electricalSystem = SELocator.GetShipTransform()
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
        foreach (ThrusterFlameController flame in SELocator.GetShipTransform().GetComponentsInChildren<ThrusterFlameController>())
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
                if ((string)thrusterColor.GetProperty() == "Rainbow")
                {
                    foreach (Renderer renderer in _thrusterRenderers)
                    {
                        renderer.material.SetColor("_Color", Color.Lerp(RainbowShipThrusters.currentThrusterColor, _overdriveColor, _cooldownT));
                    }
                }
                else
                {
                    foreach (Renderer renderer in _thrusterRenderers)
                    {
                        renderer.material.SetColor("_Color", Color.Lerp(_defaultColor, _overdriveColor, _cooldownT));
                    }
                }

                ThrustIndicatorManager.LayerColor(_indicatorColor, _cooldownT);

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
        if (_thrustersUsable != CanUseOverdrive())
        {
            _thrustersUsable = CanUseOverdrive();
            if (!_thrustersUsable)
            {
                _primeButton.SetButtonOn(false);
                _activateButton.SetButtonActive(false);
                SetPowered(false);
            }
            else
            {
                if (!_powered && !SELocator.GetShipDamageController().IsElectricalFailed()
                    && ShipEnhancements.Instance.engineOn)
                {
                    SetPowered(true);
                }
            }
        }
        if (OWInput.IsPressed(InputLibrary.freeLook, InputMode.ShipCockpit) != _wasInFreeLook)
        {
            _wasInFreeLook = OWInput.IsPressed(InputLibrary.freeLook, InputMode.ShipCockpit);
            if (!_wasInFreeLook && !_charging)
            {
                StopAllCoroutines();
                _onResetTimer = false;
                _primeButton.SetButtonOn(false);
                _activateButton.SetButtonActive(false);

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendStopOverdriveCoroutines(id);
                        ShipEnhancements.QSBCompat.SendOverdriveButtonState(id, true, false, true);
                        ShipEnhancements.QSBCompat.SendOverdriveButtonState(id, false, false, true);
                    }
                }
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
        bool host = !ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost();

        if (!_reactor.isDamaged)
        {
            _shipAudioSource.PlayOneShot(AudioType.EyeBigBang);
            if ((bool)extraNoise.GetProperty())
            {
                SELocator.GetShipDetector().GetComponent<ShipNoiseMaker>()._noiseRadius = 800f;
            }

            if (host)
            {
                _reactor.SetDamaged(true);
            }
        }
        else if (host)
        {
            SELocator.GetShipDamageController().Explode();
            return;
        }

        if (host)
        {
            SELocator.GetShipBody().AddImpulse(SELocator.GetShipTransform().forward * 500f);
            SELocator.GetShipResources().DrainFuel(150f);
            ShipElectricalComponent electrical = SELocator.GetShipDamageController()._shipElectricalComponent;
            electrical._electricalSystem.Disrupt(electrical._disruptionLength);
        }

        _defaultColor = _thrusterRenderers[0].material.GetColor("_Color");
        _primeButton.SetButtonOn(false);
        _activateButton.SetButtonActive(false);
        _cooldownT = 1f;
        _onCooldown = true;
    }

    private IEnumerator OverdriveDelay()
    {
        _charging = true;
        _shipAudioSource.AssignAudioLibraryClip(AudioType.NomaiTimeLoopClose);
        _shipAudioSource.time = 0f;
        _shipAudioSource.Play();
        _panelAudioSource.Play();
        _modulatorController.BeginOverdriveSequence();
        yield return new WaitForSeconds(3f);
        _charging = false;
        Overdrive();
        _modulatorController.EndOverdriveSequence();
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
            _charging = false;
            _onResetTimer = false;
            StopAllCoroutines();
            _modulatorController.EndOverdriveSequence();
            _shipAudioSource.Stop();
            _panelAudioSource.Stop();
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
                _onResetTimer = false;
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

    private void OnShipSystemFailure()
    {
        InterruptOverdrive();
        enabled = false;
    }

    private void OnFuelDepleted()
    {
        _fuelDepleted = true;
        _primeButton.SetButtonOn(false);
        _activateButton.SetButtonActive(false);
        SetPowered(false);
    }

    private void OnFuelRestored()
    {
        _fuelDepleted = false;
        if (!_powered && !SELocator.GetShipDamageController().IsElectricalFailed()
            && ShipEnhancements.Instance.engineOn)
        {
            SetPowered(true);
        }
    }

    public override void SetPowered(bool powered)
    {
        if (!(bool)enableThrustModulator.GetProperty() || (powered && _fuelDepleted) || (powered && !_thrustersUsable)) return;
        base.SetPowered(powered);
        if (!powered)
        {
            InterruptOverdrive();
            _primeButton.SetButtonOn(false);
            _activateButton.SetButtonActive(false);
        }

        _primeButton.SetButtonPowered(powered, _electricalSystem.IsDisrupted());
        _activateButton.SetButtonPowered(powered, _electricalSystem.IsDisrupted());
    }

    public void PlayButtonAudio(AudioClip audio, float volume)
    {
        _panelAudioSource.pitch = Random.Range(0.9f, 1.1f);
        _panelAudioSource.PlayOneShot(audio, volume);
    }

    private bool CanUseOverdrive()
    {
        ShipThrusterModel thrusters = SELocator.GetShipBody().GetComponent<ShipThrusterModel>();
        return SELocator.GetShipResources().AreThrustersUsable() 
            && (thrusters.IsThrusterBankEnabled(ThrusterBank.Left) || thrusters.IsThrusterBankEnabled(ThrusterBank.Right));
    }

    public bool IsCharging()
    {
        return _charging;
    }

    public bool IsCooldown()
    {
        return _onCooldown;
    }

    public OverdriveButton GetButton(bool isPrimeButton)
    {
        return isPrimeButton ? _primeButton : _activateButton;
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        ShipEnhancements.Instance.OnFuelDepleted -= OnFuelDepleted;
        ShipEnhancements.Instance.OnFuelRestored -= OnFuelRestored;
    }
}
