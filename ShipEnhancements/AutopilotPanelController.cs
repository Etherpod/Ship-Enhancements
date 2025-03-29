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

    private void Start()
    {
        _approachAutopilotButton.SetState(true);
        _activeAutopilot = _approachAutopilotButton;
        _matchVelocityButton.SetState(true);
        _activeMatch = _matchVelocityButton;

        _approachAutopilotButton.OnChangeState += (state) => OnChangeState(_approachAutopilotButton, true, state);
        _orbitAutopilotButton.OnChangeState += (state) => OnChangeState(_orbitAutopilotButton, true, state);
        _matchVelocityButton.OnChangeState += (state) => OnChangeState(_matchVelocityButton, false, state);
        _holdPositionButton.OnChangeState += (state) => OnChangeState(_holdPositionButton, false, state);
        _holdInputButton.OnChangeState += (state) => OnChangeState(_holdInputButton, false, state);
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
    }

    public void OnInitAutopilot()
    {
        if (!_activeAutopilot.IsActivated())
        {
            _activeAutopilot.SetActive(true);
            _activeAutopilot.OnChangeActiveEvent();
        }
    }

    public void OnCancelAutopilot()
    {
        if (_activeAutopilot.IsActivated())
        {
            _activeAutopilot.SetActive(false);
            _activeAutopilot.OnChangeActiveEvent();

            if (IsHoldInputSelected())
            {
                _holdInputButton.OnChangeStateEvent();
            }
        }
    }

    public void OnInitMatchVelocity()
    {
        if (!_activeMatch.IsActivated())
        {
            _activeMatch.SetActive(true);
            _activeMatch.OnChangeActiveEvent();
        }
    }

    public void OnCancelMatchVelocity()
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

    public bool IsAutopilotActive()
    {
        return _activeAutopilot.IsActivated();
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
}
