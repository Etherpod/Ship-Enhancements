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
            var force = base.CalculateForceAccelerationOnBody(targetBody) * 100f;
            ShipEnhancements.WriteDebugMessage(force.x);
            ShipEnhancements.WriteDebugMessage(force.y);
            ShipEnhancements.WriteDebugMessage(force.z + "\n");
            return force;
        }
        return base.CalculateForceAccelerationOnBody(targetBody);
    }

    public override Vector3 CalculateForceAccelerationAtPoint(Vector3 worldPos)
    {
        return base.CalculateForceAccelerationAtPoint(worldPos);
    }
}
