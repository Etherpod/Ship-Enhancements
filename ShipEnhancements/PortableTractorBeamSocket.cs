namespace ShipEnhancements;

public class PortableTractorBeamSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.PortableTractorBeamType;
    }
}
