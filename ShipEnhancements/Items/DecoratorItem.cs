using System;
using System.Collections.Generic;
using ShipEnhancements.Decoration;
using UnityEngine;

namespace ShipEnhancements.Items;

public class DecoratorItem : OWItem
{
	public static readonly ItemType ItemType = ShipEnhancements.Instance.DecoratorType;
	
	private readonly float _maxRaycastDistance = 100f;
	private DecoratorInterface _interface;
	private DecoratorSelectionManager _selectionManager;
	/*private DecoratorSelection _currentSelection;
	private bool _inMultiSelect;
	private DecoratorSelection _currentSubSelection;
	private List<DecoratorSelection> _multiSelectObjects = [];*/
	
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
		_selectionManager = _interface.GetSelectionManager();
		_interface.OnModeActivated += OnInterfaceModeActivated;
	}

	private void Update()
	{
		if (Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem() == this)
		{
			if ((!_selectionManager.HasAnyActive() && !_interface.IsActive()) || 
			    _selectionManager.IsInMultiSelect())
			{
				ProcessRaycast();
			}

			if (_selectionManager.HasSelection())
			{
				UpdateSelection();
			}
		}
		else if (_selectionManager.HasSelection())
		{
			DisableSelection();
		}
	}

	private void ProcessRaycast()
	{
		if (!Physics.Raycast(Locator.GetPlayerCamera().transform.position, Locator.GetPlayerCamera().transform.forward,
			out RaycastHit hit, _maxRaycastDistance, OWLayerMask.interactMask))
		{
			/*if (_currentSelection != null)
			{
				ToggleCurrentSelection(false);
				_currentSelection = null;
			}*/

			if (_selectionManager.HasSelection())
			{
				_selectionManager.ClearSelections();
			}
			
			return;
		}
		
		var selector = hit.collider.GetComponentInParent<DecoratorSelection>();

		// better collider setup so I don't have to get parent
		if (selector != null && hit.distance <= selector.GetSelectDistance())
		{
			/*if (selector != _currentSelection)
			{
				if (_currentSelection != null && 
					(_currentSelection.GetSelectionGroup() == null ||
					_currentSelection.GetSelectionGroup() != selector.GetSelectionGroup()))
				{
					ToggleCurrentSelection(false);
				}

				_currentSelection = selector;
				ToggleCurrentSelection(true);
			}*/

			if (_selectionManager.IsSelecting(selector)) return;
			
			if (selector.GetSelectionGroup() != null && !_selectionManager.IsInMultiSelect())
			{
				_selectionManager.SetGroup(selector.GetSelectionGroup());
			}
			else
			{
				_selectionManager.SetSelection(selector);
			}
		}
		else if (_selectionManager.HasSelection())
		{
			_selectionManager.ClearSelections();
		}
	}

	/*private void UpdateMultiSelect()
	{
		if (!Physics.Raycast(Locator.GetPlayerCamera().transform.position, Locator.GetPlayerCamera().transform.forward,
			    out RaycastHit hit, _maxRaycastDistance, OWLayerMask.interactMask))
		{
			if (_currentSubSelection != null && 
			    !_multiSelectObjects.Contains(_currentSubSelection))
			{
				_currentSubSelection.SetActive(false);
				_currentSubSelection = null;
			}
			
			return;
		}
		
		if (hit.collider.transform.parent.TryGetComponent(out DecoratorSelection selector) &&
		    hit.distance <= selector.GetSelectDistance())
		{
			if (selector != _currentSubSelection)
			{
				if (_currentSubSelection != null && 
				    !_multiSelectObjects.Contains(_currentSubSelection))
				{
					_currentSubSelection.SetActive(false);
				}

				_currentSubSelection = selector;
				_currentSubSelection.SetActive(true);
			}
		}
		else if (_currentSubSelection != null && 
		         !_multiSelectObjects.Contains(_currentSubSelection))
		{
			_currentSubSelection.SetActive(false);
			_currentSubSelection = null;
		}
	}*/

	private void UpdateSelection()
	{
		if (OWInput.GetInputMode() != InputMode.Character &&
			OWInput.GetInputMode() != InputMode.Menu)
		{
			DisableSelection();
            return;
		}
		
		if (OWInput.IsNewlyPressed(InputLibrary.lockOn, InputMode.Character))
		{
			_selectionManager.SetSelectionsActive();

			if (!_selectionManager.IsInMultiSelect())
			{
				_interface.Activate();
				_interface.OnInterfaceDeactivated += OnInterfaceDeactivated;
			}
		}
	}
	
	// move into event?
	private void DisableSelection()
	{
		_interface.Deactivate();
		_selectionManager.ClearSelections();
		GlobalMessenger<DecoratorSelectionGroup>.FireEvent("SE_SetDecoratorMask", null);
	}
	
	private void OnInterfaceDeactivated()
	{
		_interface.OnInterfaceDeactivated -= OnInterfaceDeactivated;
		_selectionManager.ClearSelections();
		
		GlobalMessenger<DecoratorSelectionGroup>.FireEvent("SE_SetDecoratorMask", null);
	}

	private void OnInterfaceModeActivated(float fadeOverride)
	{
		//_currentSelection.SetFadeOverride(fadeOverride);
	}

	public override void PickUpItem(Transform holdTranform)
	{
		base.PickUpItem(holdTranform);
		GlobalMessenger<DecoratorItem>.FireEvent("SE_EquipDecorator", this);
	}

	public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
	{
		bool wasHeld = Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem() == this;
		base.DropItem(position, normal, parent, sector, customDropTarget);
		if (wasHeld)
		{
			GlobalMessenger<DecoratorItem>.FireEvent("SE_UnequipDecorator", this);
		}
	}

	public override void SocketItem(Transform socketTransform, Sector sector)
	{
		bool wasHeld = Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem() == this;
		base.SocketItem(socketTransform, sector);
		if (wasHeld)
		{
			GlobalMessenger<DecoratorItem>.FireEvent("SE_UnequipDecorator", this);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		_interface.OnModeActivated -= OnInterfaceModeActivated;
	}
}