namespace ShipEnhancements;

public class PersistentInputButton : CockpitButtonSwitch
{
    private ShipPersistentInput _persistentInput;

    protected override void Start()
    {
        base.Start();

        _persistentInput = SELocator.GetShipBody().GetComponent<ShipPersistentInput>();
    }

    public override void OnChangeActiveEvent()
    {
        if (IsActivated())
        {
            _persistentInput.ResetInput();
        }
        else if (IsOn() && CanActivate())
        {
            _persistentInput.ReadNextInput();
        }
    }

    public override void OnChangeStateEvent()
    {
        _persistentInput.SetInputEnabled(CanActivate());
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        if (_electricalDisrupted) return;
        _persistentInput.SetInputEnabled(CanActivate());
    }

    protected override bool CanActivate()
    {
        return base.CanActivate() && IsOn()
            && !SELocator.GetShipDamageController().IsElectricalFailed()
            && SELocator.GetShipResources().AreThrustersUsable()
            && !SELocator.GetAutopilotPanelController().IsAutopilotDamaged();
    }
}
