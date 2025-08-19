using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    [SerializeField]
    private GameObject[] _socketObjects = [];

    [Space]
    [SerializeField]
    private PumpFlameController _flameController;
    [SerializeField]
    private OWAudioSource _flameLoopSource;
    [SerializeField]
    private float _thrusterStrength;

    [Space]
    [SerializeField]
    private OWRenderer _signalRenderer;
    [SerializeField]
    private float _maxShipDistance = 3000f;
    [SerializeField]
    private OWAudioSource _alarmSource;

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
    private readonly int _emissionPropID = Shader.PropertyToID("_Emission");
    private bool _inSignalRange;

    private ScreenPrompt _switchTypePrompt;
    private ScreenPrompt _switchModePrompt;
    private ScreenPrompt _powerPrompt;

    private ResourceType _currentType = ResourceType.Fuel;
    private int _currentTypeIndex = 0;
    private bool _isOutput = true;
    private bool _powered = false;

    private bool _geyserActive = false;
    private bool _flameActive = false;
    private bool _oxygenInputActive = false;
    private bool _waterInputActive = false;

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
        _switchTypePrompt = new ScreenPrompt(InputLibrary.toolOptionLeft, InputLibrary.toolOptionRight, "Switch Type (Fuel)", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _switchModePrompt = new ScreenPrompt(InputLibrary.toolOptionUp, InputLibrary.toolOptionDown, "Switch Mode (Output)", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _powerPrompt = new ScreenPrompt(InputLibrary.interactSecondary, "Turn On", 0, ScreenPrompt.DisplayState.Normal, false);

        ShipEnhancements.Instance.OnResourceDepleted += OnResourceDepleted;
        ShipEnhancements.Instance.OnResourceRestored += OnResourceRestored;

        List<ParticleSystem> systems = _particleSystems.ToList();
        systems.Clear();
        _particleSystems = systems.ToArray();

        foreach (var obj in _socketObjects)
        {
            obj.SetActive(false);
        }
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

        if (_lastFocused)
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
                UpdateType(nextType);
            }

            bool pressedDown = OWInput.IsNewlyPressed(InputLibrary.toolOptionDown, InputMode.Character);
            if (pressedDown || OWInput.IsNewlyPressed(InputLibrary.toolOptionUp, InputMode.Character))
            {
                _playerExternalSource.PlayOneShot(AudioType.Menu_LeftRight, 0.5f);
                _isOutput = !_isOutput;
                _switchModePrompt.SetText($"Switch Mode ({(_isOutput ? "Output" : "Input")})");
                UpdatePowered();
            }

            if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary, InputMode.Character))
            {
                _powered = !_powered;
                _powerPrompt.SetText(_powered ? "Turn Off" : "Turn On");
                UpdatePowered();
            }
        }

        float distSqr = (SELocator.GetShipTransform().position - transform.position).sqrMagnitude;
        float lerp = Mathf.InverseLerp(_maxShipDistance * _maxShipDistance, 50f * 50f, distSqr);
        UpdateBatteryLevel(lerp);

        if (_powered && _inSignalRange && _dropped)
        {
            if (_currentType == ResourceType.Fuel)
            {
                if (_isOutput)
                {
                    SELocator.GetShipResources().DrainFuel(1f * Time.deltaTime);
                }
            }
            else if (_currentType == ResourceType.Oxygen)
            {
                if (_isOutput)
                {
                    SELocator.GetShipResources().DrainOxygen(0.26f * Time.deltaTime);
                }
                else if (_oxygenDetector.GetDetectOxygen())
                {
                    if (!_oxygenInputActive)
                    {
                        _oxygenInputActive = true;
                        _oxygenDryInputSource.FadeOut(0.5f);
                        _oxygenInputSource.FadeIn(0.5f);
                        _oxygenInputParticles.Play();
                    }

                    SELocator.GetShipResources().DrainOxygen(-2f * Time.deltaTime);
                }
                else if (_oxygenInputActive)
                {
                    _oxygenInputActive = false;
                    _oxygenDryInputSource.FadeIn(0.5f);
                    _oxygenInputSource.FadeOut(0.5f);
                    _oxygenInputParticles.Stop();
                }
            }
            else if (_currentType == ResourceType.Water)
            {
                if (_isOutput)
                {
                    SELocator.GetShipWaterResource().DrainWater(1.5f * Time.deltaTime);
                }
                else if (IsInWater())
                {
                    if (!_waterInputActive)
                    {
                        _waterInputActive = true;
                        _waterDryInputSource.FadeOut(0.2f);
                        _waterInputSource.FadeIn(0.2f);
                        _waterInputParticles.Play();
                    }

                    SELocator.GetShipWaterResource().DrainWater(-5f * Time.deltaTime);
                }
                else if (_waterInputActive)
                {
                    _waterInputActive = false;
                    _waterDryInputSource.FadeIn(0.2f);
                    _waterInputSource.FadeOut(0.2f);
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
        if (_flameActive)
        {
            var body = gameObject.GetAttachedOWRigidbody();
            var toCom = transform.position - body.GetWorldCenterOfMass();
            var thrustDir = -transform.up * _thrusterStrength;

            body.AddForce(thrustDir);

            var torqueStrength = Vector3.ProjectOnPlane(toCom, thrustDir).magnitude;
            var torqueVec = Vector3.Cross(toCom, thrustDir).normalized;
            body.AddTorque(torqueVec * torqueStrength);
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

    private void UpdateType(ResourceType nextType)
    {
        if (!_powered || !_inSignalRange || nextType == _currentType)
        {
            _currentType = nextType;
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
                    _flameLoopSource.FadeOut(0.5f);
                }
            }
            else
            {

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
                    _flameLoopSource.FadeIn(0.5f);
                }
            }
            else if (!_isOutput)
            {

            }
        }

        if (_currentType == ResourceType.Oxygen)
        {
            if (_isOutput)
            {
                _oxygenVolume.SetVolumeActivation(false);
                _oxygenOutputParticles.Stop();
                _oxygenOutputSource.FadeOut(0.5f);
            }
            else
            {
                _oxygenInputParticles.Stop();
                _oxygenDryInputSource.FadeOut(0.5f);
                _oxygenInputSource.FadeOut(0.5f);
            }
        }
        else if (nextType == ResourceType.Oxygen)
        {
            if (_isOutput && SELocator.GetShipResources()._currentOxygen > 0f)
            {
                _oxygenVolume.SetVolumeActivation(true);
                _oxygenOutputParticles.Play();
                _oxygenOutputSource.FadeIn(0.5f);
            }
            else if (!_isOutput)
            {
                if (_oxygenDetector.GetDetectOxygen())
                {
                    _oxygenInputActive = true;
                    _oxygenInputParticles.Play();
                    _oxygenInputSource.FadeIn(0.5f);
                }
                else
                {
                    _oxygenInputActive = false;
                    _oxygenDryInputSource.FadeIn(0.5f);
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
                    _geyserLoopSource.FadeOut(0.5f);
                    _geyserParticles.Stop();
                    _geyserVolume.SetFiring(false);
                }
            }
            else
            {
                _waterInputSource.FadeOut(0.2f);
                _waterDryInputSource.FadeOut(0.2f);
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
                    _geyserLoopSource.FadeIn(0.5f);
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
                    _waterInputSource.FadeIn(0.2f);
                    _waterInputParticles.Play();
                }
                else
                {
                    _waterInputActive = false;
                    _waterDryInputSource.FadeIn(0.2f);
                }
            }
        }

        _currentType = nextType;
    }

    private void UpdatePowered()
    {
        UpdatePowered(_powered && _inSignalRange && _dropped);
    }

    private void UpdatePowered(bool powered)
    {
        if (_powered && _dropped && !_inSignalRange && !_alarmSource.isPlaying)
        {
            _alarmSource.Play();
        }
        else if (_alarmSource.isPlaying)
        {
            _alarmSource.Stop();
        }

        if (_currentType == ResourceType.Fuel)
        {
            var outputPowered = powered && _isOutput && SELocator.GetShipResources()._currentFuel > 0f;
            if (_flameActive != outputPowered)
            {
                _flameActive = outputPowered;
                if (_flameActive)
                {
                    _flameController.ActivateFlame();
                    _flameLoopSource.FadeIn(0.5f);
                }
                else
                {
                    _flameController.DeactivateFlame();
                    _flameLoopSource.FadeOut(0.5f);
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
                _oxygenOutputSource.FadeIn(0.5f);
            }
            else
            {
                _oxygenOutputParticles.Stop();
                _oxygenOutputSource.FadeOut(0.5f);
            }

            var inputPowered = powered && !_isOutput;
            if (inputPowered)
            {
                if (_oxygenDetector.GetDetectOxygen())
                {
                    _oxygenInputActive = true;
                    _oxygenInputParticles.Play();
                    _oxygenInputSource.FadeIn(0.5f);
                }
                else
                {
                    _oxygenInputActive = false;
                    _oxygenDryInputSource.FadeIn(0.5f);
                }
            }
            else
            {
                _oxygenInputParticles.Stop();
                _oxygenDryInputSource.FadeOut(0.5f);
                _oxygenInputSource.FadeOut(0.5f);
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
                    _geyserLoopSource.FadeIn(0.5f);
                    _geyserParticles.Play();
                    _geyserVolume.SetFiring(true);
                }
                else
                {
                    _geyserLoopSource.FadeOut(0.5f);
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
                    _waterInputSource.FadeIn(0.2f);
                    _waterInputParticles.Play();
                }
                else
                {
                    _waterInputActive = false;
                    _waterDryInputSource.FadeIn(0.2f);
                }
            }
            else
            {
                _waterInputSource.FadeOut(0.2f);
                _waterDryInputSource.FadeOut(0.2f);
                _waterInputParticles.Stop();
            }
        }
    }

    public void UpdateBatteryLevel(float battery)
    {
        _signalRenderer.SetMaterialProperty(_batteryPropID, Mathf.Clamp01(battery));
        if (_inSignalRange != battery > 0f)
        {
            _inSignalRange = battery > 0f;
            UpdatePowered();

            if (_inSignalRange)
            {
                _signalRenderer.SetMaterialProperty(_emissionPropID, 1f);
            }
        }
        if (battery == 0f)
        {
            bool light = Time.timeSinceLevelLoad * 2f % 2f < 1f;
            _signalRenderer.SetMaterialProperty(_emissionPropID, light ? 1f : 0f);
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
        UpdateAttachedBody(parent.GetAttachedOWRigidbody());
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
        UpdatePowered(false);
        base.PickUpItem(holdTranform);
        if (_dropped)
        {
            OnDisable();
        }
        _dropped = false;
        transform.localPosition = _holdPosition;
        transform.localRotation = Quaternion.Euler(_holdRotation);
        transform.localScale = _holdScale;
        foreach (var obj in _socketObjects)
        {
            obj.SetActive(false);
        }
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
        foreach (var obj in _socketObjects)
        {
            obj.SetActive(true);
        }
    }

    public bool IsInWater()
    {
        if (_fluidDetector._activeVolumes == null) return false;
        return _fluidDetector.InFluidType(FluidVolume.Type.WATER) || _fluidDetector.InFluidType(FluidVolume.Type.GEYSER);
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
    }
}
