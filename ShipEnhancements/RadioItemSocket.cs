using System;

namespace ShipEnhancements;

public class RadioItemSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.RadioType;
    }
}
