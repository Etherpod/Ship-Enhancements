namespace ShipEnhancements;

public class PersistentInputButton : CockpitButton
{
    private ShipPersistentInput _persistentInput;

    protected override void Start()
    {
        base.Start();

        ShipEnhancements.WriteDebugMessage("Persistent input start");
        _persistentInput = SELocator.GetShipBody().GetComponent<ShipPersistentInput>();
        _persistentInput.SetInputEnabled(_on && !SELocator.GetShipDamageController().IsElectricalFailed());
    }

    public override void OnChangeState()
    {
        _persistentInput.SetInputEnabled(_on);
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        if (_electricalDisrupted) return;
        _persistentInput.SetInputEnabled(_on && !SELocator.GetShipDamageController().IsElectricalFailed());
    }
}
