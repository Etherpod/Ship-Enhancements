using UnityEngine;

namespace ShipEnhancements;

public class ProbeReferenceFrame : ReferenceFrame
{
	public ProbeReferenceFrame(OWRigidbody attachedOWRigidbody) : base(attachedOWRigidbody)
	{
		_useCenterOfMass = true;
		_minSuitTargetDistance = 0f;
		_maxTargetDistance = 0f;
		_autopilotArrivalDistance = 20f;
		_autoAlignmentDistance = 0f;
		_bracketsRadius = 0f;
	}

	public string GetHUDDisplayName_New()
	{
		return "Scout";
	}

	public OWRigidbody GetOWRigidBody_New()
	{
		if (SELocator.GetProbe() == null)
		{
			return null;
		}

		if (!SELocator.GetProbe().IsLaunched())
		{
			return SELocator.GetPlayerBody();
		}

		if (SELocator.GetProbe().IsAnchored())
		{
			var body = _attachedOWRigidbody.transform.parent.GetAttachedOWRigidbody();
			if (body != null)
			{
				return body;
			}
		}

		return _attachedOWRigidbody;
	}

	public Vector3 GetPosition_New()
	{
		if (SELocator.GetProbe() == null)
		{
			return Vector3.zero;
		}

		if (!SELocator.GetProbe().IsLaunched())
		{
			return SELocator.GetPlayerBody().GetWorldCenterOfMass();
		}

		return _attachedOWRigidbody.GetWorldCenterOfMass();
	}

	public Vector3 GetVelocity_New()
	{
		if (SELocator.GetProbe() == null)
		{
			return Vector3.zero;
		}

		if (!SELocator.GetProbe().IsLaunched())
		{
			return SELocator.GetPlayerBody().GetVelocity();
		}

		if (SELocator.GetProbe().IsAnchored())
		{
			var body = _attachedOWRigidbody.transform.parent.GetAttachedOWRigidbody();
			if (body != null)
			{
				return body.GetPointVelocity(GetPosition_New());
			}
		}

		return _attachedOWRigidbody.GetVelocity();
	}

	public Vector3 GetAcceleration_New()
	{
		if (SELocator.GetProbe() == null)
		{
			return Vector3.zero;
		}

		if (!SELocator.GetProbe().IsLaunched())
		{
			return SELocator.GetPlayerBody().GetAcceleration();
		}
		
		if (SELocator.GetProbe().IsAnchored())
		{
			var body = _attachedOWRigidbody.transform.parent.GetAttachedOWRigidbody();
			if (body != null)
			{
				return body.GetPointAcceleration(GetPosition_New());
			}
		}

		return _attachedOWRigidbody.GetAcceleration();
	}
}