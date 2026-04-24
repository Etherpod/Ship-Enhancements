using System.Linq;
using ShipEnhancements.Buttons;
using ShipEnhancements.Ernesto;
using ShipEnhancements.ModSettings;
using UnityEngine;
using static ShipEnhancements.Settings;

namespace ShipEnhancements.RemoteControl;

public abstract class ShipCommand
{
	public enum CommandGroup
	{
		Modules,
		Components,
		Autopilot,
		LockOn,
		Misc
	}
	
	public abstract string GetDisplayName();

	public abstract CommandGroup GetCommandGroup();

	public abstract bool CanShow();

	public abstract bool CanActivate();
	
	public virtual bool ShouldSyncMultiplayer() => false;
	
	public abstract void Activate();
}

public class ShipCommand_Explode : ShipCommand
{
	public override string GetDisplayName() => "Explode";

	public override CommandGroup GetCommandGroup() => CommandGroup.Components;

	public override bool CanShow() => true;
	
	public override bool CanActivate() => 
		SELocator.GetShipTransform().Find("Module_Engine") != null;

	public override void Activate()
	{
		if (!SELocator.GetShipDamageController()._exploded)
		{
			SELocator.GetShipDamageController().Explode();
		}
	}
}

public class ShipCommand_EngineSwitch : ShipCommand
{
	private ShipEngineSwitch _engineSwitch;
	
	public ShipCommand_EngineSwitch()
	{
		if ((bool)addEngineSwitch.GetProperty())
		{
			_engineSwitch = SELocator.GetShipTransform().GetComponentInChildren<ShipEngineSwitch>();
		}
	}
	
	public override string GetDisplayName() => "Turn Off Engine";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Components;

	public override bool CanShow() => _engineSwitch != null;
	
	public override bool CanActivate() => ShipEnhancements.Instance.engineOn;

	public override bool ShouldSyncMultiplayer() => true;

	public override void Activate()
	{
		_engineSwitch.TurnOffEngine();
	}
}

public class ShipCommand_ReturnWarp : ShipCommand
{
	private ShipWarpCoreController _warpCoreController;
	
	public ShipCommand_ReturnWarp()
	{
		if ((string)shipWarpCoreType.GetProperty() != "Disabled")
		{
			_warpCoreController = SELocator.GetShipTransform().GetComponentInChildren<ShipWarpCoreController>();
		}
	}
	
	public override string GetDisplayName() => "Activate Return Warp";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Components;

	public override bool CanShow() => _warpCoreController != null;
	
	public override bool CanActivate() => 
		!_warpCoreController.IsWarping();

	public override void Activate()
	{
		_warpCoreController.ActivateWarp();
		_warpCoreController.SendWarpMessage();
	}
}

public class ShipCommand_HonkHorn : ShipCommand
{
	private ShipHornController _hornController;
	
	public ShipCommand_HonkHorn()
	{
		if ((string)shipHornType.GetProperty() != "None")
		{
			_hornController = SELocator.GetShipTransform().GetComponentInChildren<ShipHornController>();
		}
	}
	
	public override string GetDisplayName() => "Honk";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Components;

	public override bool CanShow() => _hornController != null;

	public override bool CanActivate() => !_hornController.IsPlaying();
	
	public override bool ShouldSyncMultiplayer() => true;

	public override void Activate()
	{
		_hornController.PlayHorn();
	}
}

public class ShipCommand_ToggleAutoAlign : ShipCommand
{
	private AutoAlignButton _alignButton;
	
	public ShipCommand_ToggleAutoAlign()
	{
		if ((bool)enableAutoAlign.GetProperty())
		{
			_alignButton = SELocator.GetShipTransform().GetComponentInChildren<AutoAlignButton>();
		}
	}
	
	public override string GetDisplayName() => 
		_alignButton.IsOn() ? "Disable Auto Align" : "Enable Auto Align";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Components;

	public override bool CanShow() => _alignButton != null;

	public override bool CanActivate() => true;
	
