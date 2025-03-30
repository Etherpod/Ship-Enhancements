namespace ShipEnhancements;

public class AutoAlignButton : CockpitButton
{
    private ShipAutoAlign _shipAlign;

    protected override void Start()
    {
        base.Start();
        _shipAlign = SELocator.GetShipBody().GetComponent<ShipAutoAlign>();
    }

    public override void OnChangeStateEvent()
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
