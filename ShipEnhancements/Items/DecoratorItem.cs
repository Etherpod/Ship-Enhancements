using System;
using ShipEnhancements.Decoration;
using UnityEngine;

namespace ShipEnhancements.Items;

public class DecoratorItem : OWItem
{
	public static readonly ItemType ItemType = ShipEnhancements.Instance.DecoratorType;
	
	private readonly float _maxRaycastDistance = 100f;
	private DecoratorInterface _interface;
	private DecoratorSelection _currentSelection;
	
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

	private void UpdateSelection()
	{
		if (OWInput.GetInputMode() != InputMode.Character &&
			OWInput.GetInputMode() != InputMode.Menu)
		{
			ToggleCurrentSelection(false);
            _interface.Deactivate();
            _currentSelection = null;
            return;
		}
		
		if (OWInput.IsNewlyPressed(InputLibrary.lockOn, InputMode.Character) && 
			!_currentSelection.IsActive())
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