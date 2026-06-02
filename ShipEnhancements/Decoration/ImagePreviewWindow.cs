using System.Collections.Generic;
using ShipEnhancements.Decoration.Modules;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class ImagePreviewWindow : DecoratorInterfaceWindow
{
	public delegate void OptionSubmitEvent(ImagePreviewElementData data);
	public event OptionSubmitEvent OnSubmitOption;
	
	[SerializeField]
	private GameObject _elementRowTemplate;
	[SerializeField]
	private GameObject _previewTemplate;
	[SerializeField]
	private int _rowSize = 3;

	private List<ImagePreviewElement> _previews = [];

	public void Initialize(ImagePreviewElementData[] elementData)
	{
		List<Transform> elementRows = [];

		for (int i = 0; i < elementData.Length; i++)
		{
			var r = i / _rowSize;
			var c = i % _rowSize;
			
			if (c == 0)
			{
				elementRows.Add(Instantiate(_elementRowTemplate, transform).transform);
			}
			
			_previews.Add(Instantiate(_previewTemplate, elementRows[r]).GetComponent<ImagePreviewElement>());
			var activeElement = _previews[i];
			activeElement.Initialize(elementData[i]);
			activeElement.OnElementSubmitted += OnElementSubmitted;

			if (i == 0)
			{
				_firstSelectedElement = activeElement;
			}
			
			if (r > 0)
			{
				var upElement = _previews[i - _rowSize];
				if (upElement)
				{
					upElement.SetDownElement(activeElement);
					activeElement.SetUpElement(upElement);
				}
			}
			
			if (c > 0)
			{
				var leftElement = _previews[i - 1];
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

		foreach (var preview in _previews)
		{
			preview.gameObject.SetActive(true);
		}
	}

	private void OnElementSubmitted(DecoratorInterfaceElement element)
	{
		if (element is not ImagePreviewElement preview ||
			!_previews.Contains(preview)) return;

		OnSubmitOption?.Invoke(preview.GetData());
	}

	private void OnDestroy()
	{
		foreach (var preview in _previews)
		{
			preview.OnElementSubmitted -= OnElementSubmitted;
		}
	}
}