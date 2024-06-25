using System;
using UnityEngine;

namespace ShipEnhancements;

public class TemperatureZone : MonoBehaviour
{
    [SerializeField]
    [Range(-100f, 100f)]
    private float _temperature;
    [SerializeField]
    private float _innerRadius;

    private OWTriggerVolume _triggerVolume;
    private bool _zoneOccupied;
    private float _outerRadius;

    private void Start()
    {
        _outerRadius = GetComponent<SphereCollider>().radius;
        _triggerVolume = GetComponent<OWTriggerVolume>();
        _triggerVolume.OnEntry += OnEffectVolumeEnter;
        _triggerVolume.OnExit += OnEffectVolumeExit;
    }

    private void OnEffectVolumeEnter(GameObject hitObj)
    {
        if (hitObj.TryGetComponent(out ShipTemperatureDetector detector))
        {
            detector.AddZone(this);
            _zoneOccupied = true;
        }
    }

    private void OnEffectVolumeExit(GameObject hitObj)
    {
        if (hitObj.TryGetComponent(out ShipTemperatureDetector detector))
        {
            detector.RemoveZone(this);
            _zoneOccupied = false;
        }
    }

    public float GetTemperature()
    {
        float distSqr = (Locator.GetShipDetector().transform.position - transform.position).sqrMagnitude;
        float falloffMultiplier = Mathf.InverseLerp(_outerRadius * _outerRadius, _innerRadius * _innerRadius, distSqr);
        return _temperature * falloffMultiplier;
    }
}
