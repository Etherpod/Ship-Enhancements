using System.Collections.Generic;
using ShipEnhancements.Decoration.Modules;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class ColorSelectorMode : DecoratorInterfaceMode
{
	[SerializeField]
	private GameObject _elementRowTemplate;
	[SerializeField]
	private GameObject _colorButtonTemplate;
	[SerializeField]
	private int _rowSize = 3;

	private List<ColorPresetButton> _buttons = [];

	public void Initialize(Color[] colorPresets)
	{
		List<Transform> elementRows = [];

		for (int i = 0; i < colorPresets.Length; i++)
		{
			var r = i / _rowSize;
			var c = i % _rowSize;
			
			if (c == 0)
			{
				elementRows.Add(Instantiate(_elementRowTemplate, transform).transform);
			}
			
			_buttons.Add(Instantiate(_colorButtonTemplate, elementRows[r]).GetComponent<ColorPresetButton>());
			var activeElement = _buttons[i];
			activeElement.SetColorPreset(colorPresets[i]);
			activeElement.OnElementSubmitted += OnElementSubmitted;

			if (i == 0)
			{
				_firstSelectedElement = activeElement;
			}
			
			if (r > 0)
			{
				var upElement = _buttons[i - _rowSize];
				if (upElement)
				{
					upElement.SetDownElement(activeElement);
					activeElement.SetUpElement(upElement);
				}
			}
			
			if (c > 0)
			{
				var leftElement = _buttons[i - 1];
				if (leftElement)
				{
					leftElement.SetRightElement(activeElement);
					activeElement.SetLeftElement(leftElement);
				}
			}
		}

		foreach (var row in elementRows)
		{
			row.gameObject.SetActive(true);
		}

		foreach (var button in _buttons)
		{
			button.gameObject.SetActive(true);
		}
	}

	private void OnElementSubmitted(DecoratorInterfaceElement element)
	{
		if (element is not ColorPresetButton colorPreset) return;

		if (_module is ColorModule colorModule)
		{
			colorModule.ApplyColor(colorPreset.GetColorPreset());
		}
	}

	public override float GetSelectionFadeOverride() => 0.1f;

	private void OnDestroy()
	{
		foreach (var button in _buttons)
		{
			button.OnElementSubmitted -= OnElementSubmitted;
		}
	}
}