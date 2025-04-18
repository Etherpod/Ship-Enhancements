namespace ShipEnhancements;

public class ExpeditionFlagSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.ExpeditionFlagType;
    }
}
