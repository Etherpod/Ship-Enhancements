using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorSelectionManager : MonoBehaviour
{
	private List<DecoratorSelection> _currentSelections = [];
	private DecoratorSelectionGroup _currentGroup;
	private DecoratorSelectionData _currentData;
	private bool _inMultiSelect;

	public void AddSelection(DecoratorSelection selection)
	{
		if (!_currentSelections.Contains(selection))
		{
			_currentSelections.Add(selection);	
			if (_inMultiSelect)
			{
				selection.SetActive(true);
			}
			else
			{
				selection.SetSelected(true);
			}
		}
	}

	public void RemoveSelection(DecoratorSelection selection)
	{
		if (_currentSelections.Contains(selection))
		{
			_currentSelections.Remove(selection);
			if (_inMultiSelect)
			{
				selection.SetActive(_currentGroup.GetActiveSelectors().Contains(selection));
			}
			else
			{
				selection.SetSelected(false);
			}
		}
	}
	
	public void ClearSelections()
	{
		if (!_inMultiSelect && _currentGroup != null)
		{
			foreach (var selection in _currentGroup.GetActiveSelectors())
			{
				selection.SetSelected(false);
			}
			_currentGroup = null;
		}

		for (int i = _currentSelections.Count - 1; i >= 0; i--)
		{
			RemoveSelection(_currentSelections[i]);
		}
	}

	public void SetSelection(DecoratorSelection selection)
	{
		ClearSelections();
		AddSelection(selection);
		if (!_inMultiSelect)
		{
			_currentData = selection.GetSelectionData();
		}
	}
	
	public void SetGroup(DecoratorSelectionGroup group, bool select = true)
	{
		if (group == _currentGroup) return;
		
		ClearSelections();
		_currentGroup = group;
		_currentData = group.GetSelectionData();
		if (!select) return;
		AddAllInGroup();
	}

	public void AddAllInGroup(bool setActive = false)
	{
		if (_currentGroup == null) return;
		
		foreach (var selection in _currentGroup.GetSelectors())
		{
			if (setActive)
			{
				_currentGroup.SetSelectorActive(selection, true);
				selection.SetActive(true);
			}
			else
			{
				AddSelection(selection);	
			}
		}
	}

	public void RemoveAllInGroup(bool setActive = false)
	{
		if (_currentGroup == null) return;
		
		foreach (var selection in _currentGroup.GetSelectors())
		{
			if (setActive)
			{
				_currentGroup.SetSelectorActive(selection, false);
				selection.SetActive(false);
			}
			else
			{
				RemoveSelection(selection);	
			}
		}
	}

	public void SetSelectionsActive()
	{
		foreach (var selection in _currentSelections)
		{
			if (_inMultiSelect)
			{
				var state = !_currentGroup.GetActiveSelectors().Contains(selection);
				_currentGroup.SetSelectorActive(selection, state);
				if (!state) selection.SetActive(false);
			}
			else
			{
				selection.SetActive(_currentGroup == null || 
					_currentGroup.GetActiveSelectors().Length == 0 || 
					_currentGroup.GetActiveSelectors().Contains(selection));
			}
		}
	}

	public void EnableMultiSelect()
	{
		foreach (var selection in _currentSelections)
		{
			selection.SetActive(_currentGroup.GetActiveSelectors().Contains(selection));
		}
		
		_inMultiSelect = true;
	}

	public void DisableMultiSelect()
	{
		_inMultiSelect = false;
		AddAllInGroup();
	}
	
	public void SetFadeOverride(float fade)
	{
		foreach (var selection in _currentSelections)
		{
			selection.SetFadeOverride(fade);
		}
	}

	public bool HasSelection() => _currentSelections.Count > 0;

	public bool IsSelecting(DecoratorSelection selection) => _currentSelections.Contains(selection);

	public bool HasAnyActive() => _currentSelections.Any(s => s.IsActive());

	public bool IsInMultiSelect() => _inMultiSelect;

	public float GetDistanceToSelector(Vector3 worldPos)
	{
		if (_currentSelections.Count == 0)
		{
			return -1;
		}

		return (_currentSelections[0].transform.position - worldPos).magnitude;
	}

	public DecoratorSelectionData GetCurrentData() => _currentData;

	public DecoratorSelection[] GetActiveSelections()
	{
		if (_currentGroup != null && _currentGroup.GetActiveSelectors().Length > 0)
		{
			return _currentGroup.GetActiveSelectors();
		}

		return _currentSelections.ToArray();
	}

	public DecoratorSelection[] GetAllSelections()
	{
		if (_currentGroup != null)
		{
			return _currentGroup.GetSelectors();
		}

		return _currentSelections.ToArray();
	}
}