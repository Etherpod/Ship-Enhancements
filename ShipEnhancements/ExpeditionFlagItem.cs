using UnityEngine;

namespace ShipEnhancements;

public class ExpeditionFlagItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.expeditionFlagItemType;

    [SerializeField] private GameObject _itemModelParent;
    [SerializeField] private GameObject _objModelParent;

    private readonly Vector3 _holdOffset = new(0f, -0.4f, 0f);
    private Vector3 _defaultOffset;

    public override string GetDisplayName()
    {
        return "Expedition Flag";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _defaultOffset = _itemModelParent.transform.localPosition;
    }

    private void Start()
    {
        SetIsDropped(false);
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);

        SetIsDropped(false);
        _itemModelParent.transform.localPosition = _defaultOffset + _holdOffset;
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);

        SetIsDropped(true);
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);

        _itemModelParent.transform.localPosition = _defaultOffset;
    }

    public void SetIsDropped(bool dropped)
    {
        _itemModelParent.SetActive(!dropped);
        _objModelParent.SetActive(dropped);
    }
}
