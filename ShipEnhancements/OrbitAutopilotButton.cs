namespace ShipEnhancements;

public class OrbitAutopilotButton : CockpitButtonSwitch
{
    private Autopilot _autopilot;
    private OrbitAutopilotTest _orbitAutopilot;

    protected override void Start()
    {
        base.Start();
        _autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
        _orbitAutopilot = SELocator.GetShipBody().GetComponent<OrbitAutopilotTest>();

        _orbitAutopilot.OnAbortAutopilot += OnAbortAutopilot;
        _orbitAutopilot.OnInitOrbit += OnInitOrbit;
    }

    public override void OnChangeActiveEvent()
    {
        if (IsActivated() && Locator.GetReferenceFrame(false) != null && !_autopilot.IsDamaged())
        {
            if (Locator.GetReferenceFrame(false) != null && !_autopilot.IsDamaged())
            {
                _orbitAutopilot.SetOrbitEnabled(true, false);
            }
            else
            {
                SetActive(false);
            }
        }
        else if (_orbitAutopilot.enabled)
        {
            _orbitAutopilot.SetOrbitEnabled(false);
        }
    }

    private void OnAbortAutopilot()
    {
        if (_activated)
        {
            SetActive(false);
        }
    }

    private void OnInitOrbit()
    {
        if (!_activated)
        {
            SetActive(true);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _orbitAutopilot.OnAbortAutopilot -= OnAbortAutopilot;
        _orbitAutopilot.OnInitOrbit -= OnInitOrbit;
    }
}
