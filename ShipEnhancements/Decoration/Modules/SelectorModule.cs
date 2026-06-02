using System.Linq;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class SelectorModule : DecorationModule
{
	[SerializeField]
	protected string[] _optionNames;

	public override DecoratorInterfaceWindow CreateWindow(Transform parent)
	{
		var window = base.CreateWindow(parent) as OptionsListWindow;
		if (window == null) return window;

		var data = GenerateOptionData();
		window.AddDisplayedOptions(data);
		window.OnSubmitOption += OnSubmitOption; 
		return window;
	}

	protected virtual OptionsListElementData[] GenerateOptionData()
	{
		return _optionNames
			.Select(n => new OptionsListElementData(n))
			.ToArray();
	}

	protected virtual void OnSubmitOption(OptionsListElementData data) { }

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