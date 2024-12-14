using UnityEngine;

namespace ShipEnhancements;

public class SignalscopeComponent : ShipComponent
{
    private float _dishT;
    private Vector3 _startRotation;

    private void Start()
    {
        _componentName = ShipEnhancements.Instance.SignalscopeName;
    }

    private void FixedUpdate()
    {
        if (_dishT < 1f)
        {
            _dishT = Mathf.Clamp01(_dishT + Time.deltaTime / 0.8f);
            transform.parent.localEulerAngles = new Vector3(Mathf.SmoothStep(_startRotation.x, 0f, _dishT), 0f, 0f);
        }
        else
        {
            enabled = false;
        }
    }

    public override void OnComponentDamaged()
    {
        enabled = false;
        _startRotation = transform.parent.localEulerAngles;
        _dishT = 0f;
    }

    public override void OnComponentRepaired()
    {
        if (_startRotation.x > 0f)
        {
            enabled = true;
        }
    }
}
