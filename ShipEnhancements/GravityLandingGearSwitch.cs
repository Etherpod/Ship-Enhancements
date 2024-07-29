using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class GravityLandingGearSwitch : CockpitSwitch
{
    public override void Awake()
    {
        base.Awake();
        if (!(bool)enableGravityLandingGear.GetProperty())
        {
            transform.parent.gameObject.SetActive(false);
            return;
        }

        GetComponentInParent<CockpitButtonPanel>().AddButton();
    }

    protected override void OnFlipSwitch(bool state)
    {
        ShipEnhancements.Instance.SetGravityLandingGearEnabled(state);
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        ShipEnhancements.Instance.SetGravityLandingGearEnabled(_on && !Locator.GetShipBody().GetComponent<ShipDamageController>().IsElectricalFailed());
    }
}
