using UnityEngine;

namespace ShipEnhancements;

public class PortableCampfireItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.PortableCampfireType;

    [SerializeField]
    private GameObject _itemObject;
    [SerializeField]
    private GameObject _campfireObject;

    private float _baseInteractRange;
    private PortableCampfire _campfire;
    private bool _dropped;

    public override string GetDisplayName()
    {
        return "Portable Campfire";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _baseInteractRange = _interactRange;
        _campfire = _campfireObject.GetComponentInChildren<PortableCampfire>();
    }

    private void Start()
    {
        TogglePackUp(true);
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
        transform.localScale = Vector3.one * 0.8f;
        _dropped = false;
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        bool wasCarrying = Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem() == this;
        ShipEnhancements.WriteDebugMessage(wasCarrying);
        base.DropItem(position, normal, parent, sector, customDropTarget);
        transform.localScale = Vector3.one;
        TogglePackUp(false);
        _campfire.UpdateProperties();
        if (wasCarrying && parent.GetComponentInParent<ShipBody>() && PlayerState.IsInsideShip())
        {
            _campfire.UpdateInsideShip(true);
        }
        _dropped = true;
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        transform.localScale = Vector3.one;
        _dropped = false;
    }

    public void TogglePackUp(bool packUp)
    {
        _itemObject.SetActive(packUp);
        _campfireObject.SetActive(!packUp);
        _interactRange = packUp ? _baseInteractRange : 0f;
    }

    public PortableCampfire GetCampfire()
    {
        return _campfire;
    }

    public bool IsUnpacked()
    {
        return _interactRange > 0f && _campfireObject.activeInHierarchy;
    }

    public bool IsDropped()
    {
        return _dropped;
    }
}
