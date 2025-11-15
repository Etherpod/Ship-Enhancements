using UnityEngine;

namespace ShipEnhancements;

public class ResourcePumpSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.ResourcePumpType;
    }
}
