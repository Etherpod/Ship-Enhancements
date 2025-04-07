using UnityEngine;

namespace ShipEnhancements;

public class AutopilotPanelController : MonoBehaviour
{
    [SerializeField]
    private CockpitButtonSwitch _approachAutopilotButton;
    [SerializeField]
    private CockpitButtonSwitch _orbitAutopilotButton;
    [SerializeField]
    private CockpitButtonSwitch _matchVelocityButton;
    [SerializeField]
    private CockpitButtonSwitch _holdPositionButton;
    [SerializeField]
    private CockpitButtonSwitch _holdInputButton;

    private CockpitButtonSwitch _activeAutopilot;
    private CockpitButtonSwitch _activeMatch;

    private Autopilot _autopilot;
    private PidAutopilot _pidAutopilot;
    private ShipPersistentInput _persistentInput;
    private ShipAutopilotComponent _autopilotComponent;
    private bool _lastThrusterState = true;

    private void Start()
    {
        _autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();
        _pidAutopilot = SELocator.GetShipBody().GetComponent<PidAutopilot>();
        _persistentInput = SELocator.GetShipBody().GetComponent<ShipPersistentInput>();
        _autopilotComponent = SELocator.GetShipTransform().GetComponentInChildren<ShipAutopilotComponent>();

        _approachAutopilotButton.SetState(true);
        _activeAutopilot = _approachAutopilotButton;
        _matchVelocityButton.SetState(true);
        _activeMatch = _matchVelocityButton;

        _approachAutopilotButton.OnChangeState += (state) => OnChangeState(_approachAutopilotButton, true, state);
        _orbitAutopilotButton.OnChangeState += (state) => OnChangeState(_orbitAutopilotButton, true, state);
        _matchVelocityButton.OnChangeState += (state) => OnChangeState(_matchVelocityButton, false, state);
        _holdPositionButton.OnChangeState += (state) => OnChangeState(_holdPositionButton, false, state);
        _holdInputButton.OnChangeState += (state) => OnChangeState(_holdInputButton, false, state);

        _autopilot.OnAbortAutopilot += OnAbortAutopilot;
        _autopilot.OnAlreadyAtDestination += OnAbortAutopilot;
        _autopilot.OnArriveAtDestination += ctx => OnAbortAutopilot();
        _autopilot.OnInitFlyToDestination += OnInitAutopilot;
        _autopilot.OnMatchedVelocity += CancelMatchVelocity;
        _pidAutopilot.OnAbortOrbit += OnAbortAutopilot;
        _pidAutopilot.OnInitOrbit += OnInitAutopilot;
        _pidAutopilot.OnAbortHoldPosition += OnAbortHoldPosition;
        _pidAutopilot.OnInitHoldPosition += OnInitHoldPosition;
        _autopilotComponent.OnDamaged += ctx => OnAutopilotDamaged();

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
    }

    private void Update()
    {
        bool usable = SELocator.GetShipResources().AreThrustersUsable();
        if (usable != _lastThrusterState)
        {
            _lastThrusterState = usable;
            if (usable)
            {
                OnThrustersUsable();
            }
            else
            {
                OnThrustersBroken();
            }
        }
    }

    private void OnChangeState(CockpitButtonSwitch button, bool autopilot, bool state)
    {
        if (!state) return;

        if (autopilot)
        {
            if (_activeAutopilot != null)
            {
                _activeAutopilot.SetActive(false);
                _activeAutopilot.SetState(false);
                _activeAutopilot.OnChangeActiveEvent();
                _activeAutopilot.OnChangeStateEvent();
            }

            _activeAutopilot = button;
        }
        else
        {
            if (_activeMatch != null)
            {
                _activeMatch.SetActive(false);
                _activeMatch.SetState(false);
                _activeMatch.OnChangeActiveEvent();
                _activeMatch.OnChangeStateEvent();
            }

            _activeMatch = button;
        }
    }

