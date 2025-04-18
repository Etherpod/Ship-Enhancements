namespace ShipEnhancements;

public class GravityGearInvertSwitch : CockpitSwitch
{
    protected override void OnChangeState()
    {
        ShipEnhancements.Instance.SetGravityLandingGearInverted(_on);
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        if (_electricalDisrupted) return;
        ShipEnhancements.Instance.SetGravityLandingGearInverted(_on 
            && !SELocator.GetShipDamageController().IsElectricalFailed());
    }
}
