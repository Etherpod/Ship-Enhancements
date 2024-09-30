using System;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipBouncyHull : MonoBehaviour
{
    private void Start()
    {
        SELocator.GetShipDamageController()._impactSensor.OnImpact += OnImpact;
    }

    private void OnImpact(ImpactData impact)
    {
        if (impact.otherCollider.CompareTag("Player"))
        {
            return;
        }

        OWRigidbody body = GetComponent<ShipBody>();
        //Vector3 velocity = impact.otherBody.GetPointVelocity(impact.point) - body.GetPointVelocity(impact.point);
        Vector3 collisionVelocity = body.GetPointTangentialVelocity(impact.point) - impact.otherBody.GetPointTangentialVelocity(impact.point);
        //Vector3 collisionVelocity = body.GetVelocity() - impact.otherBody.GetVelocity();
        Vector3 directSpeed = Vector3.Project(collisionVelocity, impact.normal);
        Vector3 direction = Vector3.Reflect(collisionVelocity, impact.normal);

        //body.AddImpulse(direction * impact.speed * (float)shipBounciness.GetProperty());
        //body.AddImpulse(direction.normalized * directSpeed.magnitude * (float)shipBounciness.GetProperty());
        body.AddImpulse(direction.normalized * impact.speed * (float)shipBounciness.GetProperty());

        Vector3 comDist = impact.point - body.GetWorldCenterOfMass();

        //body.AddTorque(Vector3.Cross(impactDirection, direction).normalized * impact.speed * (float)shipBounciness.GetProperty() * impactDirection.magnitude);
        //body.AddTorque(Vector3.Cross(comDist, direction).normalized * directSpeed.magnitude * (float)shipBounciness.GetProperty() * comDist.magnitude);
        body.AddTorque(Vector3.Cross(comDist, direction).normalized * impact.speed * (float)shipBounciness.GetProperty() * comDist.magnitude);
    }

    private void OnDestroy()
    {
        SELocator.GetShipDamageController()._impactSensor.OnImpact -= OnImpact;
    }
}
