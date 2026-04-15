using System;
using OWML.ModHelper.Events;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipRemoteControl : MonoBehaviour
{
    private AudioSignal _shipAudioSignal;
    private ShipCommand _currentCommand;
    private ScreenPrompt _interactPrompt;
    private ScreenPrompt _commandPrompt;
    private ScreenPrompt _cyclePrompt;
    private InputMode _lastInputMode;
    private bool _scopeEquipped = false;
    private bool _tuned = false;

    private List<ShipCommand> _commands = 
    [
        new ShipCommand_Explode(),
        new ShipCommand_EngineSwitch(),
        new ShipCommand_ReturnWarp(),
        new ShipCommand_HonkHorn(),
        new ShipCommand_Autopilot(),
        new ShipCommand_OrbitAutopilot(),
        new ShipCommand_MatchVelocity(),
        new ShipCommand_HoldPosition(),
        new ShipCommand_CockpitEject(),
        new ShipCommand_EngineEject(),
        new ShipCommand_SuppliesEject(),
        new ShipCommand_LandingGearEject(),
    ];

    private void Awake()
    {
        _currentCommand = _commands[0];
        _interactPrompt = new ScreenPrompt(InputLibrary.interact, "Tune In");
        _commandPrompt = new ScreenPrompt(InputLibrary.interact, _currentCommand.GetDisplayName());
        _cyclePrompt = new PriorityScreenPrompt(InputLibrary.toolOptionY, "Cycle Command");
        
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
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
    }

    private void Update()
    {
        if (Locator.GetToolModeSwapper().GetToolMode() == ToolMode.SignalScope
            && OWInput.IsInputMode(InputMode.Character | InputMode.SatelliteCam))
        {
            if (!_scopeEquipped)
            {
                Locator.GetPromptManager().AddScreenPrompt(_interactPrompt, PromptPosition.Center);
                Locator.GetPromptManager().AddScreenPrompt(_commandPrompt, PromptPosition.Center);
                Locator.GetPromptManager().AddScreenPrompt(_cyclePrompt, PromptPosition.Center);
                _scopeEquipped = true;
            }

            _interactPrompt.SetVisibility(false);
            _commandPrompt.SetVisibility(false);
            _cyclePrompt.SetVisibility(false);

            if (!_tuned && _shipAudioSignal.GetSignalStrength() == 1f)
            {
                if (OWInput.IsNewlyPressed(InputLibrary.interact))
                {
                    Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().LockOn(_shipAudioSignal.transform);
                    _lastInputMode = OWInput.GetInputMode();
                    OWInput.ChangeInputMode(InputMode.SatelliteCam);
                    _tuned = true;
                    return;
                }
                
                _interactPrompt.SetVisibility(true);
            }
            
            if (_tuned)
            {
                if (OWInput.IsNewlyPressed(InputLibrary.cancel) || _shipAudioSignal.GetSignalStrength() <= 0f)
                {
                    Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().BreakLock();
                    OWInput.ChangeInputMode(_lastInputMode);
                    _tuned = false;
                    return;
                }
                
                _commandPrompt.SetVisibility(true);
                _commandPrompt.SetText(_currentCommand.GetDisplayName());
                _cyclePrompt.SetVisibility(true);

                if (_currentCommand.CanActivate())
                {
                    _commandPrompt.SetDisplayState(ScreenPrompt.DisplayState.Normal);

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
                    _commandPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
                }

                bool changed = false;

                if (OWInput.IsNewlyPressed(InputLibrary.toolOptionUp))
                {
                    CycleCommand(false);
                    changed = true;
                }
                else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionDown))
                {
                    CycleCommand(true);
                    changed = true;
                }

                if (changed)
                {
                    _commandPrompt.SetText(_currentCommand.GetDisplayName());
                    _commandPrompt.SetDisplayState(_currentCommand.CanActivate() ? 
                        ScreenPrompt.DisplayState.Normal : ScreenPrompt.DisplayState.GrayedOut);
                    Locator.GetPlayerAudioController()._oneShotSource.PlayOneShot(AudioType.Menu_LeftRight);
                }
            }
        }
        else if (_scopeEquipped)
        {
            Locator.GetPromptManager().RemoveScreenPrompt(_interactPrompt, PromptPosition.Center);
            Locator.GetPromptManager().RemoveScreenPrompt(_commandPrompt, PromptPosition.Center);
            Locator.GetPromptManager().RemoveScreenPrompt(_cyclePrompt, PromptPosition.Center);

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
        while (cycle < _commands.Count)
        {
            if (forward)
            {
                int index = _commands.IndexOf(_currentCommand) + 1;
                if (index > _commands.Count - 1)
                {
                    index = 0;
                }
                _currentCommand = _commands[index];
            }
            else
            {
                int index = _commands.IndexOf(_currentCommand) - 1;
                if (index < 0)
                {
                    index = _commands.Count - 1;
                }
                _currentCommand = _commands[index];
            }

            if (_currentCommand.CanShow())
            {
                break;
            }

            cycle++;
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
            _tuned = false;
        }
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}