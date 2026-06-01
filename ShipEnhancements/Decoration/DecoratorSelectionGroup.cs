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

	private void Start()
	{
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

	public void SetAllSelected(bool selected)
	{
		_selectors.ForEach(s => s.SetSelected(selected));
	}

	public void SetAllActive(bool active)
	{
		_selectors.ForEach(s => s.SetActive(active));
	}
}