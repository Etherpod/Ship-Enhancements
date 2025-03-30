using UnityEngine;

namespace ShipEnhancements;

public class AutoAlignDirectionSwitch : CockpitSwitch
{
    private ShipAutoAlign _shipAlign;

    protected override void Start()
    {
        base.Start();
        _shipAlign = SELocator.GetShipBody().GetComponent<ShipAutoAlign>();
    }

    protected override void OnChangeState()
    {
        if (!_on)
        {
            _shipAlign._localAlignmentAxis = new Vector3(0, 0, 1);
        }
        else
        {
            _shipAlign._localAlignmentAxis = new Vector3(0, -1, 0);
        }
    }
}
