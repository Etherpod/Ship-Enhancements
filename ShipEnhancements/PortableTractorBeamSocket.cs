using UnityEngine;

namespace ShipEnhancements;

public class PortableTractorBeamSocket : OWItemSocket
{
    public override void Awake()
    {
        Reset();
        _sector = Locator.GetShipBody().GetComponent<Sector>();
        base.Awake();
        _acceptableType = PortableTractorBeamItem.ItemType;
    }

    public override void Start()
    {
        base.Start();
        if (_socketedItem)
        {
            _socketedItem.transform.localScale = Vector3.one * 0.5f;
        }
    }

    public void SetTractorBeamItem(PortableTractorBeamItem item)
    {
        PlaceIntoSocket(item);
    }
}
