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
        Vector3 velocity = impact.otherBody.GetPointVelocity(impact.point) - body.GetPointVelocity(impact.point);
        float speed = Vector3.Project(-velocity, impact.normal).magnitude * (float)shipBounciness.GetProperty();
        Vector3 bounceDirection = Vector3.Reflect(-velocity, impact.normal).normalized;

        body.AddImpulse(bounceDirection * speed * 0.8f);

        Vector3 toImpactPoint = impact.point - body.GetWorldCenterOfMass();

        body.AddTorque(Vector3.Cross(toImpactPoint, bounceDirection).normalized * speed * toImpactPoint.magnitude);
    }

    private void OnDestroy()
    {
        SELocator.GetShipDamageController()._impactSensor.OnImpact -= OnImpact;
    }
}
