using UnityEngine;

namespace ShipEnhancements;

public interface IGEInteraction
{
    public bool IsContinuousMatchVelocityEnabled();
    public void StopContinuousMatchVelocity();
    public void EnableContinuousMatchVelocity();
}
