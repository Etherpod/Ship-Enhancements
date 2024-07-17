using System;

namespace ShipEnhancements;

public class PortableCampfireSocket : OWItemSocket
{
    private PortableCampfireItem _campfireItem;

    public override void Awake()
    {
        base.Awake();
        _acceptableType = PortableCampfireItem.ItemType;
    }

    public void SetCampfireItem(PortableCampfireItem item)
    {
        _campfireItem = item;
        PlaceIntoSocket(item);
    }
}
