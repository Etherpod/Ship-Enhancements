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

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
    }

    public void SetCampfireItem(PortableCampfireItem item)
    {
        _campfireItem = item;
        PlaceIntoSocket(item);
    }

    private void OnShipSystemFailure()
    {
        _sector = null;
        _socketedItem?.SetSector(null);
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
