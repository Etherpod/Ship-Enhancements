using System;
using System.Collections.Generic;
using System.Linq;
using ShipEnhancements.Decoration.Modules;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class OptionsListWindow : DecoratorInterfaceWindow
{
	public delegate void OptionSubmitEvent(OptionsListElementData data);
	public event OptionSubmitEvent OnSubmitOption;
	
	[SerializeField]
	protected GameObject _optionTemplate;
	[SerializeField]
	protected Transform _rootTransform;

	protected List<OptionsListElement> _displayedOptions = [];

	public OptionsListElement[] SetDisplayedOptions(OptionsListElementData[] names)
	{
		ClearDisplayedOptions();
		return AddDisplayedOptions(names);
	}

	public OptionsListElement[] AddDisplayedOptions(OptionsListElementData[] elementData)
	{
		for (int i = 0; i < elementData.Length; i++)
		{
			var newOption = Instantiate(_optionTemplate, _rootTransform)
				.GetComponent<OptionsListElement>();
			newOption.Initialize(elementData[i]);
			
			if (i > 0)
			{
				newOption.SetUpElement(_displayedOptions[i - 1]);
				_displayedOptions[i - 1].SetDownElement(newOption);
			}
			
			newOption.gameObject.SetActive(true);
			_displayedOptions.Add(newOption);

			newOption.OnElementSubmitted += OnElementSubmitted;
		}

		_firstSelectedElement = _displayedOptions[0];
		return _displayedOptions.ToArray();
	}

	public void ClearDisplayedOptions()
	{
		_firstSelectedElement = null;
		for (int i = _displayedOptions.Count - 1; i >= 0; i--)
		{
			_displayedOptions[i].OnElementSubmitted -= OnElementSubmitted;
			Destroy(_displayedOptions[i].gameObject);
			_displayedOptions.RemoveAt(i);
		}
	}

	public OptionsListElement[] GetDisplayedOptions() => _displayedOptions.ToArray();

	private void OnElementSubmitted(DecoratorInterfaceElement element)
	{
		if (element is not OptionsListElement option ||
			!_displayedOptions.Contains(option))
		{
			return;
		}
		
		OnSubmitOption?.Invoke(option.GetData());
	}

	private void OnDestroy()
	{
		ClearDisplayedOptions();
	}
}