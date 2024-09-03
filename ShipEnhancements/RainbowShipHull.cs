using System.Collections.Generic;
using UnityEngine;

public class RainbowShipHull : MonoBehaviour
{
    private List<Material> _sharedMaterials = [];
    private float _colorTransitionTime = 6f;
    private float _red;
    private float _green;
    private float _blue;
    private int _index;
    private float _lastDelta;

    private void Start()
    {
        _red = 1f;
        _green = 0f;
        _blue = 0f;
    }

    private void FixedUpdate()
    {
        if (_sharedMaterials.Count == 0) return;

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

        foreach (Material mat in _sharedMaterials)
        {
            mat.SetColor("_Color", color);
        }

        _lastDelta = num;
    }

    public void AddSharedMaterial(Material mat)
    {
        _sharedMaterials.Add(mat);
    }
}
