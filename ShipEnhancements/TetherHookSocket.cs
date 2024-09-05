using UnityEngine;

namespace ShipEnhancements;

public class TetherHookSocket : OWItemSocket
{
    private TetherHookItem _hookItem;

    public override void Awake()
    {
        base.Awake();
        _acceptableType = ShipEnhancements.Instance.tetherHookType;
    }
}
