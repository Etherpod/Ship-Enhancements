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
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;
    }

    private void Start()
    {
        _interactReceiver.ChangePrompt("Pick up " + GetDisplayName());
    }

    private void Update()
    {
        _refuelPrompt.SetVisibility(false);

        if (_focused)
        {
            OWItem item = Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem();
            if (item != null)
            {
                _interactReceiver.ChangePrompt("Already holding " + item.GetDisplayName());
            }
            else
            {
                _interactReceiver.ChangePrompt("Pick up " + GetDisplayName());
            }

            if (OWInput.IsInputMode(InputMode.Character))
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
                        _refillingFuel = true;
                    }
                    else if (OWInput.IsNewlyReleased(InputLibrary.interactSecondary))
                    {
                        StopRefuel();
                    }
                }
            }
        }

        if (_refillingFuel)
        {
            SELocator.GetPlayerResources()._currentFuel += 30f * Time.deltaTime;
            if (PlayerMaxFuel())
            {
                StopRefuel();
            }
        }
    }

    private void OnPressInteract()
    {
        Locator.GetToolModeSwapper().GetItemCarryTool().PickUpItemInstantly(this);
    }

    private void OnGainFocus()
    {
        _focused = true;
        Locator.GetPromptManager().AddScreenPrompt(_refuelPrompt, PromptPosition.Center, false);
    }

    private void OnLoseFocus()
    {
        _focused = false;
        Locator.GetPromptManager().RemoveScreenPrompt(_refuelPrompt, PromptPosition.Center);

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
        _refillingFuel = false;
    }

    private bool PlayerMaxFuel()
    {
        return SELocator.GetPlayerResources().GetFuelFraction() >= 1f;
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        _itemCollider.enabled = false;
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        _itemCollider.enabled = true;
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        _itemCollider.enabled = true;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _interactReceiver.OnPressInteract -= OnPressInteract;
    }
}
