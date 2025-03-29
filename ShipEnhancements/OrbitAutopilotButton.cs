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
        GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
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

    private void OnEnterFlightConsole(OWRigidbody shipBody)
    {
        AlignShipWithReferenceFrame align = _orbitAutopilot.GetComponent<AlignShipWithReferenceFrame>();
        align.enabled = false;
    }

    private void OnExitFlightConsole()
    {
        _orbitAutopilot.GetComponent<AlignShipWithReferenceFrame>().enabled = IsActivated();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _orbitAutopilot.OnAbortAutopilot -= OnAbortAutopilot;
        _orbitAutopilot.OnInitOrbit -= OnInitOrbit;
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
    }
}
