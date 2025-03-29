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
                && !_autopilot.IsMatchingVelocity() && !_autopilot.IsFlyingToDestination())
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

    private void OnInitMatchVelocity()
    {
        SetActive(!_autopilot.IsFlyingToDestination());
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
