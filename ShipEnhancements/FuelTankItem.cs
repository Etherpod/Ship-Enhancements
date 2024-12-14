using System;
using UnityEngine;

namespace ShipEnhancements;

public class FuelTankItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.FuelTankItemType;

    [SerializeField]
    private InteractReceiver _interactReceiver;
    [SerializeField]
    private OWAudioSource _refuelSource;

    private Collider _itemCollider;
    private ScreenPrompt _refuelPrompt;
    private ScreenPrompt _fuelDepletedPrompt;
    private float _maxFuel = 300f;
    private float _currentFuel;
    private float _refillRate = 25f;
    private bool _refillingFuel;
    private bool _focused;

    public override string GetDisplayName()
    {
        return "Portable Fuel Canister";
    }

    public override void Awake()
    {
        base.Awake();
        _itemCollider = GetComponent<Collider>();
        _refuelPrompt = new ScreenPrompt(InputLibrary.interactSecondary, "<CMD>" + UITextLibrary.GetString(UITextType.HoldPrompt)
            + " Refill Fuel", 0, ScreenPrompt.DisplayState.Normal, false);
        _fuelDepletedPrompt = new ScreenPrompt("Fuel depleted");
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;
    }

    private void Start()
    {
        _interactReceiver.ChangePrompt("Pick up " + GetDisplayName());
        _fuelDepletedPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
        _currentFuel = _maxFuel;
    }

    private void Update()
    {
        _refuelPrompt.SetVisibility(false);
        _fuelDepletedPrompt.SetVisibility(false);

        if (_focused)
        {
            OWItem item = Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem();
            if (item != null)
            {
                _interactReceiver.ChangePrompt("Already holding " + item.GetDisplayName());
                _interactReceiver.SetKeyCommandVisible(false);
            }
            else
            {
                _interactReceiver.ChangePrompt("Pick up " + GetDisplayName());
                _interactReceiver.SetKeyCommandVisible(true);
            }

            if (OWInput.IsInputMode(InputMode.Character))
            {
                if (_currentFuel <= 0f)
                {
                    _fuelDepletedPrompt.SetVisibility(true);
                }
                else if (PlayerState.IsWearingSuit())
                {
                    _refuelPrompt.SetVisibility(true);

                    if (PlayerMaxFuel())
                    {
                        _refuelPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
                    }
                    else
                    {
                        _refuelPrompt.SetDisplayState(ScreenPrompt.DisplayState.Normal);
                        if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary))
                        {
                            _refuelSource.Play();
                            ShipNotifications.PostRefuelingNotification();
                            _interactReceiver._hasInteracted = true;
                            _refillingFuel = true;
                        }
                        else if (OWInput.IsNewlyReleased(InputLibrary.interactSecondary))
                        {
                            StopRefuel();
                        }
                    }
                }
            }
        }

        if (_refillingFuel)
        {
            SELocator.GetPlayerResources()._currentFuel += _refillRate * Time.deltaTime;
            _currentFuel = Mathf.Max(_currentFuel - _refillRate * Time.deltaTime, 0f);
            if (PlayerMaxFuel() || _currentFuel <= 0f)
            {
                StopRefuel();
            }
        }
    }

    private void OnPressInteract()
    {
        ItemTool itemTool = Locator.GetToolModeSwapper().GetItemCarryTool();
        if (itemTool.GetHeldItem() == null)
        {
            itemTool.MoveItemToCarrySocket(this);
            itemTool._heldItem = this;
            Locator.GetPlayerAudioController().PlayPickUpItem(ItemType);
            Locator.GetToolModeSwapper().EquipToolMode(ToolMode.Item);
        }
    }

    private void OnGainFocus()
    {
        _focused = true;
        Locator.GetPromptManager().AddScreenPrompt(_refuelPrompt, PromptPosition.Center, false);
        Locator.GetPromptManager().AddScreenPrompt(_fuelDepletedPrompt, PromptPosition.Center, false);
    }

    private void OnLoseFocus()
    {
        _focused = false;
        Locator.GetPromptManager().RemoveScreenPrompt(_refuelPrompt, PromptPosition.Center);
        Locator.GetPromptManager().RemoveScreenPrompt(_fuelDepletedPrompt, PromptPosition.Center);

        if (_refillingFuel)
        {
            StopRefuel();
        }
    }

    private void StopRefuel()
    {
        _refuelSource.Stop();
        Locator.GetPlayerAudioController().PlayRefuel();
        ShipNotifications.RemoveRefuelingNotification();
        _interactReceiver._hasInteracted = false;
        _refillingFuel = false;
    }

    private bool PlayerMaxFuel()
    {
        return SELocator.GetPlayerResources().GetFuelFraction() >= 1f;
    }

    public float GetFuelRatio()
    {
        return _currentFuel / _maxFuel;
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        _itemCollider.enabled = false;
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        transform.localPosition = new Vector3(0f, -0.7f, 0.2f);
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        transform.localPosition = Vector3.zero;
        _itemCollider.enabled = true;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _interactReceiver.OnPressInteract -= OnPressInteract;
    }
}
