using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class GravityLandingGearSwitch : CockpitSwitch
{
    public override void Awake()
    {
        base.Awake();
        _buttonPanel.SetGravityLandingGearActive((bool)enableGravityLandingGear.GetProperty());
    }

    protected override void OnFlipSwitch(bool state)
    {
        ShipEnhancements.Instance.SetGravityLandingGearEnabled(state);
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        if (_electricalSystem.IsDisrupted() || !SELocator.GetShipDamageController().IsElectricalFailed()) return;
        ShipEnhancements.Instance.SetGravityLandingGearEnabled(_on && powered && !SELocator.GetShipDamageController().IsElectricalFailed());
    }
}
