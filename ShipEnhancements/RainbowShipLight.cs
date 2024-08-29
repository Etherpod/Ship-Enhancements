using System;
using UnityEngine;

namespace ShipEnhancements;

public class RainbowShipLight : MonoBehaviour
{
    private ShipLight _shipLight;
    private PulsingLight _pulsingLight;
    private float _colorTransitionTime = 4f;
    //private float _transitionT;
    private float _red;
    private float _green;
    private float _blue;
    private int _index;
    private float _lastDelta;

    private void Start()
    {
        _shipLight = GetComponent<ShipLight>();
        _pulsingLight = GetComponent<PulsingLight>();
        _red = 1f;
        _green = 0f;
        _blue = 0f;
    }

    private void FixedUpdate()
    {
        if ((_shipLight && !_shipLight.IsOn()) || (_pulsingLight && !_pulsingLight.enabled))
        {
            return;
        }

        float num = Mathf.InverseLerp(0f, _colorTransitionTime, Time.time % (_colorTransitionTime));

        if (_lastDelta > num)
        {
            _index++;
            if (_index > 5) _index = 0;
        }

        if (_index == 0)
        {
            _green = Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 1)
        {
            _red = 1 - Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 2)
        {
            _blue = Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 3)
        {
            _green = 1 - num;
        }
        else if (_index == 4)
        {
            _red = Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 5)
        {
            _blue = 1 - Mathf.Lerp(0f, 1f, num);
        }
        Color color = new Color(_red, _green, _blue);

        if (_shipLight)
        {
            _shipLight._baseEmission = color;
            if (_shipLight._light.intensity == _shipLight._baseIntensity)
            {
                _shipLight._matPropBlock.SetColor(_shipLight._propID_EmissionColor, color);
                _shipLight._emissiveRenderer.SetPropertyBlock(_shipLight._matPropBlock);
            }
            _shipLight._light.color = color;
        }
        else if (_pulsingLight)
        {
            _pulsingLight._initEmissionColor = color;
            _pulsingLight._light.color = color;
        }

        _lastDelta = num;
    }
}
