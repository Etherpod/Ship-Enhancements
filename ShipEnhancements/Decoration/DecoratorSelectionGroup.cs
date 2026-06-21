using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorSelectionGroup : MonoBehaviour
{
	[SerializeField]
	private GameObject[] _selectorPrefabs;
	[SerializeField]
	private string[] _relativePaths;

	private List<DecoratorSelection> _selectors = [];
	private List<DecoratorSelection> _activeSelectors = [];
	private DecoratorSelectionData _data;
	
	private void Start()
	{
		if (TryGetComponent(out DecoratorSelectionData data))
		{
			_data = data;
		}
		
		if (_relativePaths.Length != _selectorPrefabs.Length)
		{
			ShipEnhancements.WriteDebugMessage($"ERROR - List lengths do not match on group {gameObject.name}");
		}
		
		for (int i = 0; i < _selectorPrefabs.Length; i++)
		{
			var parent = SELocator.GetShipTransform().Find(_relativePaths[i]);
			if (parent == null)
			{
				ShipEnhancements.WriteDebugMessage($"ERROR - Incorrect relative path on group {gameObject.name}\n{_relativePaths[i]}");
			}

			var selector = ShipEnhancements.CreateObject(_selectorPrefabs[i], parent)
				.GetComponent<DecoratorSelection>();
			selector.SetSelectionGroup(this);
			_selectors.Add(selector);
		}
	}

	public DecoratorSelection[] GetSelectors() => _selectors.ToArray();

	public DecoratorSelection[] GetActiveSelectors() => _activeSelectors.ToArray();

	public DecoratorSelectionData GetSelectionData() => _data;

	public void SetAllSelected(bool selected)
	{
		_selectors.ForEach(s => s.SetSelected(selected));
	}

	public void SetAllActive(bool active, bool mask)
	{
		var list = mask ? _activeSelectors : _selectors;
		list.ForEach(s => s.SetActive(active));
	}

	public void SetSelectorActive(DecoratorSelection selection, bool active)
	{
		if (!_selectors.Contains(selection)) return;

		if (active && !_activeSelectors.Contains(selection))
		{
			_activeSelectors.Add(selection);
		}
		else if (!active && _activeSelectors.Contains(selection))
		{
			_activeSelectors.Remove(selection);
		}
	}
}