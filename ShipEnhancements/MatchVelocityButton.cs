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
            if (Locator.GetReferenceFrame(false) != null && !_autopilot.IsDamaged()
                && !SELocator.GetAutopilotPanelController().IsAutopilotActive())
            {
                _autopilot.StartMatchVelocity(Locator.GetReferenceFrame(false));
            }
            else
            {
                SetActive(false);
            }
        }
        else if (_autopilot.IsMatchingVelocity())
        {
            _autopilot.StopMatchVelocity();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
