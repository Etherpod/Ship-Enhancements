namespace ShipEnhancements.Items;

public class RepairWrenchSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.RepairWrenchType;
    }
}
