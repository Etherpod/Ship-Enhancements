namespace ShipEnhancements;

public class AutoAlignButton : CockpitButton
{
    private ShipAutoAlign _shipAlign;
    private bool _lastThrusterState = true;

    protected override void Start()
    {
        base.Start();
        _shipAlign = SELocator.GetShipBody().GetComponent<ShipAutoAlign>();
    }

    private void Update()
    {
        bool usable = SELocator.GetShipResources().AreThrustersUsable();
        if (_lastThrusterState != usable)
        {
            _lastThrusterState = usable;
            if (usable)
            {
                OnThrustersUsable();
            }
            else
            {
                _shipAlign.enabled = false;
            }
        }
    }

    public override void OnChangeStateEvent()
    {
        _shipAlign.enabled = _on;
    }

    private void OnThrustersUsable()
    {
        _shipAlign.enabled = _on && !SELocator.GetShipDamageController().IsElectricalFailed()
            && SELocator.GetShipResources().AreThrustersUsable();
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        if (_electricalDisrupted) return;
        _shipAlign.enabled = _on && !SELocator.GetShipDamageController().IsElectricalFailed()
            && SELocator.GetShipResources().AreThrustersUsable();
    }
}
