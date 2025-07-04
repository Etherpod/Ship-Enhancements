﻿using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ShipEnhancements.Models.Json;

namespace ShipEnhancements;

public class RadioItem : OWItem
{
    public static ItemType ItemType = ShipEnhancements.Instance.RadioType;

    [SerializeField]
    private OWAudioSource _staticSource;
    [SerializeField]
    private OWAudioSource _musicSource;
    [SerializeField]
    private OWAudioSource _oneShotSource;
    [SerializeField]
    private OWAudioSource _codeSource;
    [SerializeField]
    private GameObject _meshParent;
    [SerializeField]
    private RadioCodeDetector _codeDetector;
    [SerializeField]
    private Canvas _codeLabelCanvas;
    [SerializeField]
    private Text[] _codeLabels;
    [SerializeField]
    private Color _deselectColor;
    [SerializeField]
    private Color _selectColor;

    [Header("Radio Effects")]
    [SerializeField]
    private Transform[] _knobTransforms;
    [SerializeField]
    private float _knobRotationDiff;
    [SerializeField]
    private RotateTransform[] _rotateEffects;
    [SerializeField]
    private Transform _powerSwitchTransform;
    [SerializeField]
    private float _switchRotationDiff;
    [SerializeField]
    private Transform _volumeNeedleTransform;
    [SerializeField]
    private float _needleRotationDiff;
    [SerializeField]
    private OWEmissiveRenderer _screenRenderer;
    [SerializeField]
    private AudioClip _onAudio;
    [SerializeField]
    private AudioClip _offAudio;

    private PriorityScreenPrompt _powerPrompt;
    private ScreenPrompt _tunePrompt;
    private PriorityScreenPrompt _volumePrompt;
    private ScreenPrompt _upDownPrompt;
    private ScreenPrompt _leftRightPrompt;
    private ScreenPrompt _leavePrompt;
    private FirstPersonManipulator _cameraManipulator;
    private OWCamera _playerCam;
    private OWAudioSource _playerExternalSource;
    private InputMode _lastInputMode = InputMode.Character;
    private bool _lastFocused = false;
    private bool _playerInteracting = false;
    private bool _connectedToShip = false;
    private bool _playingInDreamWorld = false;

    private int[] _codes = [1, 1, 1, 1];
    private int _currentCodeIndex = 0;
    private bool _powerOn = false;

    private Dictionary<string, AudioClip> _codesToAudio;
    private bool _playingAudio = false;
    private bool _playingCodes = false;
    private float _currentVolume = 0.375f;
    private AudioLowPassFilter _lowPassFilter;
    private AudioHighPassFilter _highPassFilter;
    private AudioReverbFilter _reverbFilter;

    private float _initialKnobRotation;

    private bool _moveNeedle = false;
    private float _needleT = 0f;
    private float _initialNeedleRotation;
    private float _needleStartRot;
    private float _needleTargetRot;
    private float _needleRotateSpeed = 3f;
    private int _volumeSteps = 8;

    private bool _moveSwitch = false;
    private float _switchT = 0f;
    private float _initialSwitchRotation;
    private float _switchStartRot;
    private float _switchTargetRot;
    private float _switchRotateSpeed = 7f;

    private readonly int _minFrequency = 1;
    private readonly int _maxFrequency = 6;
    private AudioClip _connectAudio;
    private AudioClip _disconnectAudio;

    private float _minVolumeDist = 50f;
    private float _maxVolumeDist = 150f;
    private float _minShipVolumeDist = 75f;
    private float _maxShipVolumeDist = 250f;
    private float _minDreamDist = 3f;
    private float _maxDreamDist = 20f;

    private readonly string _powerOnText = "Turn On Radio";
    private readonly string _powerOffText = "Turn Off Radio";

