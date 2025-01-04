namespace ShipEnhancements;

public class ExpeditionFlagSocket : OWItemSocket
{
    public override void Awake()
    {
        Reset();
        _sector = SELocator.GetShipSector();
        base.Awake();
        _acceptableType = ExpeditionFlagItem.ItemType;
    }
}
