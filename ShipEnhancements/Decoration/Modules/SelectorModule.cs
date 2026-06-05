using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class SelectorModule : DecorationModule
{
	[SerializeField]
	protected string[] _optionNames;

	protected int _selectedIndex;

	public override DecoratorInterfaceWindow CreateWindow(Transform parent)
	{
		var window = base.CreateWindow(parent) as OptionsListWindow;
		if (window == null) return window;

		var data = GenerateOptionData();
		window.AddDisplayedOptions(data);
		// might have to move selection
		window.SelectElementAtIndex(_selectedIndex);
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

	protected virtual void OnSubmitOption(OptionsListElementData data)
	{
		_selectedIndex = data.listIndex;
	}

	public string[] GetOptionNames() => _optionNames;
	
	public override void DestroyWindow()
	{
		base.DestroyWindow();
		if (_currentWindow is OptionsListWindow list)
		{
			list.OnSubmitOption -= OnSubmitOption;
		}
	}
}