using UnityEngine;

namespace ShipEnhancements;

public class ResourcePump : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.PortableTractorBeamType;

    private FirstPersonManipulator _cameraManipulator;
    private OWCamera _playerCam;
    private bool _lastFocused = false;

    private ScreenPrompt _switchModePrompt;
    private ScreenPrompt _switchTypePrompt;
    private ScreenPrompt _powerPrompt;

    public override string GetDisplayName()
    {
        return "Resource Pump";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _cameraManipulator = FindObjectOfType<FirstPersonManipulator>();
        _switchModePrompt = new ScreenPrompt(InputLibrary.up, InputLibrary.down, "Switch Mode ()", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _switchTypePrompt = new ScreenPrompt(InputLibrary.left, InputLibrary.right, "Switch Type ()", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _powerPrompt = new ScreenPrompt(InputLibrary.interactSecondary, "Turn On", 0, ScreenPrompt.DisplayState.Normal, false);
    }

    private void Start()
    {
        _playerCam = Locator.GetPlayerCamera();
    }

    private void Update()
    {
        bool focused = _cameraManipulator.GetFocusedOWItem() == this;
        if (_lastFocused != focused)
        {
            PatchClass.UpdateFocusedItems(focused);
            _lastFocused = focused;
        }

        UpdatePromptVisibility();
    }

    private void UpdatePromptVisibility()
    {
        bool flag = _lastFocused && _playerCam.enabled && OWInput.IsInputMode(InputMode.Character | InputMode.ShipCockpit);
        if (flag != _switchModePrompt.IsVisible())
        {
            _switchModePrompt.SetVisibility(flag);
        }
        if (flag != _switchModePrompt.IsVisible())
        {
            _switchTypePrompt.SetVisibility(flag);
        }
        if (flag != _switchModePrompt.IsVisible())
        {
            _powerPrompt.SetVisibility(flag);
        }
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        transform.localScale = Vector3.one * 0.5f;
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        transform.localPosition = new Vector3(0f, -0.2f, 0f);
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }
}