    public override string GetDisplayName()
    {
        return "Radio";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _cameraManipulator = FindObjectOfType<FirstPersonManipulator>();
        _lowPassFilter = _musicSource.GetComponent<AudioLowPassFilter>();
        _highPassFilter = _musicSource.GetComponent<AudioHighPassFilter>();
        _reverbFilter = _musicSource.GetComponent<AudioReverbFilter>();
        _connectAudio = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_Connect.ogg");
        _disconnectAudio = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_Disconnect.ogg");
        InitializeAudioDict();

        GlobalMessenger.AddListener("ExitShip", OnExitShip);
        GlobalMessenger.AddListener("EnterShip", OnEnterShip);
        GlobalMessenger.AddListener("EnterDreamWorld", OnEnterDreamWorld);
        GlobalMessenger.AddListener("ExitDreamWorld", OnExitDreamWorld);
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        _codeDetector.OnChangeActiveZone += OnChangeCodeZone;
    }

    private void InitializeAudioDict()
    {
        TextAsset file = (TextAsset)ShipEnhancements.LoadAsset("Assets/ShipEnhancements/TextAsset/RadioCodes.json");

        var data = JsonConvert.DeserializeObject<List<RadioCodeJson>>(file.text);

        if (data is null)
        {
            ShipEnhancements.LogMessage("Couldn't load RadioCodes.json!", warning: true);
            return;
        }

        _codesToAudio = new();

        foreach (var code in data)
        {
            string num = code.Code.ToString();
            if (_codesToAudio.ContainsKey(num)) continue;

            AudioClip clip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/" + code.Path);
            if (clip != null)
            {
                _codesToAudio.Add(code.Code.ToString(), clip);
            }
        }
    }

