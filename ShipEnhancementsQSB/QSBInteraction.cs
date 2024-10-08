using UnityEngine;
using QSB.ShipSync;

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
}
