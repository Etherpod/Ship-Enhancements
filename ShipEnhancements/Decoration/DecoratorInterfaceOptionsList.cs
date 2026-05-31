using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorInterfaceOptionsList : MonoBehaviour
{
	[SerializeField]
	private DecoratorInterfaceOption[] _optionPrefabs;

	private List<DecoratorInterfaceOption> _displayedOptions = [];

	public void SetDisplayedOptions(DecoratorInterfaceOption.InterfaceOptionType[] optionTypes)
	{
		ClearDisplayedOptions();
		AddDisplayedOptions(optionTypes);
	}

	public void AddDisplayedOptions(DecoratorInterfaceOption.InterfaceOptionType[] optionTypes)
	{
		List<DecoratorInterfaceOption> optionsToSpawn = [];
		optionsToSpawn.AddRange(optionTypes
			.Select(type => _optionPrefabs.FirstOrDefault(p => 
				p.GetOptionType() == type))
			.Where(option => option != null));

		for (int i = 0; i < optionsToSpawn.Count; i++)
		{
			var newOption = Instantiate(optionsToSpawn[i].gameObject, transform)
				.GetComponent<DecoratorInterfaceOption>();
			
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