using UnityEngine;

namespace ShipEnhancements;

public class ApproachAutopilotButton : CockpitButtonSwitch
{
    private Autopilot _autopilot;

    public override void Awake()
    {
        base.Awake();
        _autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
        _autopilot.OnAbortAutopilot += OnAbortAutopilot;
        _autopilot.OnAlreadyAtDestination += OnAbortAutopilot;
        _autopilot.OnArriveAtDestination += ctx => OnAbortAutopilot();
        _autopilot.OnInitFlyToDestination += OnInitFlyToDestination;
    }

    public override void OnChangeActiveEvent()
    {
        if (IsActivated())
        {
            if (Locator.GetReferenceFrame() != null 
                && Locator.GetReferenceFrame().GetAllowAutopilot() && !_autopilot.IsDamaged()
                && !_autopilot.IsFlyingToDestination()
                && Vector3.Distance(SELocator.GetShipBody().GetPosition(), Locator.GetReferenceFrame().GetPosition()) 
                > Locator.GetReferenceFrame().GetAutopilotArrivalDistance())
            {
                _autopilot.FlyToDestination(Locator.GetReferenceFrame());
            }
            else
            {
                SetActive(false);
            }
        }
        else if (_autopilot.IsFlyingToDestination())
        {
            _autopilot.Abort();
        }
    }

    private void OnAbortAutopilot()
    {
        if (_activated)
        {
            SetActive(false);
            SELocator.GetAutopilotPanelController().UpdatePersistentInput();
        }
    }

    private void OnInitFlyToDestination()
    {
        if (!_activated)
        {
            SetActive(true);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _autopilot.OnAbortAutopilot -= OnAbortAutopilot;
        _autopilot.OnAlreadyAtDestination -= OnAbortAutopilot;
        _autopilot.OnArriveAtDestination -= ctx => OnAbortAutopilot();
        _autopilot.OnInitFlyToDestination -= OnInitFlyToDestination;
    }
}
