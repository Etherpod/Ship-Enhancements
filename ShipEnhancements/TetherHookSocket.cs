using UnityEngine;

namespace ShipEnhancements;

public class TetherHookSocket : OWItemSocket
{
    private TetherHookItem _hookItem;

    public override void Awake()
    {
        Reset();
        _sector = SELocator.GetShipSector();
        base.Awake();
        _acceptableType = ShipEnhancements.Instance.TetherHookType;

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
    }

    private void OnShipSystemFailure()
    {
        _sector = null;
        _socketedItem?.SetSector(null);
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
