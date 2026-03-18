using UnityEngine;

namespace ShipEnhancements.Items;

public class ResourcePumpSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.ResourcePumpType;
    }
}
