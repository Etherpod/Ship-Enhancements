using UnityEngine;

namespace ShipEnhancements;

public class StaticFluidDetector : FluidDetector
{
    // Basically overrides original
    private void OnValidate()
    {
        if (!_dontApplyForces)
        {
            _dontApplyForces = true;
        }
    }

    public override void Awake()
    {
        _dontApplyForces = true;
        base.Awake();
    }

    public override float CalculateDragFactor(Vector3 relativeFluidVelocity)
    {
        return 0f;
    }

    public override float CalculateAngularDragFactor(Vector3 relativeAngularVelocity)
    {
        return 0f;
    }

    public override void ManagedFixedUpdate()
    {
    }
}
