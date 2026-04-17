using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements;

public class SignalscopeCommandList : MonoBehaviour
{
	[SerializeField]
	private GameObject _commandItemTemplate;
	
	private SignalscopeCommandListItem[] _commandListItems;
	
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
	}
}