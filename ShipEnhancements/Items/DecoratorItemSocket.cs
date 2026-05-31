namespace ShipEnhancements.Items;

public class DecoratorItemSocket : SEItemSocket
{
	protected override ItemType GetAcceptableType()
	{
		return ShipEnhancements.Instance.DecoratorType;
	}
}