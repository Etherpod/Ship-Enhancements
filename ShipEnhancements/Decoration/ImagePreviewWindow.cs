using System.Collections.Generic;
using System.Linq;
using ShipEnhancements.Decoration.Modules;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class ImagePreviewWindow : DecoratorInterfaceWindow
{
	public delegate void OptionStateEvent(ImagePreviewElementData data);
	public event OptionStateEvent OnSelectOption;
	public event OptionStateEvent OnSubmitOption;

	[SerializeField]
	private Transform _layoutParent;
	[SerializeField]
	private GameObject _elementRowTemplate;
	[SerializeField]
	private GameObject _previewTemplate;
	[SerializeField]
	private int _rowSize = 3;
	[SerializeField]
	private GameObject _colorPickerHint;
	[SerializeField]
	private GameObject _resetHint;

	private List<ImagePreviewElement> _previews = [];
	private ImagePreviewElement _activeElement;
	private bool _hasColorPicker;
	private ImagePreviewElementData _defaultData;

	public void Initialize(ImagePreviewElementData[] elementData, bool showColorPicker = false)
	{
		List<Transform> elementRows = [];

		for (int i = 0; i < elementData.Length; i++)
		{
			var r = i / _rowSize;
			var c = i % _rowSize;
			
			if (c == 0)
			{
				elementRows.Add(Instantiate(_elementRowTemplate, _layoutParent).transform);
			}
			
			_previews.Add(Instantiate(_previewTemplate, elementRows[r]).GetComponent<ImagePreviewElement>());
			var activeElement = _previews[i];
			activeElement.Initialize(elementData[i]);
			activeElement.OnElementSelected += OnElementSelected;
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

				// link larger row to smaller row
				if (i == elementData.Length - 1 && c < _rowSize - 1)
				{
					for (int k = 1; k < _rowSize - c; k++)
					{
						var extraElement = _previews[i - _rowSize + k];
						if (extraElement)
						{
							extraElement.SetDownElement(activeElement);
						}
					}
				}
			}
			
			if (i > 0)
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

		if (showColorPicker)
		{
			_hasColorPicker = true;
			_colorPickerHint.SetActive(true);
		}
		else
		{
			_colorPickerHint.SetActive(false);
		}

		if (_defaultData == null)
		{
			_resetHint.SetActive(false);
			enabled = false;
		}
	}

	public void SetInitialIndex(int index)
	{
		var element = _previews
			.FirstOrDefault(e => e.GetData().listIndex == index);
		if (element != null)
		{
			_activeElement = element;
			_firstSelectedElement = element;
		}
	}

	public void SetDefaultData(ImagePreviewElementData data)
	{
		if (data != null)
		{
			_defaultData = data;
			_resetHint.SetActive(true);
			enabled = true;
		}
	}

	private void Update()
	{
		if (_defaultData == null)
		{
			enabled = false;
			return;
		}
		
		if (OWInput.IsNewlyPressed(InputLibrary.autopilot, InputMode.Character))
		{
			if (_activeElement != null)
			{
				_activeElement.Unsubmit();
				_activeElement = null;
			}

			Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.Menu_ResetDefaults);
			OnSelectOption?.Invoke(_defaultData);
			OnSubmitOption?.Invoke(_defaultData);
		}
	}

	private void OnElementSelected(DecoratorInterfaceElement element)
	{
		if (element is not ImagePreviewElement preview ||
			!_previews.Contains(preview)) return;

		OnSelectOption?.Invoke(preview.GetData());
	}

	private void OnElementSubmitted(DecoratorInterfaceElement element)
	{
		if (element is not ImagePreviewElement preview ||
			!_previews.Contains(preview)) return;

		if (_activeElement != null && _activeElement != element)
		{
			_activeElement.Unsubmit();
		}
		
		_activeElement = preview;
		OnSelectOption?.Invoke(preview.GetData());
		OnSubmitOption?.Invoke(preview.GetData());
	}

	public override void Activate() 
	{
		base.Activate();
		if (_activeElement != null)
		{
			_activeElement.Submit(false);
		}
	}

	public override void Deactivate()
	{
		if (_activeElement != null)
		{
			_activeElement.Unsubmit();
			if (_activeElement != _interface.GetSelectedElement())
			{
				OnElementSelected(_activeElement);
			}
		}
		else
		{
			OnSelectOption?.Invoke(_defaultData);
		}
		
		base.Deactivate();
	}

	private void OnDestroy()
	{
		foreach (var preview in _previews)
		{
			preview.OnElementSelected -= OnElementSelected;
			preview.OnElementSubmitted -= OnElementSubmitted;
		}
	}
}