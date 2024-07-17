using System;
using UnityEngine;

namespace ShipEnhancements;

public class PortableCampfireItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.portableCampfireType;

    [SerializeField]
    private GameObject _itemObject;
    [SerializeField]
    private GameObject _campfireObject;

    private float _baseInteractRange;
    private PortableCampfire _campfire;

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

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
        transform.localScale = Vector3.one * 0.8f;
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        transform.localScale = Vector3.one;
        TogglePackUp(false);
        _campfire.UpdateProperties();
        if (parent.GetComponentInParent<ShipBody>() && PlayerState.IsInsideShip())
        {
            _campfire.UpdateInsideShip(true);
        }
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        transform.localScale = Vector3.one;
    }

    public void TogglePackUp(bool packUp)
    {
        _itemObject.SetActive(packUp);
        _campfireObject.SetActive(!packUp);
        _interactRange = packUp ? _baseInteractRange : 0f;
    }
}
