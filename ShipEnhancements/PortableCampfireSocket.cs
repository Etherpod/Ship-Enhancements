namespace ShipEnhancements;

public class PortableCampfireSocket : OWItemSocket
{
    private PortableCampfireItem _campfireItem;

    public override void Awake()
    {
        Reset();
        _sector = SELocator.GetShipSector();
        base.Awake();
        _acceptableType = PortableCampfireItem.ItemType;
    }

    public void SetCampfireItem(PortableCampfireItem item)
    {
        _campfireItem = item;
        PlaceIntoSocket(item);
    }
}
