using System;
using System.Reflection;
using UnityEngine;

namespace ShipEnhancements;

public class HeliostudyIceTempZone : DayNightTemperatureZone
{
    [Space]
    [SerializeField]
    private bool _isSmallPlanet;

    private Type _iceSphere;
    private object _iceSphereObj;
    private MethodInfo _isIceFull;

    private bool _lastEnabledState;

    protected override void Awake()
    {
        base.Awake();
        _iceSphere = Type.GetType("OWJam5ModProject.IceSphere, OWJam5ModProject");
        _iceSphereObj = transform.parent.GetComponentInChildren(_iceSphere);

        _isIceFull = _iceSphere.GetMethod("ReqMet", BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.Instance);

        _lastEnabledState = _isSmallPlanet;
        SetVolumeActive(_lastEnabledState);
    }

    private void Update()
    {
        _lastEnabledState = (bool)_isIceFull.Invoke(_iceSphereObj, []);

        if (_lastEnabledState != _active)
        {
            SetVolumeActive(_lastEnabledState);
        }
    }
}
