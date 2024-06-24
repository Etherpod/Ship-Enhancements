using System;

namespace ShipEnhancements;

public class GravityLandingGearSwitch : CockpitSwitch
{
    protected override void Awake()
    {
        if (!ShipEnhancements.Instance.GravityLandingGearEnabled)
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
}
