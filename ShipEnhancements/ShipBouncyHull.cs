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
        Vector3 direction = Vector3.Reflect(body.GetVelocity(), impact.normal).normalized;

        body.AddImpulse(direction * impact.speed * (float)shipBounciness.GetProperty());

        Vector3 impactDirection = impact.point - body.GetWorldCenterOfMass();

        body.AddTorque(Vector3.Cross(impactDirection, direction).normalized * impact.speed * (float)shipBounciness.GetProperty() * impactDirection.magnitude);
    }

    private void OnDestroy()
    {
        SELocator.GetShipDamageController()._impactSensor.OnImpact -= OnImpact;
    }
}
