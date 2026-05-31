using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorSelectionData : MonoBehaviour
{
	[SerializeField]
	private string _displayName;
	[SerializeField]
	private DecoratorInterfaceOption.InterfaceOptionType[] _linkedOptions;

	public string GetDisplayName() => _displayName;

	public DecoratorInterfaceOption.InterfaceOptionType[] GetOptionsToDisplay() => _linkedOptions;
}