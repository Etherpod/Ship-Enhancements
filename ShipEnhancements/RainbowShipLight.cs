using System;
using UnityEngine;

namespace ShipEnhancements;

public class RainbowShipLight : MonoBehaviour
{
    private ShipLight _shipLight;
    private PulsingLight _pulsingLight;
    private float _colorTransitionTime = 8f;
    //private float _transitionT;
    private float _red;
    private float _green;
    private float _blue;

    private void Start()
    {
        _shipLight = GetComponent<ShipLight>();
        _pulsingLight = GetComponent<PulsingLight>();
        _red = 1f;
        _green = 0f;
        _blue = 0f;
    }

    private void Update()
    {
        if (_shipLight)
        {
            float delta = Time.deltaTime / (_colorTransitionTime / 3);
            if (_red >= 1f && _green < 1f)
            {
                _green += delta;
            }
            else if (_green >= 1f && _red > 0f)
            {
                _red -= delta;
            }
            else if (_green >= 1f && _blue < 1f)
            {
                _blue += delta;
            }
            else if (_blue >= 1f && _green > 0f)
            {
                _green -= delta;
            }
            else if (_blue >= 1f && _red < 1f)
            {
                _red += delta;
            }
            else if (_red >= 1f && _blue > 0f)
            {
                _blue -= delta;
            }
            Color color = new Color(_red, _green, _blue);
            _shipLight._baseEmission = color;
            if (_shipLight._light.intensity == _shipLight._baseIntensity)
            {
                _shipLight._matPropBlock.SetColor(_shipLight._propID_EmissionColor, color);
                _shipLight._emissiveRenderer.SetPropertyBlock(_shipLight._matPropBlock);
            }
            _shipLight._light.color = color;
        }
    }
}
