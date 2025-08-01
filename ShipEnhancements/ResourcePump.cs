using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    [Space]
    [SerializeField]
    private OxygenVolume _oxygenVolume;
    [SerializeField]
    private OWAudioSource _oxygenLoopSource;

    [Space]
    [SerializeField]
    private ParticleSystem _geyserParticles;
    [SerializeField]
    private GeyserFluidVolume _geyserVolume;
    [SerializeField]
    private OWAudioSource _geyserLoopSource;

    private EffectVolume[] _volumes;
    private FirstPersonManipulator _cameraManipulator;
    private OWAudioSource _playerExternalSource;
    private OWCamera _playerCam;
    private bool _lastFocused = false;
    private bool _dropped = false;

    private readonly int _batteryPropID = Shader.PropertyToID("_Battery");
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
            }

            if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary, InputMode.Character))
            {
                _powered = !_powered;
                _powerPrompt.SetText(_powered ? "Turn Off" : "Turn On");
                UpdatePowered(_powered && _inSignalRange && _dropped);
            }
        }

        float distSqr = (SELocator.GetShipTransform().position - SELocator.GetPlayerBody().transform.position).sqrMagnitude;
        float lerp = Mathf.InverseLerp(_maxShipDistance * _maxShipDistance, 50f * 50f, distSqr);
        UpdateBatteryLevel(lerp);
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

    private void UpdateType(ResourceType type)
    {
        if (!_powered || !_inSignalRange || type == _currentType)
        {
            _currentType = type;
            return;
        }

        if (_currentType == ResourceType.Fuel)
        {
            _flameActive = false;
            _flameController.DeactivateFlame();
            _flameLoopSource.FadeOut(0.5f);
        }
        else if (type == ResourceType.Fuel)
        {
            _flameActive = true;
            _flameController.ActivateFlame();
            _flameLoopSource.FadeIn(0.5f);
        }

        if (_currentType == ResourceType.Oxygen)
        {
            _oxygenVolume.SetVolumeActivation(false);
            _oxygenLoopSource.FadeOut(0.5f);
        }
        else if (type == ResourceType.Oxygen)
        {
            _oxygenVolume.SetVolumeActivation(true);
            _oxygenLoopSource.FadeIn(0.5f);
        }

        if (_currentType == ResourceType.Water)
        {
            _geyserActive = false;
            _geyserLoopSource.FadeOut(0.5f);
            _geyserParticles.Stop();
            _geyserVolume.SetFiring(false);
        }
        else if (type == ResourceType.Water)
        {
            _geyserActive = true;
            _geyserLoopSource.FadeIn(0.5f);
            _geyserParticles.Play();
            _geyserVolume.SetFiring(true);
            UpdateGeyserLoopingAudioPosition();
        }

        _currentType = type;
    }
    
    private void UpdatePowered(bool powered)
    {
        if (_currentType == ResourceType.Fuel)
        {
            _flameActive = powered;
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
        else if (_currentType == ResourceType.Oxygen)
        {
            _oxygenVolume.SetVolumeActivation(powered);
            if (powered)
            {
                _oxygenLoopSource.FadeIn(0.5f);
            }
            else
            {
                _oxygenLoopSource.FadeOut(0.5f);
            }
        }
        else if (_currentType == ResourceType.Water)
        {
            if (_geyserActive != powered)
            {
                _geyserActive = powered;
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
        }
    }

    public void UpdateBatteryLevel(float battery)
    {
        _signalRenderer.SetMaterialProperty(_batteryPropID, Mathf.Clamp01(battery));
        if (_inSignalRange != battery > 0f)
        {
            _inSignalRange = battery > 0f;
            UpdatePowered(_powered && _inSignalRange && _dropped);
        }
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        _dropped = true;
        UpdateAttachedBody(parent.GetAttachedOWRigidbody());
        transform.localScale = Vector3.one;
        UpdatePowered(_powered && _inSignalRange);
    }

    public override void PickUpItem(Transform holdTranform)
    {
        UpdatePowered(false);
        base.PickUpItem(holdTranform);
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
        _dropped = false;
        transform.localScale = Vector3.one;
        foreach (var obj in _socketObjects)
        {
            obj.SetActive(true);
        }
    }

    private void UpdateAttachedBody(OWRigidbody body)
    {
        foreach (EffectVolume vol in _volumes)
        {
            vol.SetAttachedBody(body);
        }
    }
}
