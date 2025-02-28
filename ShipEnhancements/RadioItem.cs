using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements;

public class RadioItem : OWItem
{
    public static ItemType ItemType = ShipEnhancements.Instance.RadioType;

    [SerializeField]
    private OWAudioSource _staticSource;
    [SerializeField]
    private OWAudioSource _musicSource;
    [SerializeField]
    private Text[] _codeLabels;

    private PriorityScreenPrompt _powerPrompt;
    private ScreenPrompt _tunePrompt;
    private PriorityScreenPrompt _volumePrompt;
    private ScreenPrompt _upDownPrompt;
    private ScreenPrompt _leftRightPrompt;
    private ScreenPrompt _leavePrompt;
    private FirstPersonManipulator _cameraManipulator;
    private OWCamera _playerCam;
    private bool _lastFocused = false;
    private bool _playerInteracting = false;
    private bool _socketed = false;

    private int[] _codes = new int[4];
    private int _currentCodeIndex = 0;
    private bool _powerOn = false;

    private Dictionary<string, AudioClip> _codesToAudio;
    private bool _playingAudio = false;
    private float _currentVolume = 0.5f;

    private readonly int _minFrequency = 1;
    private readonly int _maxFrequency = 6;
    private Color _deselectColor = Color.black;
    private Color _selectColor = Color.blue;

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
        InitializeAudioDict();

