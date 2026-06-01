using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.Decoration;

public class ColorPresetButton : DecoratorInterfaceElement
{
	[SerializeField]
	private Image _colorDisplayImage;

	private Color _colorPreset;

	public void SetColorPreset(Color preset)
	{
		_colorPreset = preset;
		_colorDisplayImage.color = preset;
	}

	public Color GetColorPreset() => _colorPreset;
}