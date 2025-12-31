using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ResourcePump : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.ResourcePumpType;

    [Space]
    [SerializeField]
    private Vector3 _holdPosition;
    [SerializeField]
    private Vector3 _holdRotation;
    [SerializeField]
    private Vector3 _holdScale = Vector3.one;
    [SerializeField]
    private Vector3 _inputDropOffset;
    [SerializeField]
    private Vector3 _inputDropRotation;

    [Space]
    [SerializeField]
    private OWRenderer _signalRenderer;
    [SerializeField]
    private float _maxShipDistance = 1000f;
    [SerializeField]
    private OWAudioSource _alarmSource;
    [SerializeField]
    private OWRenderer _outputDisplayRenderer;
    [SerializeField]
    private OWRenderer _typeDisplayRenderer;

    [Space]
    [SerializeField]
    private PumpFlameController _flameController;
    [SerializeField]
    private OWAudioSource _flameLoopSource;
    [SerializeField]
    private float _thrusterStrength;
    [SerializeField]
    private OWAudioSource _fuelInputSource;
    [SerializeField]
    private ParticleSystem _fuelInputParticles;
    [SerializeField]
    private ParticleSystem _fuelTransferParticles;
    [SerializeField]
    private OWAudioSource _fuelDryInputSource;

    [Space]
    [SerializeField]
    private OxygenVolume _oxygenVolume;
    [SerializeField]
    private OWAudioSource _oxygenOutputSource;
    [SerializeField]
    private OWAudioSource _oxygenInputSource;
    [SerializeField]
    private OWAudioSource _oxygenDryInputSource;
    [SerializeField]
    private OxygenDetector _oxygenDetector;
    [SerializeField]
    private ParticleSystem _oxygenOutputParticles;
    [SerializeField]
    private ParticleSystem _oxygenInputParticles;

    [Space]
    [SerializeField]
    private ParticleSystem _geyserParticles;
    [SerializeField]
    private GeyserFluidVolume _geyserVolume;
    [SerializeField]
    private OWAudioSource _geyserLoopSource;
    [SerializeField]
    private FluidDetector _fluidDetector;
    [SerializeField]
    private OWAudioSource _waterInputSource;
    [SerializeField]
    private OWAudioSource _waterDryInputSource;
    [SerializeField]
    private ParticleSystem _waterInputParticles;

    private EffectVolume[] _volumes;
    private FirstPersonManipulator _cameraManipulator;
    private OWAudioSource _playerExternalSource;
    private OWCamera _playerCam;
    private ShipResources _shipResources;
    private ShipFluidDetector _shipFluidDetector;
    private bool _lastFocused = false;
    private bool _dropped = true;

    private readonly int _batteryPropID = Shader.PropertyToID("_Battery");
    private readonly int _outputPropID = Shader.PropertyToID("_IsOutput");
    private readonly int _errorPropID = Shader.PropertyToID("_IsError");
    private readonly int _emissionPropID = Shader.PropertyToID("_Emission");
    private readonly int _typeIndexPropID = Shader.PropertyToID("_TypeIndex");
    private readonly int _resourceRatioPropID = Shader.PropertyToID("_Ratio");
    private bool _inSignalRange;
    private bool _shipDestroyed = false;

    private PriorityScreenPrompt _switchTypePrompt;
    private PriorityScreenPrompt _switchModePrompt;
    private PriorityScreenPrompt _powerPrompt;

    private ResourceType _currentType = ResourceType.Fuel;
    private int _currentTypeIndex = 0;
    private bool _isOutput = true;
    private bool _powered = false;
    private bool _outputError = false;

    private bool _geyserActive = false;
    private bool _flameActive = false;
    private bool _fuelInputActive = false;
    private bool _oxygenInputActive = false;
    private bool _waterInputActive = false;

    private List<PlayerRecoveryPoint> _activeRecoveryPoints = [];
    private List<ParticleSystem> _activeTransferParticles = [];
    private List<FluidVolume> _waterVolumes = [];
    private int _collisionCheckInterval = 30;
    private int _framesUntilCollisionCheck;

    public enum ResourceType
    {
        Fuel,
        Oxygen,
        Water
    }

    public override string GetDisplayName()
    {
        return "Resource Pump";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _volumes = GetComponentsInChildren<EffectVolume>(true);
        _cameraManipulator = FindObjectOfType<FirstPersonManipulator>();
        _switchTypePrompt = new PriorityScreenPrompt(InputLibrary.toolOptionLeft, InputLibrary.toolOptionRight, "Switch Type (Fuel)", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _switchModePrompt = new PriorityScreenPrompt(InputLibrary.toolOptionUp, InputLibrary.toolOptionDown, "Switch Mode (Output)", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _powerPrompt = new PriorityScreenPrompt(InputLibrary.interactSecondary, "Toggle Power (Off)", 0, ScreenPrompt.DisplayState.Normal, false);

        _maxShipDistance = ShipEnhancements.ExperimentalSettings?.ResourcePump_SignalRange ?? _maxShipDistance;

        ShipEnhancements.Instance.OnResourceDepleted += OnResourceDepleted;
        ShipEnhancements.Instance.OnResourceRestored += OnResourceRestored;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        GlobalMessenger.AddListener("ShipDestroyed", OnShipDestroyed);

        List<ParticleSystem> systems = _particleSystems.ToList();
        systems.Clear();
        _particleSystems = systems.ToArray();
    }

    private void Start()
    {
        _playerExternalSource = Locator.GetPlayerAudioController()._oneShotExternalSource;
        _playerCam = Locator.GetPlayerCamera();
        Locator.GetPromptManager().AddScreenPrompt(_switchTypePrompt, PromptPosition.Center);
        Locator.GetPromptManager().AddScreenPrompt(_switchModePrompt, PromptPosition.Center);
        Locator.GetPromptManager().AddScreenPrompt(_powerPrompt, PromptPosition.Center);

        _oxygenVolume._triggerVolume._shape.enabled = true;
        _geyserVolume._triggerVolume._shape.enabled = true;
        _oxygenVolume.SetVolumeActivation(false);
        _geyserVolume.SetVolumeActivation(false);

        string color = (string)thrusterColor1.GetProperty();
        bool thrusterBlend = ((bool)enableColorBlending.GetProperty()
            && int.Parse((string)thrusterColorOptions.GetProperty()) > 1)
            || color == "Rainbow";

        if (thrusterBlend)
        {
            _flameController.gameObject.AddComponent<ShipThrusterBlendController>();
        }
        else if (color != "Default")
        {
            MeshRenderer rend = _flameController.GetComponent<MeshRenderer>();

            ThrusterTheme thrusterColors = ShipEnhancements.ThemeManager.GetThrusterTheme(color);
            rend.material.SetTexture("_MainTex",
                ShipEnhancements.LoadAsset<Texture2D>("Assets/ShipEnhancements/ThrusterColors/"
                + thrusterColors.ThrusterColor));

            Color thrustColor = Color.white * Mathf.Pow(2, thrusterColors.ThrusterIntensity);
            thrustColor.a = thrustColor.a = 0.5019608f;
            rend.material.SetColor("_Color", thrustColor);

            Light light = _flameController.GetComponentInChildren<Light>();
            light.color = thrusterColors.ThrusterLight / 255f;
        }
    }

    private void Update()
    {
        bool focused = _cameraManipulator.GetFocusedOWItem() == this;
        if (_lastFocused != focused)
        {
            PatchClass.UpdateFocusedItems(focused);
            _lastFocused = focused;
        }

        UpdatePromptVisibility();
        
        if ((ShipEnhancements.ExperimentalSettings?.ResourcePump_RemoteActivation ?? false)
            && Keyboard.current.oKey.wasPressedThisFrame)
        {
            _powered = !_powered;
            _powerPrompt.SetText(_powered ? "Toggle Power (On)" : "Toggle Power (Off)");
            UpdatePowered(sendMessage: true);
        }
        else if (_lastFocused)
        {
            bool pressedLeft = OWInput.IsNewlyPressed(InputLibrary.toolOptionLeft, InputMode.Character);
            if (pressedLeft || OWInput.IsNewlyPressed(InputLibrary.toolOptionRight, InputMode.Character))
            {
                _currentTypeIndex = (_currentTypeIndex + 3 + (pressedLeft ? -1 : 1)) % 3;
                var nextType = (Enum.GetValues(typeof(ResourceType)) as ResourceType[])[_currentTypeIndex];
                if (nextType == ResourceType.Water && !(bool)addWaterTank.GetProperty())
                {
                    _currentTypeIndex = (_currentTypeIndex + 3 + (pressedLeft ? -1 : 1)) % 3;
                    nextType = (Enum.GetValues(typeof(ResourceType)) as ResourceType[])[_currentTypeIndex];
                }
                _switchTypePrompt.SetText($"Switch Type ({nextType})");
                _playerExternalSource.PlayOneShot(AudioType.Menu_UpDown, 0.5f);
                UpdateType(nextType, sendMessage: true);
            }

            bool pressedDown = OWInput.IsNewlyPressed(InputLibrary.toolOptionDown, InputMode.Character);
            if (pressedDown || OWInput.IsNewlyPressed(InputLibrary.toolOptionUp, InputMode.Character))
            {
                _playerExternalSource.PlayOneShot(AudioType.Menu_LeftRight, 0.5f);
                _isOutput = !_isOutput;
                _switchModePrompt.SetText($"Switch Mode ({(_isOutput ? "Output" : "Input")})");
                _outputDisplayRenderer.SetMaterialProperty(_outputPropID, _isOutput ? 1f : 0f);

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (var id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendPumpMode(id, this, _isOutput);
                    }
                }
                UpdatePowered(sendMessage: true);
            }

            if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary, InputMode.Character))
            {
                _powered = !_powered;
                _powerPrompt.SetText(_powered ? "Toggle Power (On)" : "Toggle Power (Off)");
                UpdatePowered(sendMessage: true);
            }
        }

        float lerp = 0f;
        if (!_shipDestroyed)
        {
            float distSqr = (SELocator.GetShipTransform().position - transform.position).sqrMagnitude;
            lerp = Mathf.InverseLerp(_maxShipDistance * _maxShipDistance, 50f * 50f, distSqr);
        }
        UpdateBatteryLevel(lerp);
        UpdateError();
        UpdateResourceLevel();

        if (_powered && _inSignalRange && _dropped)
        {
            float multiplier = ShipEnhancements.ExperimentalSettings?.ResourcePump_TransferMultiplier ?? 1f;
            if (_currentType == ResourceType.Fuel)
            {
                if (_isOutput)
                {
                    SELocator.GetShipResources().DrainFuel(1f * Time.deltaTime * multiplier);
                }
                else if (IsNearFuel())
                {
                    if (!_fuelInputActive)
                    {
                        _fuelInputActive = true;
                        _fuelInputSource.FadeIn(0.5f, randomizePlayhead: true);
                        _fuelDryInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                        _fuelInputParticles.Play();
                        foreach (var particle in _activeTransferParticles)
                        {
                            particle.Play();
                        }
                    }

                    SELocator.GetShipResources().DrainFuel(-2.5f * Time.deltaTime * _activeRecoveryPoints.Count * multiplier);
                }
                else if (_fuelInputActive)
                {
                    _fuelInputActive = false;
                    _fuelInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                    _fuelDryInputSource.FadeIn(0.5f, randomizePlayhead: true);
                    _fuelInputParticles.Stop();
                    foreach (var particle in _activeTransferParticles)
                    {
                        particle.Stop();
                    }
                }
            }
            else if (_currentType == ResourceType.Oxygen)
            {
                if (_isOutput)
                {
                    SELocator.GetShipResources().DrainOxygen(0.26f * Time.deltaTime * multiplier);
                }
                else if (_oxygenDetector.GetDetectOxygen())
                {
                    if (!_oxygenInputActive)
                    {
                        _oxygenInputActive = true;
                        _oxygenDryInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                        _oxygenInputSource.FadeIn(0.5f, randomizePlayhead: true);
                        _oxygenInputParticles.Play();
                    }

                    SELocator.GetShipResources().DrainOxygen(-1.5f * Time.deltaTime * multiplier);
                }
                else if (_oxygenInputActive)
                {
                    _oxygenInputActive = false;
                    _oxygenDryInputSource.FadeIn(0.5f, randomizePlayhead: true);
                    _oxygenInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                    _oxygenInputParticles.Stop();
                }
            }
            else if (_currentType == ResourceType.Water)
            {
                if (_isOutput)
                {
                    SELocator.GetShipWaterResource().DrainWater(1.5f * Time.deltaTime * multiplier);
                }
                else if (IsInWater())
                {
                    if (!_waterInputActive)
                    {
                        _waterInputActive = true;
                        _waterDryInputSource.FadeOut(0.2f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                        _waterInputSource.FadeIn(0.2f, randomizePlayhead: true);
                        _waterInputParticles.Play();
                    }

                    SELocator.GetShipWaterResource().DrainWater(-5f * Time.deltaTime * multiplier);
                }
                else if (_waterInputActive)
                {
                    _waterInputActive = false;
                    _waterDryInputSource.FadeIn(0.2f, randomizePlayhead: true);
                    _waterInputSource.FadeOut(0.2f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                    _waterInputParticles.Stop();
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (_geyserActive)
        {
            UpdateGeyserLoopingAudioPosition();
        }
        if (_flameActive && (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost()))
        {
            var body = gameObject.GetAttachedOWRigidbody();
            var toCom = transform.position - body.GetWorldCenterOfMass();
            var thrustDir = -transform.up * _thrusterStrength;
            var torqueStrength = Vector3.ProjectOnPlane(toCom, thrustDir).magnitude;
            var torqueVec = Vector3.Cross(toCom, thrustDir).normalized;

            float mult = ShipEnhancements.ExperimentalSettings?.ResourcePump_ThrustStrength ?? 1f;
            thrustDir *= mult;
            torqueStrength *= mult;

            if (!body.RunningKinematicSimulation())
            {
                float massMult = body.GetMass() < 1f ? (1f - Mathf.Pow(body.GetMass() - 1f, 2f)) : 1f;
                body.AddForce(thrustDir * massMult);
                body.AddTorque(torqueVec * torqueStrength * massMult);
            }
            else if (ShipEnhancements.ExperimentalSettings?.ResourcePump_UltraThrust ?? false)
            {
                body._kinematicRigidbody.AddForce(thrustDir);
                body._kinematicRigidbody.AddTorque(torqueVec * torqueStrength);
            }
        }
        if (_powered && !_isOutput && _inSignalRange
            && _currentType is ResourceType.Fuel or ResourceType.Water)
        {
            if (_framesUntilCollisionCheck <= 0)
            {
                UpdateFuelSources();
                UpdateWaterVolumes();
                _framesUntilCollisionCheck = _collisionCheckInterval;
            }
            else
            {
                _framesUntilCollisionCheck--;
            }
        }
    }

    private void UpdatePromptVisibility()
    {
        bool flag = _lastFocused && _playerCam.enabled && OWInput.IsInputMode(InputMode.Character | InputMode.ShipCockpit);
        if (flag != _switchModePrompt.IsVisible())
        {
            _switchTypePrompt.SetVisibility(flag);
        }
        if (flag != _switchModePrompt.IsVisible())
        {
            _switchModePrompt.SetVisibility(flag);
        }
        if (flag != _powerPrompt.IsVisible())
        {
            _powerPrompt.SetVisibility(flag);
        }
    }

    private void UpdateGeyserLoopingAudioPosition()
    {
        float num = _geyserActive ? _geyserVolume.GetHeight() : _geyserVolume.GetMaximumHeight();
        float num2 = Mathf.Clamp(transform.InverseTransformPoint(Locator.GetPlayerTransform().position).y, 0f, num);
        _geyserLoopSource.transform.localPosition = new Vector3(0f, num2, 0f);
    }

    private void UpdateType(ResourceType nextType, bool sendMessage = false)
    {
        _framesUntilCollisionCheck = _collisionCheckInterval;
        UpdateWaterVolumes();
        UpdateFuelSources();

        if (!_powered || !_inSignalRange || nextType == _currentType)
        {
            _currentType = nextType;
            _typeDisplayRenderer.SetMaterialProperty(_typeIndexPropID, _currentTypeIndex);

            if (sendMessage && ShipEnhancements.InMultiplayer)
            {
                foreach (var id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendPumpType(id, this, _currentTypeIndex);
                }
            }

            return;
        }

        if (_currentType == ResourceType.Fuel)
        {
            if (_isOutput)
            {
                if (_flameActive)
                {
                    _flameActive = false;
                    _flameController.DeactivateFlame();
                    _flameLoopSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                }
            }
            else
            {
                _fuelInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                _fuelDryInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                _fuelInputParticles.Stop();
                foreach (var particle in _activeTransferParticles)
                {
                    particle.Stop();
                }
            }
        }
        else if (nextType == ResourceType.Fuel)
        {
            if (_isOutput && SELocator.GetShipResources()._currentFuel > 0f)
            {
                if (!_flameActive)
                {
                    _flameActive = true;
                    _flameController.ActivateFlame();
                    _flameLoopSource.FadeIn(0.5f, randomizePlayhead: true);
                }
            }
            else if (!_isOutput)
            {
                if (IsNearFuel())
                {
                    _fuelInputActive = true;
                    _fuelInputParticles.Play();
                    _fuelInputSource.FadeIn(0.5f, randomizePlayhead: true);
                    foreach (var particle in _activeTransferParticles)
                    {
                        particle.Play();
                    }
                }
                else
                {
                    _fuelInputActive = false;
                    _fuelDryInputSource.FadeIn(0.5f, randomizePlayhead: true);
                }
            }
        }

        if (_currentType == ResourceType.Oxygen)
        {
            if (_isOutput)
            {
                _oxygenVolume.SetVolumeActivation(false);
                _oxygenOutputParticles.Stop();
                _oxygenOutputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
            }
            else
            {
                _oxygenInputParticles.Stop();
                _oxygenDryInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                _oxygenInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
            }
        }
        else if (nextType == ResourceType.Oxygen)
        {
            if (_isOutput && SELocator.GetShipResources()._currentOxygen > 0f)
            {
                _oxygenVolume.SetVolumeActivation(true);
                _oxygenOutputParticles.Play();
                _oxygenOutputSource.FadeIn(0.5f, randomizePlayhead: true);
            }
            else if (!_isOutput)
            {
                if (_oxygenDetector.GetDetectOxygen())
                {
                    _oxygenInputActive = true;
                    _oxygenInputParticles.Play();
                    _oxygenInputSource.FadeIn(0.5f, randomizePlayhead: true);
                }
                else
                {
                    _oxygenInputActive = false;
                    _oxygenDryInputSource.FadeIn(0.5f, randomizePlayhead: true);
                }
            }
        }

        if (_currentType == ResourceType.Water)
        {
            if (_isOutput)
            {
                if (_geyserActive)
                {
                    _geyserActive = false;
                    _geyserLoopSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                    _geyserParticles.Stop();
                    _geyserVolume.SetFiring(false);
                }
            }
            else
            {
                _waterInputSource.FadeOut(0.2f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                _waterDryInputSource.FadeOut(0.2f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                _waterInputParticles.Stop();
            }
        }
        else if (nextType == ResourceType.Water)
        {
            if (_isOutput && SELocator.GetShipWaterResource().GetWater() > 0f)
            {
                if (!_geyserActive)
                {
                    _geyserActive = true;
                    _geyserLoopSource.FadeIn(0.5f, randomizePlayhead: true);
                    _geyserParticles.Play();
                    _geyserVolume.SetFiring(true);
                    UpdateGeyserLoopingAudioPosition();
                }
            }
            else if (!_isOutput)
            {
                if (IsInWater())
                {
                    _waterInputActive = true;
                    _waterInputSource.FadeIn(0.2f, randomizePlayhead: true);
                    _waterInputParticles.Play();
                }
                else
                {
                    _waterInputActive = false;
                    _waterDryInputSource.FadeIn(0.2f, randomizePlayhead: true);
                }
            }
        }

        _currentType = nextType;
        _typeDisplayRenderer.SetMaterialProperty(_typeIndexPropID, _currentTypeIndex);

        if (sendMessage && ShipEnhancements.InMultiplayer)
        {
            foreach (var id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendPumpType(id, this, _currentTypeIndex);
            }
        }
    }

    public void UpdateTypeRemote(int typeIndex)
    {
        ShipEnhancements.WriteDebugMessage("Switching to " + typeIndex);
        _currentTypeIndex = (typeIndex + 3) % 3;
        var nextType = (Enum.GetValues(typeof(ResourceType)) as ResourceType[])[_currentTypeIndex];
        ShipEnhancements.WriteDebugMessage("Next: " + nextType);
        if (nextType == ResourceType.Water && !(bool)addWaterTank.GetProperty())
        {
            _currentTypeIndex = (_currentTypeIndex + 4) % 3;
            nextType = (Enum.GetValues(typeof(ResourceType)) as ResourceType[])[_currentTypeIndex];
        }
        _switchTypePrompt.SetText($"Switch Type ({nextType})");
        UpdateType(nextType);
    }

    public void UpdateModeRemote(bool isOutput)
    {
        _isOutput = isOutput;
        _switchModePrompt.SetText($"Switch Mode ({(_isOutput ? "Output" : "Input")})");
        _outputDisplayRenderer.SetMaterialProperty(_outputPropID, _isOutput ? 1f : 0f);
        UpdatePowered();
    }

    private void UpdatePowered(bool sendMessage = false)
    {
        UpdatePowered(_powered && _inSignalRange && _dropped, sendMessage);
    }

    public void UpdatePoweredRemote(bool powered)
    {
        _powered = powered;
        _powerPrompt.SetText(_powered ? "Toggle Power (On)" : "Toggle Power (Off)");
        UpdatePowered(powered, sendMessage: false);
    }

    private void UpdatePowered(bool powered, bool sendMessage = false)
    {
        if (_powered && _dropped && !_inSignalRange && !_alarmSource.isPlaying)
        {
            _alarmSource.Play();
        }
        else if (_alarmSource.isPlaying)
        {
            _alarmSource.Stop();
        }

        _framesUntilCollisionCheck = _collisionCheckInterval;
        UpdateWaterVolumes();
        UpdateFuelSources();

        if (_currentType == ResourceType.Fuel)
        {
            var outputPowered = powered && _isOutput && SELocator.GetShipResources()._currentFuel > 0f;
            if (_flameActive != outputPowered)
            {
                _flameActive = outputPowered;
                if (_flameActive)
                {
                    _flameController.ActivateFlame();
                    _flameLoopSource.FadeIn(0.5f, randomizePlayhead: true);
                }
                else
                {
                    _flameController.DeactivateFlame();
                    _flameLoopSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                }
            }

            var inputPowered = powered && !_isOutput;
            if (inputPowered)
            {
                if (IsNearFuel())
                {
                    _fuelInputActive = true;
                    _fuelInputParticles.Play();
                    _fuelInputSource.FadeIn(0.5f, randomizePlayhead: true);
                    foreach (var particle in _activeTransferParticles)
                    {
                        particle.Play();
                    }
                }
                else
                {
                    _fuelInputActive = false;
                    _fuelDryInputSource.FadeIn(0.5f, randomizePlayhead: true);
                }
            }
            else
            {
                _fuelInputParticles.Stop();
                _fuelInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                _fuelDryInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                foreach (var particle in _activeTransferParticles)
                {
                    particle.Stop();
                }
            }
        }
        else if (_currentType == ResourceType.Oxygen)
        {
            var outputPowered = powered && _isOutput && SELocator.GetShipResources()._currentOxygen > 0f;
            _oxygenVolume.SetVolumeActivation(outputPowered);
            if (outputPowered)
            {
                _oxygenOutputParticles.Play();
                _oxygenOutputSource.FadeIn(0.5f, randomizePlayhead: true);
            }
            else
            {
                _oxygenOutputParticles.Stop();
                _oxygenOutputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
            }

            var inputPowered = powered && !_isOutput;
            if (inputPowered)
            {
                if (_oxygenDetector.GetDetectOxygen())
                {
                    _oxygenInputActive = true;
                    _oxygenInputParticles.Play();
                    _oxygenInputSource.FadeIn(0.5f, randomizePlayhead: true);
                }
                else
                {
                    _oxygenInputActive = false;
                    _oxygenDryInputSource.FadeIn(0.5f, randomizePlayhead: true);
                }
            }
            else
            {
                _oxygenInputParticles.Stop();
                _oxygenDryInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                _oxygenInputSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
            }
        }
        else if (_currentType == ResourceType.Water)
        {
            var outputPowered = powered && _isOutput && SELocator.GetShipWaterResource().GetWater() > 0f;
            if (_geyserActive != outputPowered)
            {
                _geyserActive = outputPowered;
                if (_geyserActive)
                {
                    _geyserLoopSource.FadeIn(0.5f, randomizePlayhead: true);
                    _geyserParticles.Play();
                    _geyserVolume.SetFiring(true);
                }
                else
                {
                    _geyserLoopSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                    _geyserParticles.Stop();
                    _geyserVolume.SetFiring(false);
                }

                UpdateGeyserLoopingAudioPosition();
            }

            var inputPowered = powered && !_isOutput;
            if (inputPowered)
            {
                if (IsInWater())
                {
                    _waterInputActive = true;
                    _waterInputSource.FadeIn(0.2f, randomizePlayhead: true);
                    _waterInputParticles.Play();
                }
                else
                {
                    _waterInputActive = false;
                    _waterDryInputSource.FadeIn(0.2f, randomizePlayhead: true);
                }
            }
            else
            {
                _waterInputSource.FadeOut(0.2f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                _waterDryInputSource.FadeOut(0.2f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                _waterInputParticles.Stop();
            }
        }

        if (sendMessage && ShipEnhancements.InMultiplayer)
        {
            foreach (var id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendPumpPowered(id, this, powered);
            }
        }
    }

    private void UpdateBatteryLevel(float battery)
    {
        _signalRenderer.SetMaterialProperty(_batteryPropID, Mathf.Clamp01(battery));
        if (_inSignalRange != battery > 0f)
        {
            _inSignalRange = battery > 0f;
            UpdatePowered();
        }
    }

    private void UpdateError()
    {
        bool flag = false;
        if (_isOutput)
        {
            if ((SELocator.GetShipResources()._currentFuel <= 0f && _currentType == ResourceType.Fuel)
                || (SELocator.GetShipResources()._currentOxygen <= 0f && _currentType == ResourceType.Oxygen)
                || ((bool)addWaterTank.GetProperty() && SELocator.GetShipWaterResource().GetWater() <= 0f && _currentType == ResourceType.Water))
            {
                flag = true;
            }
        }
        else
        {
            if ((_currentType == ResourceType.Fuel && !IsNearFuel())
                || (_currentType == ResourceType.Oxygen && !_oxygenDetector.GetDetectOxygen())
                || ((bool)addWaterTank.GetProperty() && _currentType == ResourceType.Water && !IsInWater()))
            {
                flag = true;
            }
        }

        if (_outputError != flag)
        {
            _outputError = flag;
            _outputDisplayRenderer.SetMaterialProperty(_errorPropID, flag ? 1f : 0f);
        }
    }

    private void UpdateResourceLevel()
    {
        float ratio;
        if (_currentType == ResourceType.Fuel)
        {
            ratio = SELocator.GetShipResources().GetFractionalFuel();
        }
        else if (_currentType == ResourceType.Oxygen)
        {
            ratio = SELocator.GetShipResources().GetFractionalOxygen();
        }
        else
        {
            ratio = SELocator.GetShipWaterResource().GetFractionalWater();
        }
        _typeDisplayRenderer.SetMaterialProperty(_resourceRatioPropID, ratio);
    }

    private void UpdateFuelSources()
    {
        _activeRecoveryPoints.Clear();
        foreach (var particle in _activeTransferParticles)
        {
            Destroy(particle.gameObject);
        }
        _activeTransferParticles.Clear();

        var colliders = Physics.OverlapSphere(_oxygenDetector.transform.position, 10f, OWLayerMask.interactMask);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out SingleInteractionVolume vol))
            {
                var eventDelegate = (MulticastDelegate)typeof(SingleInteractionVolume).GetField("OnPressInteract", 
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(vol);
                if (eventDelegate != null)
                {
                    foreach (var handler in eventDelegate.GetInvocationList())
                    {
                        if (handler.Target is PlayerRecoveryPoint recoveryPoint && recoveryPoint._refuelsPlayer
                            && !recoveryPoint.GetComponentInParent<ShipBody>())
                        {
                            ShipEnhancements.WriteDebugMessage("     Found recovery point: " + recoveryPoint.gameObject.name);
                            _activeRecoveryPoints.Add(recoveryPoint);
                            Transform particle = ShipEnhancements.CreateObject(_fuelTransferParticles.gameObject, recoveryPoint.transform).transform;
                            Quaternion toPumpRot = Quaternion.LookRotation(transform.position - particle.position, transform.up);
                            Quaternion yForward = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                            toPumpRot = toPumpRot * yForward;
                            particle.rotation = toPumpRot;
                            _activeTransferParticles.Add(particle.GetComponent<ParticleSystem>());
                        }
                    }
                }
            }
        }
    }

    private void UpdateWaterVolumes()
    {
        _waterVolumes.Clear();

        Collider[] cols = Physics.OverlapSphere(_fluidDetector.transform.position, 0.6f, OWLayerMask.effectVolumeMask);
        if (cols.Length > 0)
        {
            foreach (var col in cols)
            {
                if (!col.TryGetComponent(out FluidVolume vol) || 
                    vol.GetFluidType() is not FluidVolume.Type.WATER and not FluidVolume.Type.GEYSER)
                {
                    continue;
                }
                
                if (col.TryGetComponent(out OWCustomCollider customCol) &&
                    !customCol.IsPointInCollider(transform.position))
                {
                    return;
                }
                
                _waterVolumes.Add(vol);
            }
        }
    }

    private void OnResourceDepleted(string resource)
    {
        if ((resource == "fuel" && _currentType == ResourceType.Fuel)
            || (resource == "oxygen" && _currentType == ResourceType.Oxygen)
            || (resource == "water" && _currentType == ResourceType.Water))
        {
            UpdatePowered();
        }
    }

    private void OnResourceRestored(string resource)
    {
        if ((resource == "fuel" && _currentType == ResourceType.Fuel)
            || (resource == "oxygen" && _currentType == ResourceType.Oxygen)
            || (resource == "water" && _currentType == ResourceType.Water))
        {
            UpdatePowered();
        }
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        if (!_dropped)
        {
            _dropped = true;
            OnEnable();
        }
        ShipEnhancements.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
        {
            UpdateAttachedBody(parent.GetAttachedOWRigidbody());
        });
        if (!_isOutput)
        {
            var r = Quaternion.LookRotation(new Vector3(0, 1, 0), new Vector3(1, 0, -1));
            var ir = Quaternion.Inverse(r);
            transform.localRotation = transform.localRotation * ir;
            transform.localPosition += transform.localRotation * r * _inputDropOffset;
        }
        transform.localScale = Vector3.one;
        UpdatePowered();
    }

    public override void PickUpItem(Transform holdTranform)
    {
        UpdatePowered(false, sendMessage: false);
        base.PickUpItem(holdTranform);
        if (_dropped)
        {
            OnDisable();
        }
        _dropped = false;
        transform.localPosition = _holdPosition;
        transform.localRotation = Quaternion.Euler(_holdRotation);
        transform.localScale = _holdScale;

        _activeRecoveryPoints.Clear();
        foreach (var particle in _activeTransferParticles)
        {
            Destroy(particle.gameObject);
        }
        _activeTransferParticles.Clear();
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        if (_dropped)
        {
            OnDisable();
        }
        _dropped = false;
        transform.localScale = Vector3.one;
    }

    public bool IsInWater()
    {
        bool flag = false;
        if (_fluidDetector._activeVolumes != null)
        {
            flag = _fluidDetector.InFluidType(FluidVolume.Type.WATER) 
                || _fluidDetector.InFluidType(FluidVolume.Type.GEYSER);
        }
        return flag || _waterVolumes.Count > 0;
    }

    public bool IsNearFuel()
    {
        return _activeRecoveryPoints.Count > 0;
    }

    private void OnEnable()
    {
        if (_fluidDetector == null || !_dropped) return;

        if (_fluidDetector.GetShape())
        {
            _fluidDetector.GetShape().SetActivation(true);
        }
        if (_fluidDetector.GetCollider())
        {
            _fluidDetector.GetCollider().enabled = true;
        }
    }

    private void OnDisable()
    {
        if (_fluidDetector == null) return;

        if (_fluidDetector.GetShape())
        {
            _fluidDetector.GetShape().SetActivation(false);
        }
        if (_fluidDetector.GetCollider())
        {
            _fluidDetector.GetCollider().enabled = false;
        }

        if (_fluidDetector._activeVolumes != null)
        {
            EffectVolume[] volsToRemove = [.. _fluidDetector._activeVolumes];
            foreach (EffectVolume vol in volsToRemove)
            {
                vol._triggerVolume.RemoveObjectFromVolume(_fluidDetector.gameObject);
            }
        }
    }

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
    }

    private void OnShipDestroyed()
    {
        _shipDestroyed = true;
    }

    private void UpdateAttachedBody(OWRigidbody body)
    {
        foreach (EffectVolume vol in _volumes)
        {
            vol.SetAttachedBody(body);
        }
    }

    public override void OnDestroy()
    {
        ShipEnhancements.Instance.OnResourceDepleted -= OnResourceDepleted;
        ShipEnhancements.Instance.OnResourceRestored -= OnResourceRestored;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        GlobalMessenger.RemoveListener("ShipDestroyed", OnShipDestroyed);
    }
}
