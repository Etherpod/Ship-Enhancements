namespace ShipEnhancements;

public class FuelTankItemSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.FuelTankType;
    }
}
