namespace ShipEnhancements;

public class MatchVelocityButton : CockpitButtonSwitch
{
    private Autopilot _autopilot;

    public override void Awake()
    {
        base.Awake();
        _autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
    }

    public override void OnChangeActiveEvent()
    {
        if (IsActivated())
        {
            if (CanActivate())
            {
                if (ShipEnhancements.GEInteraction != null)
                {
                    ShipEnhancements.GEInteraction.EnableContinuousMatchVelocity();
                }
                _autopilot.StartMatchVelocity(SELocator.GetReferenceFrame(ignorePassiveFrame: false));
            }
            else
            {
                SetActive(false);
            }
        }
        else if (ShipEnhancements.GEInteraction != null 
            && ShipEnhancements.GEInteraction.IsContinuousMatchVelocityEnabled())
        {
            ShipEnhancements.GEInteraction.StopContinuousMatchVelocity();
        }
        else if (_autopilot.IsMatchingVelocity())
        {
            _autopilot.StopMatchVelocity();
        }
    }

    protected override bool CanActivate()
    {
        return base.CanActivate()
            && SELocator.GetReferenceFrame(ignorePassiveFrame: false) != null && !_autopilot.IsDamaged()
            && !SELocator.GetAutopilotPanelController().IsAutopilotActive()
            && SELocator.GetShipResources().AreThrustersUsable();
    }
}
