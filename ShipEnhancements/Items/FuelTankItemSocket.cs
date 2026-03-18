namespace ShipEnhancements.Items;

public class FuelTankItemSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.FuelTankType;
    }
}
