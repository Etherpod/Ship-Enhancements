using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements;

public class ShipRemoteControl : MonoBehaviour
{
    [SerializeField]
    private SignalscopeCommandList _canvasList;
    [SerializeField]
    private Text _groupHeader;
    [SerializeField]
    private CanvasGroupAnimator _animator;
    
    private AudioSignal _shipAudioSignal;
    private ShipCommand _currentCommand;
    private ShipCommand.CommandGroup _groupMask;
    private ScreenPrompt _interactPrompt;
    private ScreenPrompt _commandPrompt;
    private ScreenPrompt _cycleCommandPrompt;
    private ScreenPrompt _cycleGroupPrompt;
    private InputMode _lastInputMode;
    private bool _scopeEquipped;
    // should probably just combine these two
    private bool _visible;
    private bool _tuned;

    private List<ShipCommand> _commands = 
    [
        new ShipCommand_Explode(),
        new ShipCommand_EngineSwitch(),
        new ShipCommand_ReturnWarp(),
        new ShipCommand_HonkHorn(),
        new ShipCommand_CockpitEject(),
        new ShipCommand_EngineEject(),
        new ShipCommand_SuppliesEject(),
        new ShipCommand_LandingGearEject(),
        new ShipCommand_Autopilot(),
        new ShipCommand_OrbitAutopilot(),
        new ShipCommand_MatchVelocity(),
        new ShipCommand_HoldPosition(),
        new ShipCommand_TargetPlayerPlanet(),
        new ShipCommand_TargetCurrentLockOn(),
        new ShipCommand_TargetPlayer(),
        new ShipCommand_TargetProbe(),
    ];

    private Dictionary<ShipCommand.CommandGroup, string> _groupToDisplayName = new()
    {
        { ShipCommand.CommandGroup.Reactor, "Reactor Commands" },
        { ShipCommand.CommandGroup.Components, "Component Commands" },
        { ShipCommand.CommandGroup.Modules, "Module Commands" },
        { ShipCommand.CommandGroup.Autopilot, "Autopilot Commands" },
        { ShipCommand.CommandGroup.LockOn, "Targeting Commands" },
    };

    private Dictionary<ShipCommand.CommandGroup, List<ShipCommand>> _commandsByGroup = [];

    private void Awake()
    {
        foreach (var cmd in _commands)
        {
            if (!_commandsByGroup.ContainsKey(cmd.GetCommandGroup()))
            {
                _commandsByGroup.Add(cmd.GetCommandGroup(), [cmd]);
            }
            else
            {
                _commandsByGroup[cmd.GetCommandGroup()].Add(cmd);
            }
        }
        
        _groupMask = ShipCommand.CommandGroup.Reactor;
        _currentCommand = _commandsByGroup[_groupMask][0];
        _interactPrompt = new ScreenPrompt(InputLibrary.interact, "Tune In");
        _commandPrompt = new ScreenPrompt(InputLibrary.interact, _currentCommand.GetDisplayName());
        _cycleCommandPrompt = new PriorityScreenPrompt(InputLibrary.up, InputLibrary.down, 
            "Cycle Command", ScreenPrompt.MultiCommandType.POS_NEG);
        _cycleGroupPrompt = new PriorityScreenPrompt(InputLibrary.left, InputLibrary.right, 
            "Cycle Group", ScreenPrompt.MultiCommandType.POS_NEG);

        // Locator should be ready by the time this obj gets created
        var canvas = GetComponent<Canvas>();
        canvas.worldCamera = Locator.GetPlayerCamera().mainCamera;
        canvas.planeDistance = 9f;
        AddMaterials();
        
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
    }

    public void AddMaterials()
    {
        var refMat = GameObject.Find("PlayerHUD/HelmetOffUI/SignalscopeCanvas/SigScopeDisplay/FrequencyLabel")
            .GetComponent<Text>().material;
        
        foreach (var image in GetComponentsInChildren<Image>(true))
        {
            image.material = new Material(refMat);
        }
        foreach (var text in GetComponentsInChildren<Text>(true))
        {
            text.material = new Material(refMat);
        }
    }