	public override bool ShouldSyncMultiplayer() => true;

	public override void Activate()
	{
		_alignButton.SetState(!_alignButton.IsOn());
		_alignButton.OnChangeStateEvent();
		_alignButton.RaiseChangeStateEvent();
	}
}

public class ShipCommand_ToggleAlignDirection : ShipCommand
{
	private AutoAlignDirectionButton _alignDirectionButton;
	
	public ShipCommand_ToggleAlignDirection()
	{
		if ((bool)enableAutoAlign.GetProperty())
		{
			_alignDirectionButton = SELocator.GetShipTransform().GetComponentInChildren<AutoAlignDirectionButton>();
		}
	}
	
	public override string GetDisplayName() => 
		_alignDirectionButton.IsOn() ? "Set Alignment Forward" : "Set Alignment Down";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Components;

	public override bool CanShow() => _alignDirectionButton != null;

	public override bool CanActivate() => true;
	
	public override bool ShouldSyncMultiplayer() => true;

	public override void Activate()
	{
		_alignDirectionButton.SetState(!_alignDirectionButton.IsOn());
		_alignDirectionButton.OnChangeStateEvent();
		_alignDirectionButton.RaiseChangeStateEvent();
	}
}

public class ShipCommand_ToggleGravityGear : ShipCommand
{
	private GravityLandingGearSwitch _gravitySwitch;
	
	public ShipCommand_ToggleGravityGear()
	{
		if ((bool)enableGravityLandingGear.GetProperty())
		{
			_gravitySwitch = SELocator.GetShipTransform().GetComponentInChildren<GravityLandingGearSwitch>();
		}
	}
	
	public override string GetDisplayName() => 
		_gravitySwitch.IsOn() ? "Disable Gravity Landing Gear" : "Enable Gravity Landing Gear";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Components;

	public override bool CanShow() => _gravitySwitch != null;

	public override bool CanActivate() => true;
	
	public override bool ShouldSyncMultiplayer() => true;

	public override void Activate()
	{
		_gravitySwitch.SetState(!_gravitySwitch.IsOn());
	}
}

public class ShipCommand_ToggleGravityGearInvert : ShipCommand
{
	private GravityGearInvertSwitch _invertSwitch;
	
	public ShipCommand_ToggleGravityGearInvert()
	{
		if ((bool)enableGravityLandingGear.GetProperty())
		{
			_invertSwitch = SELocator.GetShipTransform().GetComponentInChildren<GravityGearInvertSwitch>();
		}
	}
	
	public override string GetDisplayName() => 
		_invertSwitch.IsOn() ? "Reset Gravity Gear" : "Invert Gravity Gear";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Components;

	public override bool CanShow() => _invertSwitch != null;

	public override bool CanActivate() => true;
	
	public override bool ShouldSyncMultiplayer() => true;

	public override void Activate()
	{
		_invertSwitch.SetState(!_invertSwitch.IsOn());
	}
}

public class ShipCommand_Autopilot : ShipCommand
{
	private Autopilot _autopilot;
	private AutopilotPanelController _controller;
	
	public ShipCommand_Autopilot()
	{
		_autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();

		if ((bool)enableEnhancedAutopilot.GetProperty())
		{
			_controller = SELocator.GetAutopilotPanelController();
		}
	}

	public override string GetDisplayName()
	{
		return _autopilot.IsFlyingToDestination() ? "Disengage Autopilot" : "Activate Autopilot";
	}
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Autopilot;

	public override bool CanShow() => _autopilot != null;

	public override bool CanActivate()
	{
		if (!ShipEnhancements.Instance.engineOn || _autopilot.IsDamaged() ||
			!SELocator.GetShipResources().AreThrustersUsable())
		{
			return false;
		}
		
		ReferenceFrame referenceFrame = SELocator.GetReferenceFrame();
		return _autopilot.IsFlyingToDestination() || (referenceFrame != null && referenceFrame.GetAllowAutopilot() && 
			Vector3.Distance(SELocator.GetShipBody().GetPosition(), referenceFrame.GetPosition()) > 
			referenceFrame.GetAutopilotArrivalDistance());
	}

