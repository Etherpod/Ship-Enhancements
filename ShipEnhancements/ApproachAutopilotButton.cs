namespace ShipEnhancements;

public class ApproachAutopilotButton : CockpitButton
{
    private Autopilot _autopilot;

    public override void Awake()
    {
        base.Awake();
        _autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
        _autopilot.OnAbortAutopilot += OnAbortAutopilot;
        _autopilot.OnInitFlyToDestination += OnInitFlyToDestination;
    }

    public override void OnChangeState()
    {
        if (_on && Locator.GetReferenceFrame() != null && !_autopilot.IsDamaged()
            && Locator.GetReferenceFrame().GetAllowAutopilot())
        {
            _autopilot.FlyToDestination(Locator.GetReferenceFrame());
        }
        else if (_autopilot.IsFlyingToDestination())
        {
            _autopilot.Abort();
        }
    }

    private void OnAbortAutopilot()
    {
        SetState(false);
    }

    private void OnInitFlyToDestination()
    {
        if (!_on)
        {
            SetState(true);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _autopilot.OnAbortAutopilot -= OnAbortAutopilot;
        _autopilot.OnInitFlyToDestination -= OnInitFlyToDestination;
    }
}
