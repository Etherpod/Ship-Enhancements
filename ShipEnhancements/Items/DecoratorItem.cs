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
	private DecoratorSelection _currentSelection;
	private bool _inMultiSelect;
	private DecoratorSelection _currentSubSelection;
	private List<DecoratorSelection> _multiSelectObjects = [];
	
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
		_interface.OnModeActivated += OnInterfaceModeActivated;
	}

	private void Update()
	{
		if (Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem() == this)
		{
			if ((_currentSelection == null || !_currentSelection.IsActive()) && 
			    !_inMultiSelect && !_interface.IsActive())
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
			DisableSelection();
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
		if (hit.collider.transform.parent.TryGetComponent(out DecoratorSelection selector) &&
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

	private void UpdateMultiSelect()
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
	}

	private void UpdateSelection()
	{
		if (OWInput.GetInputMode() != InputMode.Character &&
			OWInput.GetInputMode() != InputMode.Menu)
		{
			DisableSelection();
            return;
		}

		if (_inMultiSelect)
		{
			UpdateMultiSelect();
		}
		
		if (OWInput.IsNewlyPressed(InputLibrary.lockOn, InputMode.Character))
		{
			if (_inMultiSelect && _currentSubSelection != null)
			{
				if (!_multiSelectObjects.Contains(_currentSubSelection))
				{
					_multiSelectObjects.Add(_currentSubSelection);
				}
				else
				{
					_multiSelectObjects.Remove(_currentSubSelection);
					_currentSubSelection.SetActive(false);
				}
			}
			else if (!_currentSelection.IsActive())
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
				_interface.OnEnterMultiSelect += OnEnterMultiSelect;
				_interface.OnExitMultiSelect += OnExitMultiSelect;
			}
		}
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
	
	// move into event?
	private void DisableSelection()
	{
		ToggleCurrentSelection(false);
		_interface.Deactivate();
		_currentSelection = null;
		_currentSubSelection = null;
		_multiSelectObjects.Clear();
		GlobalMessenger<DecoratorSelectionGroup>.FireEvent("SE_SetDecoratorMask", null);
		_inMultiSelect = false;
	}
	
	private void OnInterfaceDeactivated()
	{
		_interface.OnInterfaceDeactivated -= OnInterfaceDeactivated;
		_interface.OnEnterMultiSelect -= OnEnterMultiSelect;
		_interface.OnExitMultiSelect -= OnExitMultiSelect;
		if (_currentSelection != null)
		{
			ToggleCurrentSelection(false);
			_currentSelection = null;
		}

		_currentSubSelection = null;
		_multiSelectObjects.Clear();
		GlobalMessenger<DecoratorSelectionGroup>.FireEvent("SE_SetDecoratorMask", null);
		_inMultiSelect = false;
	}
	
	private void OnEnterMultiSelect()
	{
		if (_currentSelection == null || 
		    _currentSelection.GetSelectionGroup() == null)
		{
			return;
		}

		foreach (var selection in _currentSelection.GetSelectionGroup().GetSelectors())
		{
			selection.SetActive(_multiSelectObjects.Contains(selection));
		}
		
		GlobalMessenger<DecoratorSelectionGroup>.FireEvent("SE_SetDecoratorMask", 
			_currentSelection.GetSelectionGroup());
		_inMultiSelect = true;
	}
	
	private void OnExitMultiSelect()
	{
		GlobalMessenger<DecoratorSelectionGroup>.FireEvent("SE_SetDecoratorMask", null);
		_inMultiSelect = false;
	}

	private void OnInterfaceModeActivated(float fadeOverride)
	{
		_currentSelection.SetFadeOverride(fadeOverride);
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