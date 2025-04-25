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

    private ShipEjectionSystem _cockpitEjectSystem;
    private ShipModuleEjectionSystem _suppliesEjectSystem;
    private ShipModuleEjectionSystem _engineEjectSystem;
    private ShipModuleEjectionSystem _landingGearEjectSystem;
    private Autopilot _autopilot;
    private PidAutopilot _pidAutopilot;
    private bool _lastAutopilotState = false;
    private readonly string _engageAutopilotText = "Engage Autopilot";
    private readonly string _disengageAutopilotText = "Disengage Autopilot";

    private List<ShipCommand> _commands = new()
    {
        ShipCommand.Explode,
        ShipCommand.Autopilot,
        ShipCommand.TurnOff,
        ShipCommand.Warp,
        ShipCommand.Eject,
        ShipCommand.EjectSupplies,
        ShipCommand.EjectEngine,
        ShipCommand.DetachLandingGear
    };

    private Dictionary<ShipCommand, string> _commandNames = new()
    {
        { ShipCommand.Autopilot, "Engage Autopilot" },
        { ShipCommand.TurnOff, "Turn Off Engine" },
        { ShipCommand.Warp, "Activate Return Warp" },
        { ShipCommand.Explode, "Explode" },
        { ShipCommand.Eject, "Eject Cockpit" },
        { ShipCommand.EjectSupplies, "Eject Supplies" },
        { ShipCommand.EjectEngine, "Eject Engine" },
        { ShipCommand.DetachLandingGear, "Detach Landing Gear" },
    };

    private void Awake()
    {
        _currentCommand = ShipCommand.Explode;
        _commandPrompt = new ScreenPrompt(InputLibrary.interact, _commandNames[_currentCommand], 0, ScreenPrompt.DisplayState.Normal, false);
        _cyclePrompt = new PriorityScreenPrompt(InputLibrary.toolOptionY, "Cycle Command", 0, ScreenPrompt.DisplayState.Normal, false);

        _autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
        _cockpitEjectSystem = SELocator.GetShipTransform().GetComponentInChildren<ShipEjectionSystem>();
        ShipModuleEjectionSystem[] ejects = SELocator.GetShipTransform().GetComponentsInChildren<ShipModuleEjectionSystem>();
        foreach (ShipModuleEjectionSystem eject in ejects)
        {
            if (eject.GetEjectType() == ShipModuleEjectionSystem.EjectableModule.Supplies)
            {
                _suppliesEjectSystem = eject;
            }
            else if (eject.GetEjectType() == ShipModuleEjectionSystem.EjectableModule.Engine)
            {
                _engineEjectSystem = eject;
            }
            else if (eject.GetEjectType() == ShipModuleEjectionSystem.EjectableModule.LandingGear)
            {
                _landingGearEjectSystem = eject;
            }
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

        _warpCoreController = SELocator.GetShipTransform().GetComponentInChildren<ShipWarpCoreController>();
        _pidAutopilot = SELocator.GetShipBody().GetComponent<PidAutopilot>();
    }

    private void Update()
    {
        UpdateAutopilotText();

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
                        bool run = true;
                        if (_currentCommand == ShipCommand.Explode
                            && !SELocator.GetShipDamageController()._exploded)
                        {
                            SELocator.GetShipDamageController().Explode();
                            run = false;
                        }
                        else if (_currentCommand == ShipCommand.Warp)
                        {
                            _warpCoreController.ActivateWarp();
                            _warpCoreController.SendWarpMessage();
                            run = false;
                        }
                        else if (_currentCommand == ShipCommand.TurnOff)
                        {
                            SELocator.GetShipTransform().GetComponentInChildren<ShipEngineSwitch>().TurnOffEngine();
                        }
                        else if (_currentCommand == ShipCommand.Autopilot)
                        {
                            if ((bool)enableEnhancedAutopilot.GetProperty())
                            {
                                if (_autopilot.enabled || _pidAutopilot.enabled)
                                {
                                    SELocator.GetAutopilotPanelController().CancelAutopilot();
                                }
                                else
                                {
                                    SELocator.GetAutopilotPanelController().ActivateAutopilot();
                                }
                            }
                            else
                            {
                                if (!_autopilot.enabled)
                                {
                                    ReferenceFrame rf = SELocator.GetReferenceFrame();
                                    if (rf != null)
                                    {
                                        _autopilot.FlyToDestination(rf);
                                    }
                                }
                                else
                                {
                                    _autopilot.Abort();
                                }
                            }
                        }
                        else if (_currentCommand == ShipCommand.Eject
                            && !SELocator.GetShipDamageController()._cockpitDetached)
                        {
                            _cockpitEjectSystem._ejectPressed = true;
                            _cockpitEjectSystem.enabled = true;
                        }
                        else if (_currentCommand == ShipCommand.EjectSupplies)
                        {
                            if (_suppliesEjectSystem != null && _suppliesEjectSystem.CanEject())
                            {
                                _suppliesEjectSystem.Eject();
                            }
                        }
                        else if (_currentCommand == ShipCommand.EjectEngine)
                        {
                            if (_engineEjectSystem != null && _engineEjectSystem.CanEject())
                            {
                                _engineEjectSystem.Eject();
                            }
                        }
                        else if (_currentCommand == ShipCommand.DetachLandingGear)
                        {
                            if (_landingGearEjectSystem != null && _landingGearEjectSystem.CanEject())
                            {
                                _landingGearEjectSystem.Eject();
                            }
                        }
                        else
                        {
                            run = false;
                        }

                        if (run && ShipEnhancements.InMultiplayer)
                        {
                            foreach (uint id in ShipEnhancements.PlayerIDs)
                            {
                                ShipEnhancements.QSBCompat.SendShipCommand(id, _currentCommand);
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
                    _commandPrompt.SetText(_commandNames[_currentCommand]);
                    _commandPrompt.SetDisplayState(ShipCommandAvailable(_currentCommand) ? ScreenPrompt.DisplayState.Normal : ScreenPrompt.DisplayState.GrayedOut);
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
            case ShipCommand.Explode:
                return _engineEjectSystem == null || _engineEjectSystem.CanEject();
            case ShipCommand.Warp:
                return !_warpCoreController.IsWarping();
            case ShipCommand.TurnOff:
                return ShipEnhancements.Instance.engineOn;
            case ShipCommand.Autopilot:
                return AutopilotCommandAvailable();
            case ShipCommand.Eject:
                return !SELocator.GetShipDamageController().IsCockpitDetached();
            case ShipCommand.EjectSupplies:
                return _suppliesEjectSystem != null && _suppliesEjectSystem.CanEject();
            case ShipCommand.EjectEngine:
                return _engineEjectSystem != null && _engineEjectSystem.CanEject();
            case ShipCommand.DetachLandingGear:
                return _landingGearEjectSystem != null && _landingGearEjectSystem.CanEject();
            default:
                return true;
        }
    }

    private bool AutopilotCommandAvailable()
    {
        if (!ShipEnhancements.Instance.engineOn || _autopilot.IsDamaged())
        {
            return false;
        }

        if ((bool)enableEnhancedAutopilot.GetProperty() 
            && SELocator.GetAutopilotPanelController().IsOrbitSelected())
        {
            ReferenceFrame rf = SELocator.GetReferenceFrame(ignorePassiveFrame: false);
            return _pidAutopilot.enabled || rf != null;
        }
        else
        {
            ReferenceFrame referenceFrame = SELocator.GetReferenceFrame();
            return _autopilot.enabled || (referenceFrame != null && referenceFrame.GetAllowAutopilot()
                && (PlayerData.GetAutopilotEnabled() || (bool)enableEnhancedAutopilot.GetProperty())
                && Vector3.Distance(SELocator.GetShipBody().GetPosition(), referenceFrame.GetPosition())
                > referenceFrame.GetAutopilotArrivalDistance());
        }
    }

    private bool ShipCommandHidden(ShipCommand cmd)
    {
        switch (cmd)
        {
            case ShipCommand.Warp:
                return (string)shipWarpCoreType.GetProperty() != "Disabled" || _warpCoreController == null;
            case ShipCommand.TurnOff:
                return !(bool)addEngineSwitch.GetProperty();
            case ShipCommand.Autopilot:
                return !(bool)enableEnhancedAutopilot.GetProperty() &&
                    ((bool)disableReferenceFrame.GetProperty() || !PlayerData.GetAutopilotEnabled());
            case ShipCommand.EjectSupplies:
                return !(bool)extraEjectButtons.GetProperty();
            case ShipCommand.EjectEngine:
                return !(bool)extraEjectButtons.GetProperty();
            case ShipCommand.DetachLandingGear:
                return !(bool)extraEjectButtons.GetProperty();
            default:
                return false;
        }
    }

    private void UpdateAutopilotText()
    {
        if (_currentCommand != ShipCommand.Autopilot) return;

        if ((bool)enableEnhancedAutopilot.GetProperty())
        {
            if (_lastAutopilotState != (_autopilot.enabled || _pidAutopilot.enabled))
            {
                _lastAutopilotState = _autopilot.enabled || _pidAutopilot.enabled;
                if (_lastAutopilotState)
                {
                    _commandNames[_currentCommand] = _disengageAutopilotText;
                }
                else
                {
                    _commandNames[_currentCommand] = _engageAutopilotText;
                }
                _commandPrompt.SetText(_commandNames[_currentCommand]);
            }
        }
        else
        {
            if (_lastAutopilotState != _autopilot.enabled)
            {
                _lastAutopilotState = _autopilot.enabled;
                if (_lastAutopilotState)
                {
                    _commandNames[_currentCommand] = _disengageAutopilotText;
                }
                else
                {
                    _commandNames[_currentCommand] = _engageAutopilotText;
                }
                _commandPrompt.SetText(_commandNames[_currentCommand]);
            }
        }
    }

    public void ReceiveCommandRemote(ShipCommand command)
    {
        if (ShipCommandAvailable(command))
        {
            if (command == ShipCommand.Explode
                && !SELocator.GetShipDamageController()._exploded)
            {
                SELocator.GetShipDamageController().Explode();
            }
            else if (command == ShipCommand.Warp)
            {
                _warpCoreController.ActivateWarp();
            }
            else if (command == ShipCommand.TurnOff)
            {
                SELocator.GetShipTransform().GetComponentInChildren<ShipEngineSwitch>().TurnOffEngine();
            }
            else if (command == ShipCommand.Eject
                && !SELocator.GetShipDamageController()._cockpitDetached)
            {
                _cockpitEjectSystem._ejectPressed = true;
                _cockpitEjectSystem.enabled = true;
            }
            else if (_currentCommand == ShipCommand.EjectSupplies)
            {
                if (_suppliesEjectSystem != null && _suppliesEjectSystem.CanEject())
                {
                    _suppliesEjectSystem.Eject();
                }
            }
            else if (_currentCommand == ShipCommand.EjectEngine)
            {
                if (_engineEjectSystem != null && _engineEjectSystem.CanEject())
                {
                    _engineEjectSystem.Eject();
                }
            }
            else if (_currentCommand == ShipCommand.DetachLandingGear)
            {
                if (_landingGearEjectSystem != null && _landingGearEjectSystem.CanEject())
                {
                    _landingGearEjectSystem.Eject();
                }
            }
        }
    }
}

public enum ShipCommand
{
    Explode, 
    Warp,
    TurnOff,
    Autopilot,
    Eject,
    EjectEngine,
    EjectSupplies,
    DetachLandingGear,
}