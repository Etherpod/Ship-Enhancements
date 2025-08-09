using UnityEngine;

namespace ShipEnhancements;

public class ItemFluidDetector : PriorityDetector
{
    public delegate void FluidTypeEvent(FluidVolume.Type fluidType);
    public delegate void FluidEvent(FluidVolume volume);

    public event FluidTypeEvent OnEnterFluidType;
    public event FluidTypeEvent OnExitFluidType;
    public event FluidEvent OnEnterFluid;
    public event FluidEvent OnExitFluid;

    private FluidTypeData[] _fluidDataByType;

    public override void Awake()
    {
        base.Awake();
        _fluidDataByType = new FluidTypeData[9];
    }

    public bool InFluidType(FluidVolume.Type fluidType)
    {
        return _fluidDataByType[(int)fluidType].count > 0;
    }

    public override void AddVolume(EffectVolume eVol)
    {
        FluidVolume fluidVolume = eVol as FluidVolume;
        if (fluidVolume != null && !fluidVolume.IsInheritible())
        {
            base.AddVolume(eVol);
        }
    }

    public override void RemoveVolume(EffectVolume eVol)
    {
        FluidVolume fluidVolume = eVol as FluidVolume;
        if (fluidVolume != null && !fluidVolume.IsInheritible())
        {
            base.RemoveVolume(eVol);
        }
    }

    public override void OnVolumeActivated(PriorityVolume volume)
    {
        FluidVolume fluidVolume = volume as FluidVolume;
        FluidVolume.Type fluidType = fluidVolume.GetFluidType();
        FluidTypeData[] fluidDataByType = _fluidDataByType;
        FluidVolume.Type type = fluidType;
        fluidDataByType[(int)type].count = fluidDataByType[(int)type].count + 1;
        if (OnEnterFluid != null)
        {
            OnEnterFluid(fluidVolume);
        }
        if (_fluidDataByType[(int)fluidType].count == 1)
        {
            if (OnEnterFluidType != null)
            {
                OnEnterFluidType(fluidType);
            }
        }
    }

    public override void OnVolumeDeactivated(PriorityVolume volume)
    {
        FluidVolume fluidVolume = volume as FluidVolume;
        FluidVolume.Type fluidType = fluidVolume.GetFluidType();
        FluidTypeData[] fluidDataByType = _fluidDataByType;
        FluidVolume.Type type = fluidType;
        fluidDataByType[(int)type].count = fluidDataByType[(int)type].count - 1;
        if (OnExitFluid != null)
        {
            OnExitFluid(fluidVolume);
        }
        if (_fluidDataByType[(int)fluidType].count == 0)
        {
            if (OnExitFluidType != null)
            {
                OnExitFluidType(fluidType);
            }
        }
    }
}
