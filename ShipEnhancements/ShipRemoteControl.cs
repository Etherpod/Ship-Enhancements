using OWML.ModHelper.Events;
using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipRemoteControl : MonoBehaviour
{
    private AudioSignal _shipAudioSignal;
    private ShipWarpCoreController _warpCoreController;
    private ShipCommand _currentCommand;
    private ScreenPrompt _commandPrompt;
    private ScreenPrompt _cyclePrompt;
    private bool _scopeEquipped = false;

    private List<ShipCommand> _commands = new()
    {
        ShipCommand.Explode,
        ShipCommand.Warp,
        ShipCommand.Eject,
        ShipCommand.TurnOff
    };

    private Dictionary<ShipCommand, string> _commandNames = new()
    {
        { ShipCommand.Explode, "Explode" },
        { ShipCommand.Warp, "Activate Return Warp" },
        { ShipCommand.Eject, "Eject Cockpit" },
        { ShipCommand.TurnOff, "Turn Off Engine" },
    };

    private void Awake()
    {
        _currentCommand = ShipCommand.Explode;
        _commandPrompt = new ScreenPrompt(InputLibrary.interact, _commandNames[_currentCommand], 0, ScreenPrompt.DisplayState.Normal, false);
        _cyclePrompt = new PriorityScreenPrompt(InputLibrary.toolOptionY, "Cycle Command", 0, ScreenPrompt.DisplayState.Normal, false);
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

        _warpCoreController = SELocator.GetShipTransform().GetComponentInChildren<ShipWarpCoreController>();
    }

    private void Update()
    {
        if (Locator.GetToolModeSwapper().GetToolMode() == ToolMode.SignalScope
            && OWInput.IsInputMode(InputMode.Character))
        {
            if (!_scopeEquipped)
            {
                Locator.GetPromptManager().AddScreenPrompt(_commandPrompt, PromptPosition.Center);
                Locator.GetPromptManager().AddScreenPrompt(_cyclePrompt, PromptPosition.Center);
                _scopeEquipped = true;
            }

            _commandPrompt.SetVisibility(false);
            _cyclePrompt.SetVisibility(false);

            if (_shipAudioSignal.GetSignalStrength() == 1f)
            {
                _commandPrompt.SetVisibility(true);
                _cyclePrompt.SetVisibility(true);

                if (ShipCommandAvailable(_currentCommand))
                {
                    _commandPrompt.SetDisplayState(ScreenPrompt.DisplayState.Normal);

                    if (OWInput.IsNewlyPressed(InputLibrary.interact))
                    {
                        if (_currentCommand == ShipCommand.Explode
                            && !SELocator.GetShipDamageController()._exploded)
                        {
                            SELocator.GetShipDamageController().Explode();
                        }
                        else if (_currentCommand == ShipCommand.Warp)
                        {
                            _warpCoreController.ActivateWarp();
                        }
                        else if (_currentCommand == ShipCommand.Eject
                            && !SELocator.GetShipDamageController()._cockpitDetached)
                        {
                            SELocator.GetShipTransform().Find("Module_Cockpit").GetComponent<ShipDetachableModule>().Detach();
                        }
                        else if (_currentCommand == ShipCommand.TurnOff)
                        {
                            SELocator.GetShipTransform().GetComponentInChildren<ShipEngineSwitch>().TurnOffEngine();
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
                    CycleCommand(true);
                    changed = true;
                }
                else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionDown))
                {
                    CycleCommand(false);
                    changed = true;
                }

                if (changed)
                {
                    _commandPrompt.SetText(_commandNames[_currentCommand]);
                    Locator.GetPlayerAudioController()._oneShotSource.PlayOneShot(AudioType.Menu_LeftRight);
                }
            }
        }
        else if (_scopeEquipped)
        {
            Locator.GetPromptManager().RemoveScreenPrompt(_commandPrompt, PromptPosition.Center);
            Locator.GetPromptManager().RemoveScreenPrompt(_cyclePrompt, PromptPosition.Center);
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

            if (!ShipCommandHidden(_currentCommand))
            {
                break;
            }

            cycle++;
        }
    }

    private bool ShipCommandAvailable(ShipCommand cmd)
    {
        switch (cmd)
        {
            case ShipCommand.Warp:
                return !_warpCoreController.IsWarping();
            case ShipCommand.TurnOff:
                return ShipEnhancements.Instance.engineOn;
            default:
                return true;
        }
    }

    private bool ShipCommandHidden(ShipCommand cmd)
    {
        switch (cmd)
        {
            case ShipCommand.Warp:
                return !(bool)addShipWarpCore.GetProperty() || _warpCoreController == null;
            case ShipCommand.TurnOff:
                return !(bool)addEngineSwitch.GetProperty();
            default:
                return false;
        }
    }
}

public enum ShipCommand
{
    Explode, 
    Warp,
    Eject,
    TurnOff
}