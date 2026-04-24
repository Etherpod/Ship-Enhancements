namespace ShipEnhancements.Items;

public class ExpeditionFlagSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.ExpeditionFlagType;
    }
}
