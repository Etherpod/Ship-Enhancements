using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class SelectorModule : DecorationModule
{
	[SerializeField]
	protected string[] _optionNames;

	protected int _selectedIndex;
	protected OptionsListElementData _defaultData;

	public override DecoratorInterfaceWindow CreateWindow(Transform parent)
	{
		var window = base.CreateWindow(parent) as OptionsListWindow;
		if (window == null) return window;

		var data = GenerateOptionData();
		
		if (_defaultData == null)
		{
			_defaultData = GenerateDefaultData();
		}
		
		window.SetDefaultData(_defaultData);
		window.AddDisplayedOptions(data);
		window.SetInitialIndex(_selectedIndex);
		
		window.OnSelectOption += OnSelectOption;
		window.OnSubmitOption += OnSubmitOption;
		return window;
	}

	protected virtual OptionsListElementData[] GenerateOptionData()
	{
		List<OptionsListElementData> data = [];
		for (int i = 0; i < _optionNames.Length; i++)
		{
			data.Add(new OptionsListElementData(i, _optionNames[i]));
		}

		return data.ToArray();
	}
	
	protected virtual OptionsListElementData GenerateDefaultData()
	{
		return null;
	}
	
	protected virtual void OnSelectOption(OptionsListElementData data) { }

	protected virtual void OnSubmitOption(OptionsListElementData data)
	{
		_selectedIndex = data.listIndex;
		if (_currentWindow is OptionsListWindow listWindow)
		{
			listWindow.SetInitialIndex(data.listIndex);
		}
	}

	public string[] GetOptionNames() => _optionNames;
	
	public override void DestroyWindow()
	{
		base.DestroyWindow();
		if (_currentWindow is OptionsListWindow list)
		{
			list.OnSelectOption -= OnSelectOption;
			list.OnSubmitOption -= OnSubmitOption;
		}
	}
}