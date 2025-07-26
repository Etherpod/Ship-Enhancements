using UnityEngine;

namespace ShipEnhancements;

public class ResourcePump : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.ResourcePumpType;

    [Space]
    [SerializeField]
    private Vector3 _holdPosition;
    [SerializeField]
    private Vector3 _holdRotation;
    [SerializeField]
    private Vector3 _holdScale = Vector3.one;
    [SerializeField]
    private GameObject[] _socketObjects = [];

    private FirstPersonManipulator _cameraManipulator;
    private OWCamera _playerCam;
    private bool _lastFocused = false;

    private ScreenPrompt _switchTypePrompt;
    private ScreenPrompt _switchModePrompt;
    private ScreenPrompt _powerPrompt;

    private ResourceType _currentType = ResourceType.Fuel;
    private bool _isOutput = true;
    private bool _powered = false;

    public enum ResourceType
    {
        Fuel = 0,
        Oxygen = 1,
        Water = 2
    }

    public override string GetDisplayName()
    {
        return "Resource Pump";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _cameraManipulator = FindObjectOfType<FirstPersonManipulator>();
        _switchTypePrompt = new ScreenPrompt(InputLibrary.left, InputLibrary.right, "Switch Type (Fuel)", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _switchModePrompt = new ScreenPrompt(InputLibrary.up, InputLibrary.down, "Switch Mode (Output)", ScreenPrompt.MultiCommandType.POS_NEG, 0, ScreenPrompt.DisplayState.Normal, false);
        _powerPrompt = new ScreenPrompt(InputLibrary.interactSecondary, "Turn On", 0, ScreenPrompt.DisplayState.Normal, false);
        foreach (var obj in _socketObjects)
        {
            obj.SetActive(false);
        }
    }

    private void Start()
    {
        _playerCam = Locator.GetPlayerCamera();
        Locator.GetPromptManager().AddScreenPrompt(_switchTypePrompt, PromptPosition.Center);
        Locator.GetPromptManager().AddScreenPrompt(_switchModePrompt, PromptPosition.Center);
        Locator.GetPromptManager().AddScreenPrompt(_powerPrompt, PromptPosition.Center);
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

        if (_lastFocused)
        {
            bool pressedLeft = OWInput.IsNewlyPressed(InputLibrary.toolOptionLeft, InputMode.Character);
            if (pressedLeft || OWInput.IsNewlyPressed(InputLibrary.toolOptionRight, InputMode.Character))
            {
                int next = ((int)_currentType + (pressedLeft ? -1 : 1)) % 3;
                _currentType = (ResourceType)next;
                _switchTypePrompt.SetText($"Switch Type ({_currentType})");
            }

            bool pressedDown = OWInput.IsNewlyPressed(InputLibrary.toolOptionDown, InputMode.Character);
            if (pressedDown || OWInput.IsNewlyPressed(InputLibrary.toolOptionUp, InputMode.Character))
            {
                _isOutput = !_isOutput;
                _switchModePrompt.SetText($"Switch Mode ({(_isOutput ? "Output" : "Input")})");
            }

            if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary, InputMode.Character))
            {
                _powered = !_powered;
                _powerPrompt.SetText(_powered ? "Turn Off" : "Turn On");
            }
        }
    }

    private void UpdatePromptVisibility()
    {
        bool flag = _lastFocused && _playerCam.enabled && OWInput.IsInputMode(InputMode.Character | InputMode.ShipCockpit);
        if (flag != _switchModePrompt.IsVisible())
        {
            _switchTypePrompt.SetVisibility(flag);
        }
        if (flag != _switchModePrompt.IsVisible())
        {
            _switchModePrompt.SetVisibility(flag);
        }
        if (flag != _powerPrompt.IsVisible())
        {
            _powerPrompt.SetVisibility(flag);
        }
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        transform.localScale = Vector3.one;
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        transform.localPosition = _holdPosition;
        transform.localRotation = Quaternion.Euler(_holdRotation);
        transform.localScale = _holdScale;
        foreach (var obj in _socketObjects)
        {
            obj.SetActive(false);
        }
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        transform.localScale = Vector3.one;
        foreach (var obj in _socketObjects)
        {
            obj.SetActive(true);
        }
    }
}
