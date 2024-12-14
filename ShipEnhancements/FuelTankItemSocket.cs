namespace ShipEnhancements;

public class FuelTankItemSocket : OWItemSocket
{
    public override void Awake()
    {
        Reset();
        _sector = SELocator.GetShipBody().GetComponent<Sector>();
        base.Awake();
        _acceptableType = FuelTankItem.ItemType;
    }
}
