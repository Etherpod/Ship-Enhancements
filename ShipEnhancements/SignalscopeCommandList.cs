using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements;

public class SignalscopeCommandList : MonoBehaviour
{
	[SerializeField]
	private GameObject _commandItemTemplate;
	
	private SignalscopeCommandListItem[] _commandListItems;
	private SignalscopeCommandListItem _selectedItem;
	
	private void Start()
	{
		_commandListItems = new SignalscopeCommandListItem[10];
		for (int i = 0; i < _commandListItems.Length; i++)
		{
			var obj = Instantiate(_commandItemTemplate, _commandItemTemplate.transform.parent);
			obj.name = "CommandListItem_" + i;
			_commandListItems[i] = obj.GetComponent<SignalscopeCommandListItem>();
		}

		Destroy(_commandItemTemplate);
	}

	public void SetCommands(List<ShipCommand> commands)
	{
		if (_selectedItem != null)
		{
			_selectedItem.Deselect();
			_selectedItem = null;
		}
		
		for (int i = 0; i < _commandListItems.Length; i++)
		{
			if (commands.Count > i)
			{
				_commandListItems[i].DisplayCommand(commands[i]);
			}
			else
			{
				_commandListItems[i].Clear();
			}
		}

		if (_commandListItems[0].gameObject.activeSelf)
		{
			_selectedItem = _commandListItems[0];
			_selectedItem.Select();
		}
	}

	public void SelectCommand(ShipCommand command)
	{
		if (_selectedItem != null)
		{
			_selectedItem.Deselect();
			_selectedItem = null;
		}
		
		foreach (var item in _commandListItems)
		{
			if (item.GetCommand() == command)
			{
				_selectedItem = item;
				item.Select();
			}
		}
	}
}