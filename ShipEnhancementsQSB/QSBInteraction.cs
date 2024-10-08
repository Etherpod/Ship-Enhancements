using UnityEngine;
using QSB.ShipSync;
using QSB.ShipSync.TransformSync;

namespace ShipEnhancementsQSB;

public class QSBInteraction : MonoBehaviour, IQSBInteraction
{
    private void Start()
    {
        ShipEnhancements.ShipEnhancements.Instance.AssignQSBInterface(this);
    }
    
    public bool FlightConsoleOccupied()
    {
        return ShipManager.Instance.CurrentFlyer != uint.MaxValue;
    }

    public Vector3 GetShipAcceleration()
    {
        return ShipTransformSync.LocalInstance.ThrusterVariableSyncer.AccelerationSyncer.Value;
    }
}
