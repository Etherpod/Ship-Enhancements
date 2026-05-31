using ShipEnhancements.Decoration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShipEnhancements.Items;

// PLEASE FIX NAMESPACE AFTER PUSH
public class ColorPresetButton : DecoratorInterfaceElement
{
	[SerializeField]
	private Color _colorPreset = Color.white;
	[SerializeField]
	private Image _colorDisplayImage;

	private void Start()
	{
		_colorDisplayImage.color = _colorPreset;
	}
}