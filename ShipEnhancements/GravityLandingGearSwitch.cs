namespace ShipEnhancements;

public class GravityLandingGearSwitch : CockpitSwitch
{
    protected override void OnChangeState()
    {
        ShipEnhancements.Instance.SetGravityLandingGearEnabled(_on);
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        if (_electricalDisrupted) return;
        ShipEnhancements.Instance.SetGravityLandingGearEnabled(_on && powered 
            && !SELocator.GetShipDamageController().IsElectricalFailed());
    }
}
