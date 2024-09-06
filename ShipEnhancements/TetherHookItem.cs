using MonoMod.RuntimeDetour;
using NAudio.Wave;
using UnityEngine;

namespace ShipEnhancements;

public class TetherHookItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.tetherHookType;

    private ShipTether _tether;
    private ShipTether _activeTether;
    private FirstPersonManipulator _cameraManipulator;
    private ScreenPrompt _tetherPrompt;

    public override string GetDisplayName()
    {
        return "Tether Hook";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _tether = GetComponent<ShipTether>();
        _activeTether = _tether;
        _cameraManipulator = Locator.GetPlayerCamera().GetComponent<FirstPersonManipulator>();
        _tetherPrompt = new ScreenPrompt(InputLibrary.interactSecondary, "Attach Tether", 0, ScreenPrompt.DisplayState.Normal, false);
    }

    private void Update()
    {
        bool focused = _cameraManipulator.GetFocusedOWItem() == this;
        _tetherPrompt.SetVisibility(focused);
        if (focused && OWInput.IsNewlyPressed(InputLibrary.interactSecondary))
        {
            OnPressInteract();
        }
    }

    private void OnPressInteract()
    {
        // if untethered
        if (!_activeTether.IsTethered())
        {
            // if player is not tethered to anything
            if (!ShipEnhancements.Instance.playerTether)
            {
                _activeTether.CreateTether(Locator.GetPlayerBody(), Vector3.zero);
                ShipEnhancements.Instance.playerTether = _activeTether;
            }
            // if player is tethered to a hook already
            else
            {
                _activeTether = ShipEnhancements.Instance.playerTether;
                _activeTether.TransferTether(GetComponentInParent<OWRigidbody>(), transform.localPosition, this);
                ShipEnhancements.Instance.playerTether = null;
            }
        }
        // if tethered
        else
        {
            DisconnectTether();
        }
    }

    public void DisconnectTether()
    {
        _activeTether.DisconnectTether();
        _activeTether = _tether;
        if (_activeTether == ShipEnhancements.Instance.playerTether)
        {
            ShipEnhancements.Instance.playerTether = null;
        }
    }

    public void DisconnectFromHook()
    {
        _activeTether = _tether;
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        _tether.SetAttachedRigidbody(GetComponentInParent<OWRigidbody>());
        Locator.GetPromptManager().AddScreenPrompt(_tetherPrompt, PromptPosition.Center, false);
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        Locator.GetPromptManager().RemoveScreenPrompt(_tetherPrompt);
        DisconnectTether();
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
    }
}
