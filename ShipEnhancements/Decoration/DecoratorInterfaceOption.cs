using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShipEnhancements.Decoration;

public class DecoratorInterfaceOption : DecoratorInterfaceElement
{
	[SerializeField]
	private InterfaceOptionType _optionType;
	[SerializeField]
	private DecoratorInterfaceMode _linkedMode;

	public enum InterfaceOptionType
	{
		Color,
		HullColor,
		WoodColor,
		Texture,
		HullTexture,
		WoodTexture,
		GlassMaterial,
		FlameColor,
		Reset,
	}

	protected override void Submit_Internal()
	{
		if (_linkedMode != null)
		{
			ShipEnhancements.WriteDebugMessage("Switch to mode " + _linkedMode);
			_interface.SwitchToMode(_linkedMode);
		}
	}

	public InterfaceOptionType GetOptionType() => _optionType;
}