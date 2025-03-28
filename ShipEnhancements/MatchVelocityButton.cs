namespace ShipEnhancements;

public class MatchVelocityButton : CockpitButtonSwitch
{
    private Autopilot _autopilot;

    public override void Awake()
    {
        base.Awake();
        _autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
        _autopilot.OnInitMatchVelocity += OnInitMatchVelocity;
        // disable event?
    }

    public override void OnChangeActiveEvent()
    {
        if (IsActivated() && Locator.GetReferenceFrame() != null && !_autopilot.IsDamaged())
        {
            _autopilot.StartMatchVelocity(Locator.GetReferenceFrame());
        }
        else if (_autopilot.IsMatchingVelocity())
        {
            _autopilot.StopMatchVelocity();
        }
    }

    private void OnInitMatchVelocity()
    {
        //SetActive(true);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _autopilot.OnInitMatchVelocity -= OnInitMatchVelocity;
    }
}
