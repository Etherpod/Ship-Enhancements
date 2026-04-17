using System.Linq;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public abstract class ShipCommand
{
	public enum CommandGroup
	{
		Modules,
		Components,
		Autopilot,
		LockOn
	}
	
	public abstract string GetDisplayName();

	public abstract CommandGroup GetCommandGroup();

	public abstract bool CanShow();

	public abstract bool CanActivate();
	
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

	public override void Activate()
	{
		_hornController.PlayHorn();
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
			}
		}
		else
		{
			_autopilot.Abort();
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
	}
}

public class ShipCommand_TargetCurrentLockOn : ShipCommand
{
	public override string GetDisplayName() => "Target Current Lock-on";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.LockOn;

	public override bool CanShow() => (bool)splitLockOn.GetProperty();

	public override bool CanActivate() => SELocator.GetReferenceFrame() != Locator.GetReferenceFrame();

	public override void Activate()
	{
		SELocator.SetShipReferenceFrame(Locator.GetReferenceFrame());
	}
}

public class ShipCommand_TargetPlayer : ShipCommand
{
	public override string GetDisplayName() => "Target Me";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.LockOn;

	public override bool CanShow() => true;

	public override bool CanActivate() => !SELocator.IsShipTargetingPlayer();

	public override void Activate()
	{
		Locator._rfTracker.UntargetReferenceFrame(false);
		SELocator.TargetPlayerWithShip();
		Locator.GetPlayerAudioController().PlayLockOn();
	}
}

public class ShipCommand_TargetProbe : ShipCommand
{
	public override string GetDisplayName() => "Target Scout";
	
	public override CommandGroup GetCommandGroup() => CommandGroup.LockOn;

	public override bool CanShow() => true;

	public override bool CanActivate() => !SELocator.IsShipTargetingProbe();

	public override void Activate()
	{
		Locator._rfTracker.UntargetReferenceFrame(false);
		SELocator.TargetProbeWithShip();
		Locator.GetPlayerAudioController().PlayLockOn();
	}
}