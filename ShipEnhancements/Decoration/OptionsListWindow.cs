using System;
using System.Collections.Generic;
using System.Linq;
using ShipEnhancements.Decoration.Modules;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class OptionsListWindow : DecoratorInterfaceWindow
{
	public delegate void OptionStateEvent(OptionsListElementData data);
	public event OptionStateEvent OnSelectOption;
	public event OptionStateEvent OnSubmitOption;
	
	[SerializeField]
	protected GameObject _optionTemplate;
	[SerializeField]
	protected Transform _rootTransform;

	protected List<OptionsListElement> _displayedOptions = [];
	protected OptionsListElement _activeElement;

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

			newOption.OnElementSelected += OnElementSelected;
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
			_displayedOptions[i].OnElementSelected -= OnElementSelected;
			_displayedOptions[i].OnElementSubmitted -= OnElementSubmitted;
			Destroy(_displayedOptions[i].gameObject);
			_displayedOptions.RemoveAt(i);
		}
	}

	public OptionsListElement[] GetDisplayedOptions() => _displayedOptions.ToArray();

	public void SetInitialIndex(int index)
	{
		var element = _displayedOptions
			.FirstOrDefault(e => e.GetData().listIndex == index);
		if (element != null)
		{
			_activeElement = element;
			_firstSelectedElement = element;
		}
	}
	
	private void OnElementSelected(DecoratorInterfaceElement element)
	{
		if (element is not OptionsListElement option ||
			!_displayedOptions.Contains(option)) return;

		OnSelectOption?.Invoke(option.GetData());
	}

	private void OnElementSubmitted(DecoratorInterfaceElement element)
	{
		if (element is not OptionsListElement option ||
			!_displayedOptions.Contains(option))
		{
			return;
		}
		
		if (_activeElement != null && _activeElement != element)
		{
			_activeElement.Unsubmit();
		}
		
		OnSubmitOption?.Invoke(option.GetData());
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
		base.Deactivate();
	}

	private void OnDestroy()
	{
		ClearDisplayedOptions();
	}
}