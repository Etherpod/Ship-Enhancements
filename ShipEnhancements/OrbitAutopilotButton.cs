namespace ShipEnhancements;

public class OrbitAutopilotButton : CockpitButton
{
    private Autopilot _autopilot;
    private OrbitAutopilotTest _orbitAutopilot;

    protected override void Start()
    {
        base.Start();
        _autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
        _orbitAutopilot = SELocator.GetShipBody().GetComponent<OrbitAutopilotTest>();
    }

    public override void OnChangeState()
    {
        if (_on && Locator.GetReferenceFrame() != null && !_autopilot.IsDamaged())
        {
            _orbitAutopilot.SetOrbitEnabled(true, false);
        }
        else if (_orbitAutopilot.enabled)
        {
            _orbitAutopilot.SetOrbitEnabled(false);
        }
    }
}
