namespace ShipEnhancements;

public class AutoAlignSwitch : CockpitSwitch
{
    private ShipAutoAlign _shipAlign;

    protected override void Start()
    {
        base.Start();
        _shipAlign = SELocator.GetShipBody().GetComponent<ShipAutoAlign>();
    }

    protected override void OnChangeState()
    {
        _shipAlign.enabled = _on;
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        if (_electricalDisrupted) return;
        _shipAlign.enabled = _on && powered;
    }
}
