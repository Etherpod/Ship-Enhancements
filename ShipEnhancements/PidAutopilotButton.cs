﻿namespace ShipEnhancements;

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

        _pidAutopilot.OnAbortAutopilot += OnAbortAutopilot;
        _pidAutopilot.OnInitOrbit += OnInitPid;
        GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
    }

    public override void OnChangeActiveEvent()
    {
        if (IsActivated() && Locator.GetReferenceFrame(false) != null && !_autopilot.IsDamaged())
        {
            if (Locator.GetReferenceFrame(false) != null && !_autopilot.IsDamaged())
            {
                _pidAutopilot.SetAutopilotActive(true, _mode, false);
            }
            else
            {
                SetActive(false);
            }
        }
        else if (_pidAutopilot.enabled)
        {
            _pidAutopilot.SetAutopilotActive(false);
        }
    }

    private void OnAbortAutopilot()
    {
        if (_activated)
        {
            SetActive(false);
        }
    }

    private void OnInitPid()
    {
        if (!_activated)
        {
            SetActive(true);
        }
    }

    private void OnEnterFlightConsole(OWRigidbody shipBody)
    {
        AlignShipWithReferenceFrame align = _pidAutopilot.GetComponent<AlignShipWithReferenceFrame>();
        align.enabled = false;
    }

    private void OnExitFlightConsole()
    {
        _pidAutopilot.GetComponent<AlignShipWithReferenceFrame>().enabled = IsActivated();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _pidAutopilot.OnAbortAutopilot -= OnAbortAutopilot;
        _pidAutopilot.OnInitOrbit -= OnInitPid;
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
    }
}
