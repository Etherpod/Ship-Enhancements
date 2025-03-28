namespace ShipEnhancements;

public class PersistentInputButton : CockpitButtonSwitch
{
    private ShipPersistentInput _persistentInput;

    protected override void Start()
    {
        base.Start();

        ShipEnhancements.WriteDebugMessage("Persistent input start");
        _persistentInput = SELocator.GetShipBody().GetComponent<ShipPersistentInput>();
        _persistentInput.SetInputEnabled(IsActivated() && !SELocator.GetShipDamageController().IsElectricalFailed());
    }

    public override void OnChangeActiveEvent()
    {
        _persistentInput.SetInputEnabled(IsActivated());
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        if (_electricalDisrupted) return;
        _persistentInput.SetInputEnabled(IsActivated() && !SELocator.GetShipDamageController().IsElectricalFailed());
    }
}