    private void Start()
    {
        foreach (AudioSignal signal in Locator.GetAudioSignals())
        {
            if (signal.GetName() == ShipEnhancements.Instance.ShipSignalName)
            {
                _shipAudioSignal = signal;
            }
        }
        
        _animator.SetImmediate(0f, new Vector3(1f, 0f, 1f));
    }

    private void Update()
    {
        if (Locator.GetToolModeSwapper().GetToolMode() == ToolMode.SignalScope
            && OWInput.IsInputMode(InputMode.Character | InputMode.SatelliteCam))
        {
            if (!_scopeEquipped)
            {
                Locator.GetPromptManager().AddScreenPrompt(_interactPrompt, PromptPosition.Center);
                //Locator.GetPromptManager().AddScreenPrompt(_commandPrompt, PromptPosition.Center);
                Locator.GetPromptManager().AddScreenPrompt(_cycleCommandPrompt, PromptPosition.UpperRight);
                Locator.GetPromptManager().AddScreenPrompt(_cycleGroupPrompt, PromptPosition.UpperRight);
                _scopeEquipped = true;
            }

            _interactPrompt.SetVisibility(false);
            //_commandPrompt.SetVisibility(false);
            _cycleCommandPrompt.SetVisibility(false);
            _cycleGroupPrompt.SetVisibility(false);

            if (!_tuned && _shipAudioSignal.GetSignalStrength() == 1f)
            {
                if (OWInput.IsNewlyPressed(InputLibrary.interact))
                {
                    Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().LockOn(_shipAudioSignal.transform);
                    _lastInputMode = OWInput.GetInputMode();
                    OWInput.ChangeInputMode(InputMode.SatelliteCam);

                    _groupHeader.text = _groupToDisplayName[_groupMask];
                    _canvasList.SetCommands(_commandsByGroup[_groupMask].Where(c => c.CanShow()).ToList());
                    SetVisible(true);
                    Locator.GetPlayerAudioController()._oneShotSource.PlayOneShot(AudioType.ShipLogHighlightEntry);
                        
                    _tuned = true;
                    return;
                }

                _interactPrompt.SetVisibility(true);
            }
            
            if (_tuned)
            {
                if (OWInput.IsNewlyPressed(InputLibrary.cancel, InputMode.SatelliteCam) || 
                    _shipAudioSignal.GetSignalStrength() <= 0f)
                {
                    Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().BreakLock();
                    OWInput.ChangeInputMode(_lastInputMode);
                    SetVisible(false);
                    Locator.GetPlayerAudioController()._oneShotSource.PlayOneShot(AudioType.ShipLogDeselectEntry);
                    _tuned = false;
                    return;
                }
                
                //_commandPrompt.SetVisibility(true);
                //_commandPrompt.SetText(_currentCommand.GetDisplayName());
                _cycleCommandPrompt.SetVisibility(true);
                _cycleGroupPrompt.SetVisibility(true);

                if (_currentCommand.CanActivate())
                {
                    //_commandPrompt.SetDisplayState(ScreenPrompt.DisplayState.Normal);

                    if (OWInput.IsNewlyPressed(InputLibrary.interact))
                    {
                        _currentCommand.Activate();

                        if (ShipEnhancements.InMultiplayer)
                        {
                            foreach (uint id in ShipEnhancements.PlayerIDs)
                            {
                                ShipEnhancements.QSBCompat.SendShipCommand(id, _currentCommand.GetType().Name);
                            }
                        }
                    }
                }
                else
                {
                    //_commandPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
                }

                bool changeGroup = OWInput.IsNewlyPressed(InputLibrary.left) ||
                    OWInput.IsNewlyPressed(InputLibrary.right);
                bool changeCommand = OWInput.IsNewlyPressed(InputLibrary.up) ||
                    OWInput.IsNewlyPressed(InputLibrary.down);

                if (changeGroup)
                {
                    CycleGroup(OWInput.IsNewlyPressed(InputLibrary.right));
                    _groupHeader.text = _groupToDisplayName[_groupMask];
                    _canvasList.SetCommands(_commandsByGroup[_groupMask].Where(c => c.CanShow()).ToList());
                }
                if (changeCommand)
                {
                    CycleCommand(OWInput.IsNewlyPressed(InputLibrary.up));
                }

                if (changeGroup || changeCommand)
                {
                    //_commandPrompt.SetText(_currentCommand.GetDisplayName());
                    //_commandPrompt.SetDisplayState(_currentCommand.CanActivate() ? 
                        //ScreenPrompt.DisplayState.Normal : ScreenPrompt.DisplayState.GrayedOut);
                    Locator.GetPlayerAudioController()._oneShotSource
                        .PlayOneShot(changeGroup ? AudioType.Menu_LeftRight : AudioType.Menu_UpDown);
                }
            }
        }
        else if (_scopeEquipped && !OWInput.IsInputMode(InputMode.Menu))
        {
            Locator.GetPromptManager().RemoveScreenPrompt(_interactPrompt, PromptPosition.Center);
            //Locator.GetPromptManager().RemoveScreenPrompt(_commandPrompt, PromptPosition.Center);
            Locator.GetPromptManager().RemoveScreenPrompt(_cycleCommandPrompt, PromptPosition.UpperRight);
            Locator.GetPromptManager().RemoveScreenPrompt(_cycleGroupPrompt, PromptPosition.UpperRight);

            if (_tuned)
            {
                Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().BreakLock();
                OWInput.ChangeInputMode(_lastInputMode);
                _tuned = false;
            }
            
            _scopeEquipped = false;
        }
    }

