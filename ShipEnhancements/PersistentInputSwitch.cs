using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class PersistentInputSwitch : CockpitSwitch
{
    private ShipPersistentInput _persistentInput;

    public override void Awake()
    {
        base.Awake();
        _buttonPanel.SetPersistentInputActive((bool)enablePersistentInput.GetProperty());
    }

    protected override void Start()
    {
        base.Start();
        _persistentInput = Locator.GetShipBody().GetComponent<ShipPersistentInput>();
        _persistentInput.SetInputEnabled(_on && !SELocator.GetShipDamageController().IsElectricalFailed());
    }

    protected override void OnFlipSwitch(bool state)
    {
        _persistentInput.SetInputEnabled(state);
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        if (_electricalSystem.IsDisrupted()) return;
        if (_persistentInput)
        {
            _persistentInput.SetInputEnabled(_on && !SELocator.GetShipDamageController().IsElectricalFailed());
        }
    }
}
