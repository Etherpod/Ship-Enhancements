namespace ShipEnhancements;

public class ExpeditionFlagSocket : OWItemSocket
{
    public override void Awake()
    {
        Reset();
        _sector = SELocator.GetShipBody().GetComponent<Sector>();
        base.Awake();
        _acceptableType = ExpeditionFlagItem.ItemType;
    }
}
