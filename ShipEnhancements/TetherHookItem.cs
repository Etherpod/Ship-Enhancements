using UnityEngine;

namespace ShipEnhancements;

public class TetherHookItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.tetherHookType;

    [SerializeField]
    private GameObject _connectionMesh;
    [SerializeField]
    private Transform _anchorPos;

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
        _connectionMesh.SetActive(false);
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
                _activeTether.CreateTether(Locator.GetPlayerBody(), _anchorPos.localPosition, Vector3.zero);
                ShipEnhancements.Instance.playerTether = _activeTether;
                _connectionMesh.SetActive(true);
                _tetherPrompt.SetText("Detach Tether");
            }
            // if player is tethered to a hook already
            else
            {
                _activeTether = ShipEnhancements.Instance.playerTether;
                _activeTether.TransferTether(GetComponentInParent<OWRigidbody>(), transform.parent.InverseTransformPoint(_anchorPos.position), this);
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
        _connectionMesh.SetActive(false);
        _tetherPrompt.SetText("Attach Tether");
        if (_activeTether == ShipEnhancements.Instance.playerTether)
        {
            ShipEnhancements.Instance.playerTether = null;
        }
    }

    public void DisconnectFromHook()
    {
        _activeTether = _tether;
        _connectionMesh.SetActive(false);
        _tetherPrompt.SetText("Attach Tether");
    }

    public void TransferToHook()
    {
        _connectionMesh.SetActive(true);
        _tetherPrompt.SetText("Detach Tether");
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        _tether.SetAttachedRigidbody(GetComponentInParent<OWRigidbody>());
        Locator.GetPromptManager().AddScreenPrompt(_tetherPrompt, PromptPosition.Center, false);
        transform.localScale = Vector3.one;
    }

    public override void PickUpItem(Transform holdTranform)
    {
        DisconnectTether();
        base.PickUpItem(holdTranform);
        Locator.GetPromptManager().RemoveScreenPrompt(_tetherPrompt);
        transform.localScale = Vector3.one * 0.6f;
        transform.localPosition -= new Vector3(0f, 0.1f, 0f);
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        transform.localScale = Vector3.one * 0.7f;
    }

    public override void OnCompleteUnsocket()
    {
        base.OnCompleteUnsocket();
        transform.localScale = Vector3.one * 0.6f;
        transform.localPosition -= new Vector3(0f, 0.1f, 0f);
    }
}