        GlobalMessenger.AddListener("ExitShip", OnExitShip);
        GlobalMessenger.AddListener("EnterShip", OnEnterShip);
    }

    private void InitializeAudioDict()
    {
        _codesToAudio = new()
        {
            { "2441", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_LostSignal.mp3") },
            { "2662", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_Chadvelers.mp3") },
            { "1513", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_NoTimeForCaution.mp3") },
            { "3363", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_LastDreamOfHome.mp3") },
            { "5416", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_HearthsShadow.mp3") },
            { "6621", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_OlderThanTheUniverse.mp3") },
            { "1524", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_TheSpiritOfWater.mp3") },
            { "2425", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_ElegyForTheRings.mp3") },
            { "3156", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_RiversEndTimes.mp3") },
            { "4241", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_NomaiMeditation.mp3") },
            { "5511", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_CampfireSong.mp3") },
            { "1111", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_MainTitle.mp3") },
            { "1122", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_OuterWilds.mp3") },
            { "1133", ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/Radio_TimberHearth.mp3") },
        };
    }

    private void Start()
    {
        _playerCam = Locator.GetPlayerCamera();
        _powerPrompt = new PriorityScreenPrompt(InputLibrary.cancel, _powerOnText, 0, ScreenPrompt.DisplayState.Normal, false);
        _tunePrompt = new ScreenPrompt(InputLibrary.interactSecondary, "Tune Radio", 0, ScreenPrompt.DisplayState.Normal, false);
        _volumePrompt = new PriorityScreenPrompt(InputLibrary.toolOptionUp, InputLibrary.toolOptionDown, "Adjust Volume", ScreenPrompt.MultiCommandType.POS_NEG, 
            0, ScreenPrompt.DisplayState.Normal, false);
        _leftRightPrompt = new ScreenPrompt(InputLibrary.left, InputLibrary.right, "Change Knob   <CMD>", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _upDownPrompt = new ScreenPrompt(InputLibrary.up, InputLibrary.down, "Change Frequency   <CMD>", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _leavePrompt = new ScreenPrompt(InputLibrary.cancel, "Leave   <CMD>", 0, ScreenPrompt.DisplayState.Normal, false);

        Locator.GetPromptManager().AddScreenPrompt(_powerPrompt, PromptPosition.Center, false);
        Locator.GetPromptManager().AddScreenPrompt(_tunePrompt, PromptPosition.Center, false);
        Locator.GetPromptManager().AddScreenPrompt(_volumePrompt, PromptPosition.Center, false);

        _musicSource.SetLocalVolume(_currentVolume);
        _staticSource.SetLocalVolume(_currentVolume);
    }

    private void Update()
    {
        bool focused = _cameraManipulator.GetFocusedOWItem() == this 
            || (_socketed && _cameraManipulator.GetFocusedItemSocket()?._acceptableType == ItemType);
        if (_lastFocused != focused)
        {
            PatchClass.UpdateFocusedItems(focused);
            _lastFocused = focused;
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
                GlobalMessenger.FireEvent("ExitSatelliteCameraMode");
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
                        }
                        _playingAudio = true;
                    }
                }
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.right, InputMode.All) 
                || OWInput.IsNewlyPressed(InputLibrary.right2, InputMode.All) 
                || OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary, InputMode.All))
            {
                _codeLabels[_currentCodeIndex].color = _deselectColor;
                _currentCodeIndex++;
                if (_currentCodeIndex >= _codes.Length)
                {
                    _currentCodeIndex = 0;
                }
                _codeLabels[_currentCodeIndex].color = _selectColor;
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.left, InputMode.All)
                || OWInput.IsNewlyPressed(InputLibrary.left2, InputMode.All)
                || OWInput.IsNewlyPressed(InputLibrary.toolActionSecondary, InputMode.All))
            {
                _codeLabels[_currentCodeIndex].color = _deselectColor;
                _currentCodeIndex--;
                if (_currentCodeIndex < 0)
                {
                    _currentCodeIndex = _codes.Length - 1;
                }
                _codeLabels[_currentCodeIndex].color = _selectColor;
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.up, InputMode.All)
                || OWInput.IsNewlyPressed(InputLibrary.up2, InputMode.All))
            {
                _codes[_currentCodeIndex]++;
                if (_codes[_currentCodeIndex] > _maxFrequency)
                {
                    _codes[_currentCodeIndex] = _minFrequency;
                }
                _codeLabels[_currentCodeIndex].text = _codes[_currentCodeIndex].ToString();

                if (_playingAudio)
                {
                    if (_powerOn)
                    {
                        _musicSource.FadeOut(0.5f);
                        _staticSource.FadeIn(0.5f, false, false, _currentVolume);
                    }
                    else if (_playingAudio)
                    {
                        _musicSource.time = 0;
                    }
                    _playingAudio = false;
                }
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.down, InputMode.All)
                || OWInput.IsNewlyPressed(InputLibrary.down2, InputMode.All))
            {
                _codes[_currentCodeIndex]--;
                if (_codes[_currentCodeIndex] < _minFrequency)
                {
                    _codes[_currentCodeIndex] = _maxFrequency;
                }
                _codeLabels[_currentCodeIndex].text = _codes[_currentCodeIndex].ToString();

                if (_playingAudio)
                {
                    if (_powerOn)
                    {
                        _musicSource.FadeOut(0.5f);
                        _staticSource.FadeIn(0.5f, false, false, _currentVolume);
                    }
                    else if (_playingAudio)
                    {
                        _musicSource.time = 0;
                    }
                    _playingAudio = false;
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
                        _staticSource.FadeIn(0.5f, false, false, _currentVolume);
                    }
                    else
                    {
                        _musicSource.FadeIn(0.5f, false, false, _currentVolume);
                    }
                    _powerPrompt.SetText(_powerOffText);
                }
                else
                {
                    if (!_playingAudio)
                    {
                        _staticSource.FadeOut(0.5f);
                    }
                    else
                    {
                        _musicSource.FadeOut(0.5f, OWAudioSource.FadeOutCompleteAction.PAUSE);
                    }
                    _powerPrompt.SetText(_powerOnText);
                }
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary))
            {
                Locator.GetToolModeSwapper().UnequipTool();
                Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().LockOn(transform, Vector3.zero);
                GlobalMessenger.FireEvent("EnterSatelliteCameraMode");
                Locator.GetPromptManager().AddScreenPrompt(_upDownPrompt, PromptPosition.UpperRight, true);
                Locator.GetPromptManager().AddScreenPrompt(_leftRightPrompt, PromptPosition.UpperRight, true);
                Locator.GetPromptManager().AddScreenPrompt(_leavePrompt, PromptPosition.UpperRight, true);
                _codeLabels[_currentCodeIndex].color = _selectColor;
                _playerInteracting = true;
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionUp))
            {
                _currentVolume = Mathf.Min(_currentVolume + 0.1f, 1f);
                _musicSource.SetLocalVolume(_currentVolume);
                _staticSource.SetLocalVolume(_currentVolume);
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionDown))
            {
                _currentVolume = Mathf.Max(_currentVolume - 0.1f, 0f);
                _musicSource.SetLocalVolume(_currentVolume);
                _staticSource.SetLocalVolume(_currentVolume);
            }
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

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        _socketed = true;
        _musicSource.spatialBlend = 0f;
        _musicSource.spread = 0f;
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        _socketed = false;
        _musicSource.spatialBlend = 1f;
        _musicSource.spread = 60f;
    }

    private void OnExitShip()
    {
        _musicSource.spatialBlend = 1f;
        _musicSource.spread = 60f;
    }

    private void OnEnterShip()
    {
        if (_socketed)
        {
            _musicSource.spatialBlend = 0f;
            _musicSource.spread = 0f;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
        GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
    }
}