	public override void Activate()
	{
		if (_controller != null)
		{
			if (!_controller.IsApproachSelected())
			{
				_controller.SetAutopilotMode(false);
			}
			
			_controller.CancelMatchVelocity();
			
			if (!_autopilot.IsFlyingToDestination())
			{
				_controller.ActivateAutopilot();
			}
			else
			{
				_controller.CancelAutopilot();
			}

			return;
		}
		
		if (!_autopilot.IsFlyingToDestination())
		{
			ReferenceFrame rf = SELocator.GetReferenceFrame();
			if (rf != null)
			{
				_autopilot.FlyToDestination(rf);

				if (ShipEnhancements.InMultiplayer)
				{
					foreach (var id in ShipEnhancements.PlayerIDs)
					{
						ShipEnhancements.QSBCompat.SendAutopilotState(id, rf.GetOWRigidBody(), destination: true);
					}
				}
			}
		}
		else
		{
			_autopilot.Abort();
			
			if (ShipEnhancements.InMultiplayer)
			{
				foreach (var id in ShipEnhancements.PlayerIDs)
				{
					ShipEnhancements.QSBCompat.SendAutopilotState(id, null, abort: true);
				}
			}
		}
	}
}

public class ShipCommand_OrbitAutopilot : ShipCommand
{
	private Autopilot _autopilot;
	private PidAutopilot _pidAutopilot;
	private AutopilotPanelController _controller;
	
	public ShipCommand_OrbitAutopilot()
	{
		_autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
		if ((bool)enableEnhancedAutopilot.GetProperty())
		{
			_pidAutopilot = SELocator.GetShipBody().GetComponent<PidAutopilot>();
			_controller = SELocator.GetAutopilotPanelController();
		}
	}

	public override string GetDisplayName()
	{
		return _pidAutopilot.enabled && _pidAutopilot.GetCurrentMode() == PidMode.Orbit ? 
			"Disengage Orbital Autopilot" : "Activate Orbital Autopilot";
	}
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Autopilot;

	public override bool CanShow() => _autopilot != null && _pidAutopilot != null && _controller != null;

	public override bool CanActivate()
	{
		if (!ShipEnhancements.Instance.engineOn || _autopilot.IsDamaged() ||
			!SELocator.GetShipResources().AreThrustersUsable())
		{
			return false;
		}

		ReferenceFrame rf = SELocator.GetReferenceFrame(ignorePassiveFrame: false);
		return (_pidAutopilot.enabled && _pidAutopilot.GetCurrentMode() == PidMode.Orbit) || rf != null;
	}

	public override void Activate()
	{
		if (!_controller.IsOrbitSelected())
		{
			_controller.SetAutopilotMode(true);
		}

		_controller.CancelMatchVelocity();

		if (!_pidAutopilot.enabled)
		{
			_controller.ActivateAutopilot();
		}
		else
		{
			_controller.CancelAutopilot();
		}
	}
}

public class ShipCommand_MatchVelocity : ShipCommand
{
	private Autopilot _autopilot;
	private AutopilotPanelController _controller;
	
	public ShipCommand_MatchVelocity()
	{
		_autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();

		if ((bool)enableEnhancedAutopilot.GetProperty())
		{
			_controller = SELocator.GetAutopilotPanelController();
		}
	}

	public override string GetDisplayName()
	{
		return _autopilot.IsMatchingVelocity() && !_autopilot.IsFlyingToDestination() ? 
			"Disengage Match Velocity" : "Activate Match Velocity";
	}
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Autopilot;

	public override bool CanShow() => _autopilot != null;

	public override bool CanActivate()
	{
		if (!ShipEnhancements.Instance.engineOn || _autopilot.IsDamaged() ||
			!SELocator.GetShipResources().AreThrustersUsable())
		{
			return false;
		}

		return _autopilot.IsMatchingVelocity() || 
			SELocator.GetReferenceFrame(ignorePassiveFrame: false) != null;
	}

