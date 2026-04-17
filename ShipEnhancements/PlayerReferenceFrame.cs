namespace ShipEnhancements;

public class PlayerReferenceFrame : ReferenceFrame
{
	public PlayerReferenceFrame(OWRigidbody attachedOWRigidbody) : base(attachedOWRigidbody)
	{
		_useCenterOfMass = true;
		_minSuitTargetDistance = 0f;
		_maxTargetDistance = 0f;
		_autopilotArrivalDistance = 20f;
		_autoAlignmentDistance = 0f;
		_bracketsRadius = 0f;
	}
	
	public new string GetHUDDisplayName_New()
	{
		return "Player";
	}
}