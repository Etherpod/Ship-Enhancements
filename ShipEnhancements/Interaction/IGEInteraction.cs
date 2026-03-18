using UnityEngine;

namespace ShipEnhancements.Interaction;

public interface IGEInteraction
{
    public bool IsContinuousMatchVelocityEnabled();
    public void StopContinuousMatchVelocity();
    public void EnableContinuousMatchVelocity();
}
