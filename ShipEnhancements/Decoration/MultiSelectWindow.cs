using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.Decoration;

public class MultiSelectWindow : DecoratorInterfaceWindow
{
	[SerializeField]
	private Text _selectionText;

	private DecoratorSelectionManager _selectionManager;

	private void Start()
	{
		_selectionManager = _interface.GetSelectionManager();
	}

	private void Update()
	{
		if (_selectionManager.IsInMultiSelect())
		{
			var current = _selectionManager.GetActiveSelections().Length;
			var total = _selectionManager.GetAllSelections().Length;
			_selectionText.text = $"{current}/{total} Selected";
		}
	}
}