    private void Start()
    {
        _playerCam = Locator.GetPlayerCamera();
        _playerExternalSource = Locator.GetPlayerAudioController()._oneShotExternalSource;
        _powerPrompt = new PriorityScreenPrompt(InputLibrary.cancel, _powerOnText, 0, ScreenPrompt.DisplayState.Normal, false);
        _tunePrompt = new ScreenPrompt(InputLibrary.interactSecondary, "Tune Radio", 0, ScreenPrompt.DisplayState.Normal, false);
        _volumePrompt = new PriorityScreenPrompt(InputLibrary.toolOptionLeft, InputLibrary.toolOptionRight, "Adjust Volume", ScreenPrompt.MultiCommandType.POS_NEG, 
            0, ScreenPrompt.DisplayState.Normal, false);
        _leftRightPrompt = new ScreenPrompt(InputLibrary.left, InputLibrary.right, "Change Knob   <CMD>", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _upDownPrompt = new ScreenPrompt(InputLibrary.up, InputLibrary.down, "Change Frequency   <CMD>", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _leavePrompt = new ScreenPrompt(InputLibrary.cancel, "Leave   <CMD>", 0, ScreenPrompt.DisplayState.Normal, false);

        Locator.GetPromptManager().AddScreenPrompt(_powerPrompt, PromptPosition.Center);
        Locator.GetPromptManager().AddScreenPrompt(_tunePrompt, PromptPosition.Center);
        Locator.GetPromptManager().AddScreenPrompt(_volumePrompt, PromptPosition.Center);

        _lowPassFilter.enabled = false;
        _highPassFilter.enabled = !_connectedToShip;
        _reverbFilter.enabled = false;

        SetRadioVolume();

        _codeLabelCanvas.gameObject.SetActive(false);
        _screenRenderer.SetEmissiveScale(0f);
        foreach (RotateTransform rotator in _rotateEffects)
        {
            rotator.enabled = false;
        }
        _initialKnobRotation = _knobTransforms[0].localRotation.z;
    }

    private void Update()
    {
        bool focused = _cameraManipulator.GetFocusedOWItem() == this 
            || (_cameraManipulator.GetFocusedItemSocket()?._acceptableType == ItemType && GetComponentInParent<RadioItemSocket>());
        if (_lastFocused != focused)
        {
            PatchClass.UpdateFocusedItems(focused);
            _lastFocused = focused;
            if (focused)
            {
                SELocator.GetFlightConsoleInteractController().AddInteractible();
            }
            else
            {
                SELocator.GetFlightConsoleInteractController().RemoveInteractible();
            }
        }

        UpdatePromptVisibility();

        if (_playerInteracting)
        {
            if (OWInput.IsNewlyPressed(InputLibrary.cancel))
            {
                Locator.GetPromptManager().RemoveScreenPrompt(_leftRightPrompt);
                Locator.GetPromptManager().RemoveScreenPrompt(_upDownPrompt);
                Locator.GetPromptManager().RemoveScreenPrompt(_leavePrompt);
                Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().BreakLock();
                //GlobalMessenger.FireEvent("ExitSatelliteCameraMode");
                OWInput.ChangeInputMode(_lastInputMode);
                _codeLabels[_currentCodeIndex].color = _deselectColor;
                _playerInteracting = false;

                if (!_playingAudio)
                {
                    var clip = GetSelectedAudio();
                    if (clip != null)
                    {
                        _musicSource.clip = clip;
                        if (_powerOn)
                        {
                            _musicSource.FadeIn(2f, false, false, _currentVolume);
                            _staticSource.FadeOut(10f);
                            if (_playingCodes)
                            {
                                _codeSource.FadeOut(2f);
                            }
                        }
                        _playingAudio = true;
                    }
                }

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendRadioCancelTuning(id, this);
                    }
                }
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.right, InputMode.All) 
                || OWInput.IsNewlyPressed(InputLibrary.right2, InputMode.All))
            {
                _codeLabels[_currentCodeIndex].color = _deselectColor;
                _currentCodeIndex = (_currentCodeIndex + 1 + _codes.Length) % _codes.Length;
                _codeLabels[_currentCodeIndex].color = _selectColor;

                _playerExternalSource.PlayOneShot(AudioType.Menu_UpDown, 0.5f);
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.left, InputMode.All)
                || OWInput.IsNewlyPressed(InputLibrary.left2, InputMode.All))
            {
                _codeLabels[_currentCodeIndex].color = _deselectColor;
                _currentCodeIndex = (_currentCodeIndex - 1 + _codes.Length) % _codes.Length;
                _codeLabels[_currentCodeIndex].color = _selectColor;

                _playerExternalSource.PlayOneShot(AudioType.Menu_UpDown, 0.5f);
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.up, InputMode.All)
                || OWInput.IsNewlyPressed(InputLibrary.up2, InputMode.All))
            {
                int range = _maxFrequency - _minFrequency + 1;
                _codes[_currentCodeIndex] = (_codes[_currentCodeIndex] + 1 - _minFrequency + range)
                    % range + _minFrequency;
                _codeLabels[_currentCodeIndex].text = _codes[_currentCodeIndex].ToString();

                var euler = _knobTransforms[_currentCodeIndex].localEulerAngles;
                euler.z = Mathf.Lerp(_initialKnobRotation, _initialKnobRotation + _knobRotationDiff, (_codes[_currentCodeIndex] - 1) / 5f);
                _knobTransforms[_currentCodeIndex].localRotation = Quaternion.Euler(euler);

                if (_playingAudio)
                {
                    if (_powerOn)
                    {
                        _musicSource.FadeOut(0.5f);
                        _staticSource.FadeIn(0.5f, false, false, _currentVolume);
                        if (_playingCodes)
                        {
                            _codeSource.FadeIn(0.5f, false, false, _currentVolume);
                        }
                    }
                    else
                    {
                        _musicSource.time = 0;
                    }
                    _playingAudio = false;
                }

                _playerExternalSource.PlayOneShot(AudioType.Menu_LeftRight, 0.5f);

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendRadioCodes(id, this, _codes);
                    }
                }
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.down, InputMode.All)
                || OWInput.IsNewlyPressed(InputLibrary.down2, InputMode.All))
            {
                int range = _maxFrequency - _minFrequency + 1;
                _codes[_currentCodeIndex] = (_codes[_currentCodeIndex] - 1 - _minFrequency + range) 
                    % range + _minFrequency;
                _codeLabels[_currentCodeIndex].text = _codes[_currentCodeIndex].ToString();

                var euler = _knobTransforms[_currentCodeIndex].localEulerAngles;
                euler.z = Mathf.Lerp(_initialKnobRotation, _initialKnobRotation + _knobRotationDiff, (_codes[_currentCodeIndex] - 1) / 5f);
                _knobTransforms[_currentCodeIndex].localRotation = Quaternion.Euler(euler);

                if (_playingAudio)
                {
                    if (_powerOn)
                    {
                        _musicSource.FadeOut(0.5f);
                        _staticSource.FadeIn(0.5f, false, false, _currentVolume);
                        if (_playingCodes)
                        {
                            _codeSource.FadeIn(0.5f, false, false, _currentVolume);
                        }
                    }
                    else
                    {
                        _musicSource.time = 0;
                    }
                    _playingAudio = false;
                }

                _playerExternalSource.PlayOneShot(AudioType.Menu_LeftRight, 0.5f);

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendRadioCodes(id, this, _codes);
                    }
                }
            }
        }
        else if (focused)
        {
            if (OWInput.IsNewlyPressed(InputLibrary.cancel))
            {
                _powerOn = !_powerOn;
                if (_powerOn)
                {
                    if (!_playingAudio)
                    {
                        //_staticSource.SetLocalVolume(_currentVolume);
                        //_staticSource.Play();
                        _staticSource.FadeIn(0f, false, false, _currentVolume);
                        if (_playingCodes)
                        {
                            //_codeSource.SetLocalVolume(_currentVolume);
                            //_codeSource.Play();
                            _codeSource.FadeIn(0f, false, false, _currentVolume);
                        }
                    }
                    else
                    {
                        //_musicSource.SetLocalVolume(_currentVolume);
                        //_musicSource.Play();
                        _musicSource.FadeIn(0f, false, false, _currentVolume);
                    }
                    _powerPrompt.SetText(_powerOffText);

                    _codeLabelCanvas.gameObject.SetActive(true);
                    _screenRenderer.SetEmissiveScale(1f);
                    foreach (RotateTransform rotator in _rotateEffects)
                    {
                        rotator.enabled = true;
                    }

                    _switchStartRot = _powerSwitchTransform.localEulerAngles.y;
                    _switchTargetRot = _initialSwitchRotation + _switchRotationDiff;
                    _switchT = 0f;
                    _moveSwitch = true;

                    _needleStartRot = _volumeNeedleTransform.localEulerAngles.y;
                    _needleTargetRot = Mathf.Lerp(_initialNeedleRotation, _initialNeedleRotation + _needleRotationDiff, _currentVolume);
                    _needleT = 0f;
                    _moveNeedle = true;

                    _oneShotSource.PlayOneShot(_onAudio, 0.2f);
                }
                else
                {
                    _staticSource.FadeOut(0f);
                    if (_playingCodes)
                    {
                        //_codeSource.Stop();
                        _codeSource.FadeOut(0f);
                    }
                    if (_playingAudio)
                    {
                        //_musicSource.Pause();
                        _musicSource.FadeOut(0f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                    }
                    _powerPrompt.SetText(_powerOnText);

                    _codeLabelCanvas.gameObject.SetActive(false);
                    _screenRenderer.SetEmissiveScale(0f);
                    foreach (RotateTransform rotator in _rotateEffects)
                    {
                        rotator.enabled = false;
                    }

                    _switchStartRot = _powerSwitchTransform.localEulerAngles.y;
                    _switchTargetRot = _initialSwitchRotation;
                    _switchT = 0f;
                    _moveSwitch = true;

                    _needleStartRot = _volumeNeedleTransform.localEulerAngles.y;
                    _needleTargetRot = _initialNeedleRotation;
                    _needleT = 0f;
                    _moveNeedle = true;

                    _oneShotSource.PlayOneShot(_offAudio, 0.2f);
                }

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendRadioPower(id, this, _powerOn);
                    }
                }
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary))
            {
                Locator.GetToolModeSwapper().UnequipTool();
                Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().LockOn(transform, Vector3.zero);
                //GlobalMessenger.FireEvent("EnterSatelliteCameraMode");
                _lastInputMode = OWInput.GetInputMode();
                OWInput.ChangeInputMode(InputMode.SatelliteCam);
                Locator.GetPromptManager().AddScreenPrompt(_upDownPrompt, PromptPosition.UpperRight, true);
                Locator.GetPromptManager().AddScreenPrompt(_leftRightPrompt, PromptPosition.UpperRight, true);
                Locator.GetPromptManager().AddScreenPrompt(_leavePrompt, PromptPosition.UpperRight, true);
                _codeLabels[_currentCodeIndex].color = _selectColor;
                _playerInteracting = true;
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionRight))
            {
                _currentVolume = Mathf.Min(_currentVolume + 1f / _volumeSteps, 1f);
                SetRadioVolume();

                if (_powerOn)
                {
                    _needleStartRot = _volumeNeedleTransform.localEulerAngles.y;
                    _needleTargetRot = Mathf.Lerp(_initialNeedleRotation, _initialNeedleRotation + _needleRotationDiff, _currentVolume);
                    _needleT = 0f;
                    _moveNeedle = true;
                }

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendRadioVolume(id, this, _currentVolume);
                    }
                }
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionLeft))
            {
                _currentVolume = Mathf.Max(_currentVolume - 1f / _volumeSteps, 0f);
                SetRadioVolume();

                if (_powerOn)
                {
                    _needleStartRot = _volumeNeedleTransform.localEulerAngles.y;
                    _needleTargetRot = Mathf.Lerp(_initialNeedleRotation, _initialNeedleRotation + _needleRotationDiff, _currentVolume);
                    _needleT = 0f;
                    _moveNeedle = true;
                }

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendRadioVolume(id, this, _currentVolume);
                    }
                }
            }
        }

        if (_moveNeedle)
        {
            Vector3 euler = _volumeNeedleTransform.localEulerAngles;
            euler.y = Mathf.Lerp(_needleStartRot, _needleTargetRot, _needleT);
            _volumeNeedleTransform.localRotation = Quaternion.Euler(euler);
            _needleT += Time.deltaTime * _needleRotateSpeed;
            if (_needleT >= 1f)
            {
                _moveNeedle = false;
            }
        }
        if (_moveSwitch)
        {
            Vector3 euler = _powerSwitchTransform.localEulerAngles;
            euler.y = Mathf.SmoothStep(_switchStartRot, _switchTargetRot, _switchT);
            _powerSwitchTransform.localRotation = Quaternion.Euler(euler);
            _switchT += Time.deltaTime * _switchRotateSpeed;
            if (_switchT >= 1f)
            {
                _moveSwitch = false;
            }
        }

        if (_playingInDreamWorld)
        {
            Vector3 sleepPosition = Locator.GetDreamWorldController().GetDreamCampfire().transform.position
                + Locator.GetDreamWorldController()._relativeSleepLocation.localPosition;
            float distSqr = (sleepPosition - transform.position).sqrMagnitude;
            float lerp = Mathf.InverseLerp(_maxDreamDist * _maxDreamDist, _minDreamDist * _minDreamDist, distSqr);
            _musicSource.SetMaxVolume(lerp);
        }
    }

    private void SetRadioVolume()
    {
        if (_connectedToShip)
        {
            _musicSource.maxDistance = Mathf.Lerp(_minShipVolumeDist, _maxShipVolumeDist, _currentVolume);
        }
        else
        {
            _musicSource.maxDistance = Mathf.Lerp(_minVolumeDist, _maxVolumeDist, _currentVolume);
        }

        if (!_powerOn) return;
        if (_playingAudio)
        {
            _musicSource.FadeTo(_currentVolume, 0.2f);
        }
        else
        {
            _staticSource.FadeTo(_currentVolume, 0.2f);
            _codeSource.FadeTo(_currentVolume, 0.2f);
        }
    }

    private void UpdatePromptVisibility()
    {
        bool flag = _lastFocused && _playerCam.enabled && OWInput.IsInputMode(InputMode.Character | InputMode.ShipCockpit);
        if (flag != _powerPrompt.IsVisible())
        {
            _powerPrompt.SetVisibility(flag);
        }
        if (flag != _tunePrompt.IsVisible())
        {
            _tunePrompt.SetVisibility(flag);
        }
        if (flag != _volumePrompt.IsVisible())
        {
            _volumePrompt.SetVisibility(flag);
        }
    }

    private AudioClip GetSelectedAudio()
    {
        string result = "";
        for (int i = 0; i < _codes.Length; i++)
        {
            result += _codes[i].ToString();
        }
        if (_codesToAudio.ContainsKey(result))
        {
            return _codesToAudio[result];
        }

        return null;
    }

    public void SetRadioPowerRemote(bool powered)
    {
        _powerOn = powered;
        if (_powerOn)
        {
            if (!_playingAudio)
            {
                _staticSource.FadeIn(0f, false, false, _currentVolume);
                if (_playingCodes)
                {
                    _codeSource.FadeIn(0f, false, false, _currentVolume);
                }
            }
            else
            {
                _musicSource.FadeIn(0f, false, false, _currentVolume);
            }
            _powerPrompt.SetText(_powerOffText);

            _codeLabelCanvas.gameObject.SetActive(true);
            _screenRenderer.SetEmissiveScale(1f);
            foreach (RotateTransform rotator in _rotateEffects)
            {
                rotator.enabled = true;
            }

            _switchStartRot = _powerSwitchTransform.localEulerAngles.y;
            _switchTargetRot = _initialSwitchRotation + _switchRotationDiff;
            _switchT = 0f;
            _moveSwitch = true;

            _needleStartRot = _volumeNeedleTransform.localEulerAngles.y;
            _needleTargetRot = Mathf.Lerp(_initialNeedleRotation, _initialNeedleRotation + _needleRotationDiff, _currentVolume);
            _needleT = 0f;
            _moveNeedle = true;
        }
        else
        {
            _staticSource.FadeOut(0f);
            if (_playingCodes)
            {
                _codeSource.FadeOut(0f);
            }
            if (_playingAudio)
            {
                _musicSource.FadeOut(0f, OWAudioSource.FadeOutCompleteAction.PAUSE);
            }
            _powerPrompt.SetText(_powerOnText);

            _codeLabelCanvas.gameObject.SetActive(false);
            _screenRenderer.SetEmissiveScale(0f);
            foreach (RotateTransform rotator in _rotateEffects)
            {
                rotator.enabled = false;
            }

            _switchStartRot = _powerSwitchTransform.localEulerAngles.y;
            _switchTargetRot = _initialSwitchRotation;
            _switchT = 0f;
            _moveSwitch = true;

            _needleStartRot = _volumeNeedleTransform.localEulerAngles.y;
            _needleTargetRot = _initialNeedleRotation;
            _needleT = 0f;
            _moveNeedle = true;
        }
    }

    public void SetRadioCodesRemote(int[] newCodes)
    {
        for (int i = 0; i < _codes.Length; i++)
        {
            _codes[i] = newCodes[i];
            _codeLabels[i].text = _codes[i].ToString();

            var euler = _knobTransforms[i].localEulerAngles;
            euler.y = Mathf.Lerp(_initialKnobRotation, _initialKnobRotation + _knobRotationDiff, (_codes[i] - 1) / 5f);
            _knobTransforms[i].localRotation = Quaternion.Euler(euler);
        }

        if (_playingAudio)
        {
            if (_powerOn)
            {
                _musicSource.FadeOut(0.5f);
                _staticSource.FadeIn(0.5f, false, false, _currentVolume);
                if (_playingCodes)
                {
                    _codeSource.FadeIn(0.5f, false, false, _currentVolume);
                }
            }
            else
            {
                _musicSource.time = 0;
            }
            _playingAudio = false;
        }
    }

    public void CancelTuningRemote()
    {
        if (!_playingAudio)
        {
            var clip = GetSelectedAudio();
            if (clip != null)
            {
                _musicSource.clip = clip;
                if (_powerOn)
                {
                    _musicSource.FadeIn(2f, false, false, _currentVolume);
                    _staticSource.FadeOut(10f);
                    if (_playingCodes)
                    {
                        _codeSource.FadeOut(2f);
                    }
                }
                _playingAudio = true;
            }
        }
    }

    public void ChangeVolumeRemote(float volume)
    {
        _currentVolume = volume;
        SetRadioVolume();

        if (_powerOn)
        {
            _needleStartRot = _volumeNeedleTransform.localEulerAngles.y;
            _needleTargetRot = Mathf.Lerp(_initialNeedleRotation, _initialNeedleRotation + _needleRotationDiff, _currentVolume);
            _needleT = 0f;
            _moveNeedle = true;
        }
    }

    public float GetNoiseRadius()
    {
        if (!_powerOn || !_playingAudio) return 0f;

        float lerp = 0f;
        if (_connectedToShip)
        {
            lerp = _currentVolume;
        }
        else
        {
            lerp = _currentVolume * 0.75f;
        }

        return Mathf.Lerp(0f, 450f, lerp);
    }

    public bool ShouldOverrideTravelMusic()
    {
        return _powerOn && _playingAudio && _connectedToShip;
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);

        if (socketTransform.GetComponent<RadioItemSocket>() && !SELocator.GetShipDamageController().IsSystemFailed())
        {
            _musicSource.spatialBlend = 0f;
            _musicSource.spread = 0f;
            _highPassFilter.enabled = false;
            _oneShotSource.PlayOneShot(_connectAudio, 1f);
            _connectedToShip = true;

            SetRadioVolume();
        }

        _meshParent.transform.localScale = Vector3.one;
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);

        if (_connectedToShip)
        {
            _musicSource.spatialBlend = 1f;
            _musicSource.spread = 60f;
            _highPassFilter.enabled = true;
            _oneShotSource.PlayOneShot(_disconnectAudio, 1f);
            _connectedToShip = false;

            SetRadioVolume();
        }

        _meshParent.transform.localScale = Vector3.one * 0.6f;
        transform.localPosition = new Vector3(0f, -0.1f, 0f);
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        _meshParent.transform.localScale = Vector3.one;
    }

    private void OnExitShip()
    {
        _musicSource.spatialBlend = 1f;
        _musicSource.spread = 60f;
    }

    private void OnEnterShip()
    {
        if (_connectedToShip)
        {
            _musicSource.spatialBlend = 0f;
            _musicSource.spread = 0f;
        }
    }

    private void OnEnterDreamWorld()
    {
        if (!PlayerState.IsResurrected())
        {
            ShipEnhancements.WriteDebugMessage("Start playing in DW");
            _playingInDreamWorld = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.spread = 0f;
            _lowPassFilter.enabled = true;
            _reverbFilter.enabled = true;
        }
    }

    private void OnExitDreamWorld()
    {
        if (_playingInDreamWorld)
        {
            ShipEnhancements.WriteDebugMessage("Stop playing in DW");
            _playingInDreamWorld = false;
            _musicSource.spatialBlend = _connectedToShip && PlayerState.IsInsideShip() ? 0f : 1f;
            _musicSource.spread = _connectedToShip && PlayerState.IsInsideShip() ? 0f : 60f;
            _musicSource.SetMaxVolume(1f);
            _lowPassFilter.enabled = false;
            _reverbFilter.enabled = false;
        }
    }

    private void OnShipSystemFailure()
    {
        if (_connectedToShip)
        {
            _musicSource.spatialBlend = 1f;
            _musicSource.spread = 60f;
            _highPassFilter.enabled = true;
            //_oneShotSource.PlayOneShot(_disconnectAudio, 1f);
            _connectedToShip = false;
        }
    }

    private void OnChangeCodeZone(RadioCodeZone zone)
    {
        if (zone != null && !_playingCodes)
        {
            _codeSource.clip = zone.GetAudioCode();
            if (_powerOn && !_playingAudio)
            {
                _codeSource.time = 0f;
                //_codeSource.SetLocalVolume(_currentVolume);
                //_codeSource.Play();
                _codeSource.FadeIn(0f, false, false, _currentVolume);
            }
            _playingCodes = true;
        }
        else if (_playingCodes)
        {
            _codeSource.clip = null;
            if (_codeSource.isPlaying)
            {
                _codeSource.Stop();
            }
            _playingCodes = false;
        }
    }

    private void OnDisable()
    {
        if (_lastFocused)
        {
            SELocator.GetFlightConsoleInteractController().RemoveInteractible();
            _lastFocused = false;
        }
    }

    private void OnEnable()
    {
        if (!_powerOn) return;

        if (_playingAudio)
        {
            _musicSource.Play();
        }
        else
        {
            _staticSource.Play();
            if (_playingCodes)
            {
                _codeSource.Play();
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
        GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
        GlobalMessenger.RemoveListener("EnterDreamWorld", OnEnterDreamWorld);
        GlobalMessenger.RemoveListener("ExitDreamWorld", OnExitDreamWorld);
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        _codeDetector.OnChangeActiveZone -= OnChangeCodeZone;
    }
}
