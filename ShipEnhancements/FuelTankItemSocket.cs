namespace ShipEnhancements;

public class FuelTankItemSocket : OWItemSocket
{
    public override void Awake()
    {
        Reset();
        _sector = SELocator.GetShipSector();
        base.Awake();
        _acceptableType = FuelTankItem.ItemType;
    }
}
