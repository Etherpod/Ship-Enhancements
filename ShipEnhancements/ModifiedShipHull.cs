using System;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ModifiedShipHull : MonoBehaviour
{
    private float _shipHeatDamage = 10f;
    private HazardVolume _tempHazardVolume;

    private void Start()
    {
        if ((float)shipBounciness.GetProperty() > 1f)
        {
            SELocator.GetShipDamageController()._impactSensor.OnImpact += OnImpact;
        }
        if ((string)temperatureZonesAmount.GetProperty() != "None")
        {
            GameObject obj = new GameObject("ShipHullHeatHazard");
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;
            _tempHazardVolume = obj.GetAddComponent<HeatHazardVolume>();

            GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        }
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

    private void OnCollisionEnter(Collision collision)
    {
        if (_tempHazardVolume != null && collision.collider.GetAttachedOWRigidbody().CompareTag("Player") && !PlayerState.IsInsideShip())
        {
            float ratio = SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio();
            if (ratio > 0.75f)
            {
                float lerp = Mathf.InverseLerp(0.75f, 1f, ratio);
                _tempHazardVolume._damagePerSecond = Mathf.Lerp(0f, _shipHeatDamage, lerp);
                Locator.GetPlayerDetector().GetComponent<HazardDetector>().AddVolume(_tempHazardVolume);
                ShipEnhancements.WriteDebugMessage(Locator.GetPlayerDetector().GetComponent<HazardDetector>()._activeVolumes[0]);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (_tempHazardVolume != null && collision.collider.GetAttachedOWRigidbody().CompareTag("Player") && !PlayerState.IsInsideShip())
        {
            Locator.GetPlayerDetector().GetComponent<HazardDetector>().RemoveVolume(_tempHazardVolume);
        }
    }

    private void OnShipSystemFailure()
    {
        Locator.GetPlayerDetector().GetComponent<HazardDetector>().RemoveVolume(_tempHazardVolume);
    }

    private void OnDestroy()
    {
        if ((float)shipBounciness.GetProperty() > 1f)
        {
            SELocator.GetShipDamageController()._impactSensor.OnImpact -= OnImpact;
        }
        if ((string)temperatureZonesAmount.GetProperty() != "None")
        {
            GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        }
    }
}
