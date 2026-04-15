using System.Linq;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public abstract class ShipCommand
{
	public enum CommandGroup
	{
		Modules,
		Reactor,
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

	public override CommandGroup GetCommandGroup() => CommandGroup.Reactor;

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
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Reactor;

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
	
	public ShipCommand_Autopilot()
	{
		_autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
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
		return _autopilot.enabled || (referenceFrame != null && referenceFrame.GetAllowAutopilot() && 
			(PlayerData.GetAutopilotEnabled() || (bool)enableEnhancedAutopilot.GetProperty()) && 
			Vector3.Distance(SELocator.GetShipBody().GetPosition(), referenceFrame.GetPosition()) > 
			referenceFrame.GetAutopilotArrivalDistance());
	}

	public override void Activate()
	{
		if ((bool)enableEnhancedAutopilot.GetProperty() && 
			!SELocator.GetAutopilotPanelController().IsApproachSelected())
		{
			SELocator.GetAutopilotPanelController().SetAutopilotMode(false);
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
	
	public ShipCommand_OrbitAutopilot()
	{
		_autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
		if ((bool)enableEnhancedAutopilot.GetProperty())
		{
			_pidAutopilot = SELocator.GetShipBody().GetComponent<PidAutopilot>();
		}
	}

	public override string GetDisplayName()
	{
		return (_pidAutopilot.enabled && _pidAutopilot.GetCurrentMode() == PidMode.Orbit) ? 
			"Disengage Orbital Autopilot" : "Activate Orbital Autopilot";
	}
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Autopilot;

	public override bool CanShow() => _autopilot != null && _pidAutopilot != null;

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
		if (!SELocator.GetAutopilotPanelController().IsOrbitSelected())
		{
			SELocator.GetAutopilotPanelController().SetAutopilotMode(true);
		}

		if (!_pidAutopilot.enabled)
		{
			SELocator.GetAutopilotPanelController().ActivateAutopilot();
		}
		else
		{
			SELocator.GetAutopilotPanelController().CancelAutopilot();
		}
	}
}

public class ShipCommand_MatchVelocity : ShipCommand
{
	private Autopilot _autopilot;
	
	public ShipCommand_MatchVelocity()
	{
		_autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
	}

	public override string GetDisplayName()
	{
		return _autopilot.IsMatchingVelocity() ? "Disengage Match Velocity" : "Activate Match Velocity";
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

		if ((bool)enableEnhancedAutopilot.GetProperty() && 
			SELocator.GetAutopilotPanelController().IsAutopilotActive(true, false))
		{
			return false;
		}

		return !_autopilot.IsFlyingToDestination() && 
			SELocator.GetReferenceFrame(ignorePassiveFrame: false) != null;
	}

	public override void Activate()
	{
		if ((bool)enableEnhancedAutopilot.GetProperty())
		{
			if (!SELocator.GetAutopilotPanelController().IsMatchVelocitySelected())
			{
				SELocator.GetAutopilotPanelController().SetMatchMode(false);
			}
			
			if (_autopilot.IsMatchingVelocity())
			{
				SELocator.GetAutopilotPanelController().CancelMatchVelocity();
			}
			else
			{
				SELocator.GetAutopilotPanelController().ActivateMatchVelocity();
			}

			return;
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
	
	public ShipCommand_HoldPosition()
	{
		_autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
		if ((bool)enableEnhancedAutopilot.GetProperty())
		{
			_pidAutopilot = SELocator.GetShipBody().GetComponent<PidAutopilot>();
		}
	}

	public override string GetDisplayName()
	{
		return (_pidAutopilot.enabled && _pidAutopilot.GetCurrentMode() == PidMode.HoldPosition) ? 
			"Disengage Position Hold" : "Activate Position Hold";
	}
	
	public override CommandGroup GetCommandGroup() => CommandGroup.Autopilot;

	public override bool CanShow() => _autopilot != null && _pidAutopilot != null;

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
		if (!SELocator.GetAutopilotPanelController().IsHoldPositionSelected())
		{
			SELocator.GetAutopilotPanelController().SetMatchMode(true);
		}

		if (!_pidAutopilot.enabled)
		{
			SELocator.GetAutopilotPanelController().ActivateMatchVelocity();
		}
		else
		{
			SELocator.GetAutopilotPanelController().CancelMatchVelocity();
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