    private void OnDestroy()
    {
        _approachAutopilotButton.OnChangeState -= (state) => OnChangeState(_approachAutopilotButton, true, state);
        _orbitAutopilotButton.OnChangeState -= (state) => OnChangeState(_orbitAutopilotButton, true, state);
        _matchVelocityButton.OnChangeState -= (state) => OnChangeState(_matchVelocityButton, false, state);
        _holdPositionButton.OnChangeState -= (state) => OnChangeState(_holdPositionButton, false, state);
        _holdInputButton.OnChangeState -= (state) => OnChangeState(_holdInputButton, false, state);

        _autopilot.OnAbortAutopilot -= OnAbortAutopilot;
        _autopilot.OnAlreadyAtDestination -= OnAbortAutopilot;
        _autopilot.OnArriveAtDestination -= ctx => OnAbortAutopilot();
        _autopilot.OnInitFlyToDestination -= OnInitAutopilot;
        _autopilot.OnMatchedVelocity -= CancelMatchVelocity;
        _pidAutopilot.OnAbortOrbit -= OnAbortAutopilot;
        _pidAutopilot.OnInitOrbit -= OnInitAutopilot;
        _pidAutopilot.OnAbortHoldPosition -= OnAbortHoldPosition;
        _pidAutopilot.OnInitHoldPosition -= OnInitHoldPosition;
        _autopilotComponent.OnDamaged -= ctx => OnAutopilotDamaged();

        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
    
    private void OnAbortAutopilot()
    {
        if (IsApproachSelected())
        {
            _approachAutopilotButton.SetActive(false);
        }
        else if (IsOrbitSelected())
        {
            _orbitAutopilotButton.SetActive(false);
        }
        if (IsHoldInputSelected() && !IsAutopilotActive() && !_autopilotComponent.isDamaged)
        {
            _persistentInput.SetInputEnabled(true);
        }
    }

    private void OnInitAutopilot()
    {
        if (IsApproachSelected())
        {
            _approachAutopilotButton.SetActive(true);
        }
        else if (IsOrbitSelected())
        {
            _orbitAutopilotButton.SetActive(true);
        }
        if (IsHoldInputSelected())
        {
            _persistentInput.SetInputEnabled(false);
        }
        if (IsHoldPositionSelected())
        {
            _holdPositionButton.SetActive(false);
        }
    }

    private void OnAbortHoldPosition()
    {
        if (IsHoldPositionSelected())
        {
            _holdPositionButton.SetActive(false);
        }
        if (IsHoldInputSelected() && !IsAutopilotActive() && !_autopilotComponent.isDamaged)
        {
            _persistentInput.SetInputEnabled(true);
        }
    }

    private void OnInitHoldPosition()
    {
        if (IsHoldPositionSelected())
        {
            _holdPositionButton.SetActive(true);
        }
        if (IsHoldInputSelected())
        {
            _persistentInput.SetInputEnabled(false);
        }
    }

    private void OnAutopilotDamaged()
    {
        if (_pidAutopilot.enabled)
        {
            _pidAutopilot.SetAutopilotActive(false);
        }
        if (_persistentInput.enabled)
        {
            _persistentInput.SetInputEnabled(false);
        }
    }

    private void OnThrustersUsable()
    {
        if (IsHoldInputSelected() && !IsAutopilotActive() && !_autopilotComponent.isDamaged)
        {
            _persistentInput.SetInputEnabled(true);
        }
    }

    private void OnThrustersBroken()
    {
        CancelAutopilot();
        CancelMatchVelocity();
        _persistentInput.SetInputEnabled(false);
    }

    private void OnShipSystemFailure()
    {
        CancelAutopilot();
        CancelMatchVelocity();
        _persistentInput.SetInputEnabled(false);
        enabled = false;
    }

    public void ActivateAutopilot()
    {
        if (!_activeAutopilot.IsActivated())
        {
            _activeAutopilot.SetActive(true);
            _activeAutopilot.OnChangeActiveEvent();
        }
    }

    public void CancelAutopilot()
    {
        if (_activeAutopilot.IsActivated())
        {
            _activeAutopilot.SetActive(false);
            _activeAutopilot.OnChangeActiveEvent();
        }
    }

    public void ActivateMatchVelocity()
    {
        if (!_activeMatch.IsActivated())
        {
            _activeMatch.SetActive(true);
            _activeMatch.OnChangeActiveEvent();
        }
    }

    public void CancelMatchVelocity()
    {
        if (_activeMatch.IsActivated())
        {
            _activeMatch.SetActive(false);
            _activeMatch.OnChangeActiveEvent();
        }
    }

    public bool IsApproachSelected()
    {
        return _activeAutopilot == _approachAutopilotButton;
    }

    public bool IsOrbitSelected()
    {
        return _activeAutopilot == _orbitAutopilotButton;
    }

    public bool IsAutopilotActive(bool ignoreHoldPosition = false)
    {
        return _autopilot.enabled || (_pidAutopilot.enabled 
            && (!ignoreHoldPosition || _pidAutopilot.GetCurrentMode() == PidMode.Orbit));
    }

    public bool IsPersistentInputActive()
    {
        return _persistentInput.enabled;
    }

    public bool IsMatchVelocitySelected()
    {
        return _activeMatch == _matchVelocityButton;
    }

    public bool IsHoldPositionSelected()
    {
        return _activeMatch == _holdPositionButton;
    }

    public bool IsHoldInputSelected()
    {
        return _activeMatch == _holdInputButton;
    }

    public bool IsAutopilotDamaged()
    {
        return _autopilotComponent.isDamaged;
    }
}