	public override void Activate()
	{
		if (_controller != null)
		{
			if (!_controller.IsMatchVelocitySelected())
			{
				_controller.SetMatchMode(false);
			}
			
			_controller.CancelAutopilot();
			
			if (!_autopilot.IsMatchingVelocity())
			{
				if (ShipEnhancements.GEInteraction != null)
				{
					ShipEnhancements.GEInteraction.EnableContinuousMatchVelocity();
				}
				_controller.ActivateMatchVelocity();
			}
			else if (ShipEnhancements.GEInteraction != null 
				&& ShipEnhancements.GEInteraction.IsContinuousMatchVelocityEnabled())
			{
				ShipEnhancements.GEInteraction.StopContinuousMatchVelocity();
			}
			else
			{
				_controller.CancelMatchVelocity();
			}

			return;
		}

		if (_autopilot.IsFlyingToDestination())
		{
			_autopilot.Abort();
			
			if (ShipEnhancements.InMultiplayer)
			{
				foreach (var id in ShipEnhancements.PlayerIDs)
				{
					ShipEnhancements.QSBCompat.SendAutopilotState(id, null, abort: true);
				}
			}
		}
		
		if (!_autopilot.IsMatchingVelocity())
		{
			ReferenceFrame rf = SELocator.GetReferenceFrame(ignorePassiveFrame: false);
			if (rf != null)
			{
				if (ShipEnhancements.GEInteraction != null)
				{
					ShipEnhancements.GEInteraction.EnableContinuousMatchVelocity();
				}
				
				_autopilot.StartMatchVelocity(rf);
				
				if (ShipEnhancements.InMultiplayer)
				{
					foreach (var id in ShipEnhancements.PlayerIDs)
					{
						ShipEnhancements.QSBCompat.SendAutopilotState(id, rf.GetOWRigidBody(), startMatch: true);
					}
				}
			}
		}
		else if (ShipEnhancements.GEInteraction != null 
			&& ShipEnhancements.GEInteraction.IsContinuousMatchVelocityEnabled())
		{
			ShipEnhancements.GEInteraction.StopContinuousMatchVelocity();
		}
		else
		{
			_autopilot.StopMatchVelocity();
			
			if (ShipEnhancements.InMultiplayer)
			{
				foreach (var id in ShipEnhancements.PlayerIDs)
				{
					ShipEnhancements.QSBCompat.SendAutopilotState(id, null, stopMatch: true);
				}
			}
		}
	}
}

public class ShipCommand_HoldPosition : ShipCommand
{
	private Autopilot _autopilot;
	private PidAutopilot _pidAutopilot;
	private AutopilotPanelController _controller;
	
	public ShipCommand_HoldPosition()
	{
		_autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
		if ((bool)enableEnhancedAutopilot.GetProperty())
		{
			_pidAutopilot = SELocator.GetShipBody().GetComponent<PidAutopilot>();
			_controller = SELocator.GetAutopilotPanelController();
		}
	}

	public override string GetDisplayName()
	{
		return (_pidAutopilot.enabled && _pidAutopilot.GetCurrentMode() == PidMode.HoldPosition) ? 
			"Disengage Position Hold" : "Activate Position Hold";
	}
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Autopilot;

	public override bool CanShow() => _autopilot != null && _pidAutopilot != null && _controller != null;

	public override bool CanActivate()
	{
		if (!ShipEnhancements.Instance.engineOn || _autopilot.IsDamaged() ||
			!SELocator.GetShipResources().AreThrustersUsable())
		{
			return false;
		}

		ReferenceFrame rf = SELocator.GetReferenceFrame(ignorePassiveFrame: false);
		return (_pidAutopilot.enabled && _pidAutopilot.GetCurrentMode() == PidMode.HoldPosition) || rf != null;
	}

