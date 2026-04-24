using System;

namespace ShipEnhancements.Items;

public class RadioItemSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.RadioType;
    }
}
