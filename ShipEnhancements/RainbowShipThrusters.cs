using UnityEngine;

public class RainbowShipThrusters : MonoBehaviour
{
    private MeshRenderer _rend;
    private Material _thrustMat;
    private Light _light;
    private float _colorTransitionTime = 6f;
    private float _red;
    private float _green;
    private float _blue;
    private int _index;
    private float _lastDelta;

    public static Color currentColor;

    private void Start()
    {
        _rend = GetComponent<MeshRenderer>();
        _thrustMat = _rend?.material;
        _light = GetComponentInChildren<Light>();
        _red = 1f;
        _green = 0f;
        _blue = 0f;
    }

    private void FixedUpdate()
    {
        if (!_thrustMat)
        {
            return;
        }

        float num = Mathf.InverseLerp(0f, _colorTransitionTime, Time.time % _colorTransitionTime);

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
        Color prevColor = _thrustMat.GetColor("_Color");
        Color thrusterColor = color * (_red * 2f + _green * 3f + _blue * 7f);
        thrusterColor.a = prevColor.a;
        _thrustMat.SetColor("_Color", thrusterColor);
        if (_light)
        {
            _light.color = color;
        }

        currentColor = thrusterColor;
        _lastDelta = num;
    }
}
