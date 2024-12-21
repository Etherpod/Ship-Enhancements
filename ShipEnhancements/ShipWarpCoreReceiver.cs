using UnityEngine;

namespace ShipEnhancements;

public class ShipWarpCoreReceiver : MonoBehaviour
{
    [SerializeField]
    private Transform _warpPosition;

    public Transform GetWarpPosition()
    {
        return _warpPosition;
    }
}
