using UnityEngine;
using QSB.ShipSync;
using QSB.ShipSync.TransformSync;
using QSB.Player;
using QSB.TimeSync;

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
        return ShipTransformSync.LocalInstance?.ThrusterVariableSyncer?.AccelerationSyncer?.Value
            ?? Vector3.zero;
    }

    public int GetPlayersInShip()
    {
        int num = 0;

        foreach (uint id in ShipEnhancements.ShipEnhancements.QSBAPI.GetPlayerIDs())
        {
            if (QSBPlayerManager.GetPlayer(id).IsInShip)
            {
                num++;
            }
        }

        return num;
    }
}
