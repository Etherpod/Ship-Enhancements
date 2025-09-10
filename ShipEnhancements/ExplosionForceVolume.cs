using UnityEngine;

namespace ShipEnhancements;

public class ExplosionForceVolume : RadialForceVolume
{
    public override bool GetAffectsAlignment(OWRigidbody targetBody)
    {
        return false;
    }

    public override Vector3 CalculateForceAccelerationOnBody(OWRigidbody targetBody)
    {
        if (targetBody is ShipBody)
        {
            return base.CalculateForceAccelerationAtPoint(targetBody.GetWorldCenterOfMass());
        }
        return base.CalculateForceAccelerationOnBody(targetBody);
    }
}