	public override void Activate()
	{
		if (!_controller.IsHoldPositionSelected())
		{
			_controller.SetMatchMode(true);
		}
		
		_controller.CancelAutopilot();

		if (!_pidAutopilot.enabled)
		{
			_controller.ActivateMatchVelocity();
		}
		else
		{
			_controller.CancelMatchVelocity();
		}
	}
}

public class ShipCommand_CockpitEject : ShipCommand
{
	private ShipEjectionSystem _ejectSystem;

	public ShipCommand_CockpitEject()
	{
		_ejectSystem = SELocator.GetShipTransform().GetComponentInChildren<ShipEjectionSystem>();
	}

	public override string GetDisplayName() => "Eject Cockpit";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Modules;

	public override bool CanShow() => _ejectSystem != null;

	public override bool CanActivate() => !_ejectSystem._cockpitModule.isDetached;

	public override void Activate()
	{
		_ejectSystem._ejectPressed = true;
		_ejectSystem.enabled = true;
	}
}

public class ShipCommand_EngineEject : ShipCommand
{
	private ShipModuleEjectionSystem _ejectSystem;

	public ShipCommand_EngineEject()
	{
		if ((bool)extraEjectButtons.GetProperty())
		{
			_ejectSystem = SELocator.GetShipTransform().GetComponentsInChildren<ShipModuleEjectionSystem>()
				.First(e => e.GetEjectType() == ShipModuleEjectionSystem.EjectableModule.Engine);
		}
	}

	public override string GetDisplayName() => "Eject Engine";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Modules;

	public override bool CanShow() => _ejectSystem != null;

	public override bool CanActivate() => _ejectSystem.CanEject();

	public override void Activate()
	{
		_ejectSystem.Eject();
	}
}

public class ShipCommand_SuppliesEject : ShipCommand
{
	private ShipModuleEjectionSystem _ejectSystem;

	public ShipCommand_SuppliesEject()
	{
		if ((bool)extraEjectButtons.GetProperty())
		{
			_ejectSystem = SELocator.GetShipTransform().GetComponentsInChildren<ShipModuleEjectionSystem>()
				.First(e => e.GetEjectType() == ShipModuleEjectionSystem.EjectableModule.Supplies);
		}
	}

	public override string GetDisplayName() => "Eject Supplies";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Modules;

	public override bool CanShow() => _ejectSystem != null;

	public override bool CanActivate() => _ejectSystem.CanEject();

	public override void Activate()
	{
		_ejectSystem.Eject();
	}
}

public class ShipCommand_LandingGearEject : ShipCommand
{
	private ShipModuleEjectionSystem _ejectSystem;

	public ShipCommand_LandingGearEject()
	{
		if ((bool)extraEjectButtons.GetProperty())
		{
			_ejectSystem = SELocator.GetShipTransform().GetComponentsInChildren<ShipModuleEjectionSystem>()
				.First(e => e.GetEjectType() == ShipModuleEjectionSystem.EjectableModule.LandingGear);
		}
	}

	public override string GetDisplayName() => "Detach Landing Gear";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Modules;

	public override bool CanShow() => _ejectSystem != null;

	public override bool CanActivate() => _ejectSystem.CanEject();

	public override void Activate()
	{
		_ejectSystem.Eject();
	}
}

public class ShipCommand_RemoveTarget : ShipCommand
{
	public override string GetDisplayName() => "Remove Target";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.LockOn;

	public override bool CanShow() => true;

	public override bool CanActivate() => SELocator.GetReferenceFrame() != null;

	public override void Activate()
	{
		if ((bool)splitLockOn.GetProperty() || SELocator.IsShipTargetingPlayer() || 
			SELocator.IsShipTargetingProbe())
		{
			Locator.GetPlayerAudioController().PlayLockOff();
		}
		
		SELocator.SetShipReferenceFrame(null);
	}
}

public class ShipCommand_TargetPlayerPlanet : ShipCommand
{
	public override string GetDisplayName() => "Target My Planet";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.LockOn;

