namespace ShipEnhancements.Items;

public class TetherHookSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.TetherHookType;
    }
}
