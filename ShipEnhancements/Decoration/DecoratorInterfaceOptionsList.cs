using System.Collections.Generic;
using System.Linq;
using ShipEnhancements.Decoration.Modules;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorInterfaceOptionsList : MonoBehaviour
{
	[SerializeField]
	private GameObject _optionTemplate;

	private List<DecoratorInterfaceOption> _displayedOptions = [];

	public void SetDisplayedOptions(DecorationModule[] modules, DecoratorInterfaceMode[] modes)
	{
		ClearDisplayedOptions();
		AddDisplayedOptions(modules, modes);
	}

	public void AddDisplayedOptions(DecorationModule[] modules, DecoratorInterfaceMode[] modes)
	{
		for (int i = 0; i < modules.Length; i++)
		{
			var newOption = Instantiate(_optionTemplate, transform)
				.GetComponent<DecoratorInterfaceOption>();
			newOption.Initialize();
			newOption.SetDisplayText(modules[i].GetDisplayName());
			if (i < modes.Length)
			{
				newOption.SetLinkedMode(modes[i]);
			}
			
			if (i > 0)
			{
				newOption.SetUpElement(_displayedOptions[i - 1]);
				_displayedOptions[i - 1].SetDownElement(newOption);
			}
			
			newOption.gameObject.SetActive(true);
			_displayedOptions.Add(newOption);
		}
		
		_displayedOptions[0].Select();
		ShipEnhancements.WriteDebugMessage("Select " + _displayedOptions[0]);
	}

	public void ClearDisplayedOptions()
	{
		for (int i = _displayedOptions.Count - 1; i >= 0; i--)
		{
			Destroy(_displayedOptions[i].gameObject);
			_displayedOptions.RemoveAt(i);
		}
	}
}