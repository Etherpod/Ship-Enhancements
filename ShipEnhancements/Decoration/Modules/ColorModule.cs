using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class ColorModule : DecorationModule
{
	[SerializeField]
	protected Color[] _colorPresets;

	public override DecoratorInterfaceMode CreateInterfaceMode(Transform parent)
	{
		var mode = base.CreateInterfaceMode(parent) as ColorSelectorMode;
		mode?.Initialize(_colorPresets);
		return mode;
	}

	public virtual void ApplyColor(Color color) { }
}