    private void CycleCommand(bool forward)
    {
        int cycle = 0;
        var commands = _commandsByGroup[_groupMask];
        while (cycle < commands.Count)
        {
            int index = (commands.IndexOf(_currentCommand) + commands.Count + (forward ? 1 : -1)) % commands.Count;
            _currentCommand = commands[index];

            if (_currentCommand.CanShow())
            {
                break;
            }

            cycle++;
        }
    }
    
    private void CycleGroup(bool forward)
    {
        int cycle = 0;
        var groups = _commandsByGroup.Keys.ToList();
        while (cycle < groups.Count)
        {
            int index = (groups.IndexOf(_currentCommand.GetCommandGroup()) + groups.Count + (forward ? 1 : -1)) % groups.Count;
            _groupMask = groups[index];
            _currentCommand = _commandsByGroup[_groupMask][0];

            if (_commandsByGroup[_groupMask].Any(c => c.CanShow()))
            {
                break;
            }

            cycle++;
        }
    }

    public void SetVisible(bool visible)
    {
        if (_visible != visible)
        {
            _visible = visible;
            _animator.AnimateTo(_visible ? 1f : 0f, _visible ? Vector3.one : new Vector3(1f, 0f, 1f), 0.1f);
        }
    }

    public void ReceiveCommandRemote(string commandName)
    {
        ShipCommand command = null;
        foreach (var cmd in _commands)
        {
            if (cmd.GetType().Name == commandName)
            {
                command = cmd;
            }
        }

        if (command == null) return;
        
        if (command.CanActivate())
        {
            command.Activate();
        }
    }

    public bool IsTuned() => _tuned;

    private void OnShipSystemFailure()
    {
        if (_tuned)
        {
            Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().BreakLock();
            OWInput.ChangeInputMode(_lastInputMode);
            SetVisible(false);
            Locator.GetPlayerAudioController()._oneShotSource.PlayOneShot(AudioType.ShipLogDeselectEntry);
            _tuned = false;
        }
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}