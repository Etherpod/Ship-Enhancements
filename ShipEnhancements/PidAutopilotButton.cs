namespace ShipEnhancements;

public class PidAutopilotButton : CockpitButtonSwitch
{
    private Autopilot _autopilot;
    private PidAutopilot _pidAutopilot;
    private PidMode _mode;

    public PidAutopilotButton(PidMode mode)
    {
        _mode = mode;
    }

    protected override void Start()
    {
        base.Start();
        _autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
        _pidAutopilot = SELocator.GetShipBody().GetComponent<PidAutopilot>();
    }

    public override void OnChangeActiveEvent()
    {
        if (IsActivated())
        {
            if (CanActivate())
            {
                _pidAutopilot.SetAutopilotActive(true, _mode, false);
            }
            else
            {
                SetActive(false);
            }
        }
        else if (_pidAutopilot.enabled && _pidAutopilot.GetCurrentMode() == _mode)
        {
            _pidAutopilot.SetAutopilotActive(false);
        }
    }

    protected override bool CanActivate()
    {
        return base.CanActivate()
            && Locator.GetReferenceFrame(false) != null && !_autopilot.IsDamaged()
            && SELocator.GetShipResources().AreThrustersUsable();
    }
}