	public override bool CanShow() => true;

	public override bool CanActivate() => Locator.GetPlayerSectorDetector().GetPassiveReferenceFrame() != null &&
		SELocator.GetReferenceFrame() != Locator.GetPlayerSectorDetector().GetPassiveReferenceFrame();

	public override void Activate()
	{
		SELocator.SetShipReferenceFrame(Locator.GetPlayerSectorDetector().GetPassiveReferenceFrame());
		
		if ((bool)splitLockOn.GetProperty())
		{
			Locator.GetPlayerAudioController().PlayLockOn();
		}
	}
}

public class ShipCommand_TargetCurrentLockOn : ShipCommand
{
	public override string GetDisplayName() => "Target Current Lock-on";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.LockOn;

	public override bool CanShow() => (bool)splitLockOn.GetProperty();

	public override bool CanActivate() => Locator.GetReferenceFrame() != null && 
		SELocator.GetReferenceFrame() != Locator.GetReferenceFrame();

	public override void Activate()
	{
		SELocator.SetShipReferenceFrame(Locator.GetReferenceFrame());
		
		if ((bool)splitLockOn.GetProperty())
		{
			Locator.GetPlayerAudioController().PlayLockOn();
		}
	}
}

public class ShipCommand_TargetPlayer : ShipCommand
{
	public override string GetDisplayName() => "Target Me";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.LockOn;

	public override bool CanShow() => !ShipEnhancements.InMultiplayer;

	public override bool CanActivate() => !SELocator.IsShipTargetingPlayer();

	public override void Activate()
	{
		if (!(bool)splitLockOn.GetProperty())
		{
			Locator._rfTracker.UntargetReferenceFrame(false);
		}
		
		SELocator.TargetPlayerWithShip();
		Locator.GetPlayerAudioController().PlayLockOn();
	}
}

public class ShipCommand_TargetProbe : ShipCommand
{
	public override string GetDisplayName() => "Target Scout";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.LockOn;

	public override bool CanShow() => !ShipEnhancements.InMultiplayer;

	public override bool CanActivate() => !SELocator.IsShipTargetingProbe();

	public override void Activate()
	{
		if (!(bool)splitLockOn.GetProperty())
		{
			Locator._rfTracker.UntargetReferenceFrame(false);
		}
		
		SELocator.TargetProbeWithShip();
		Locator.GetPlayerAudioController().PlayLockOn();
	}
}

public class ShipCommand_CallErnesto : ShipCommand
{
	private CharacterDialogueTree _dialogue;
	private ErnestoCallController _callController;
	
	public override string GetDisplayName() => "Call Ernesto";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Misc;

	public override bool CanShow() => _dialogue != null;
	
	public override bool CanActivate() => 
		!_dialogue.InConversation() && !_callController.InRemoteConversation();

	public override void Activate()
	{
		if (SELocator.GetRemoteControl().IsVisible())
		{
			SELocator.GetRemoteControl().SetVisible(false);
		}
		
		if (Vector3.Distance(Locator.GetPlayerTransform().position, SELocator.GetShipTransform().position) < 10f)
		{
			DialogueConditionManager.SharedInstance
				.SetConditionState("SE_ERNESTO_CLOSECALL", true);
		}
		else if (Random.value < 0.1f)
		{
			DialogueConditionManager.SharedInstance
				.SetConditionState("SE_ERNESTO_LONGPICKUP", true);
		}
		
		_dialogue.StartConversation();
	}

	public void AssignDialogue(CharacterDialogueTree dialogueTree)
	{
		_dialogue = dialogueTree;
		_callController = _dialogue.transform.parent.GetComponent<ErnestoCallController>();
	}
}

public class ShipCommand_ViewShip : ShipCommand
{
	public override string GetDisplayName() => "Enter Ship Viewer";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Misc;

	public override bool CanShow() => true;

	public override bool CanActivate() => true;

	public override void Activate()
	{
		SELocator.GetRemoteControl().EnterShipViewerMode();
	}
}