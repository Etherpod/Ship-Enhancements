using UnityEngine;

namespace ShipEnhancements;

public class StaticFluidDetector : FluidDetector
{
    [SerializeField]
    private bool _ignoreChildVolumes = false;

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

    public override void AddVolume(EffectVolume eVol)
    {
        ShipEnhancements.WriteDebugMessage(eVol + " is child of " + _splashSpawnRoot + ": " + eVol.transform.IsChildOf(_splashSpawnRoot));
        if (_ignoreChildVolumes && eVol.transform.IsChildOf(_splashSpawnRoot))
        {
            return;
        }

        base.AddVolume(eVol);
    }

    public override void OnEnterFluidType_Internal(FluidVolume fluid)
    {

    }

    public override void OnExitFluidType_Internal(FluidVolume fluid)
    {

    }
}
