using System;
using ShipEnhancements.Decoration;
using UnityEngine;

namespace ShipEnhancements.Items;

public class DecoratorItem : OWItem
{
	public static readonly ItemType ItemType = ShipEnhancements.Instance.DecoratorType;
	
	private readonly float _maxRaycastDistance = 100f;
	private DecoratorInterface _interface;
	private DecoratorSelector _currentSelection;
	
	public override string GetDisplayName()
	{
		return "Decorator";
	}

	public override void Awake()
	{
		base.Awake();
		_type = ItemType;
	}

	private void Start()
	{
		_interface = FindObjectOfType<DecoratorInterface>();
	}

	private void Update()
	{
		if (Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem() == this)
		{
			if (_currentSelection == null || !_currentSelection.IsActive())
			{
				ProcessRaycast();
			}

			if (_currentSelection != null)
			{
				UpdateSelection();
			}
		}
		else if (_currentSelection != null)
		{
			ToggleCurrentSelection(false);
			_interface.Deactivate();
			_currentSelection = null;
		}
	}

	private void ProcessRaycast()
	{
		if (!Physics.Raycast(Locator.GetPlayerCamera().transform.position, Locator.GetPlayerCamera().transform.forward,
			out RaycastHit hit, _maxRaycastDistance, OWLayerMask.interactMask))
		{
			if (_currentSelection != null)
			{
				ToggleCurrentSelection(false);
				_currentSelection = null;
			}
			
			return;
		}

		// better collider setup so I don't have to get parent
		if (hit.collider.transform.parent.TryGetComponent(out DecoratorSelector selector) &&
			hit.distance <= selector.GetSelectDistance())
		{
			if (selector != _currentSelection)
			{
				if (_currentSelection != null && 
					(_currentSelection.GetSelectionGroup() == null ||
					_currentSelection.GetSelectionGroup() != selector.GetSelectionGroup()))
				{
					ToggleCurrentSelection(false);
				}

				_currentSelection = selector;
				ToggleCurrentSelection(true);
			}
		}
		else if (_currentSelection != null)
		{
			ToggleCurrentSelection(false);
			_currentSelection = null;
		}
	}

	private void UpdateSelection()
	{
		if (OWInput.IsNewlyPressed(InputLibrary.lockOn) && !_currentSelection.IsActive())
		{
			DecoratorSelectionData data;
			if (_currentSelection.GetSelectionGroup() != null)
			{
				data = _currentSelection.GetSelectionGroup().GetComponent<DecoratorSelectionData>();
				_currentSelection.GetSelectionGroup().SetAllActive(true);
			}
			else
			{
				data = _currentSelection.GetComponent<DecoratorSelectionData>();
				_currentSelection.SetActive(true);
			}
			
			_interface.Activate(data);
			_interface.OnInterfaceDeactivated += OnInterfaceDeactivated;
		}
		/*else if (OWInput.IsNewlyPressed(InputLibrary.cancel) && _currentSelection.IsActive())
		{
			if (_currentSelection.GetSelectionGroup() != null)
			{
				_currentSelection.GetSelectionGroup().SetAllActive(false);
			}
			else
			{
				_currentSelection.SetActive(false);
			}
			
			// close menu
		}*/
	}

	private void ToggleCurrentSelection(bool selected)
	{
		if (_currentSelection == null) return;

		if (_currentSelection.GetSelectionGroup() != null)
		{
			_currentSelection.GetSelectionGroup().SetAllSelected(selected);
		}
		else
		{
			_currentSelection.SetSelected(selected);
		}
	}
	
	private void OnInterfaceDeactivated()
	{
		_interface.OnInterfaceDeactivated -= OnInterfaceDeactivated;
		if (_currentSelection != null)
		{
			ToggleCurrentSelection(false);
			_currentSelection = null;
		}
	}
}