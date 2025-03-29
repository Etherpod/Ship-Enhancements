using UnityEngine;

namespace ShipEnhancements;

public class AutopilotPanelController : MonoBehaviour
{
    [SerializeField]
    private CockpitButtonSwitch[] _leftButtonGroup;
    [SerializeField]
    private CockpitButtonSwitch[] _rightButtonGroup;

    private CockpitButtonSwitch _activeLeft;
    private CockpitButtonSwitch _activeRight;

    private void Start()
    {
        _leftButtonGroup[0].SetState(true);
        _activeLeft = _leftButtonGroup[0];
        _rightButtonGroup[0].SetState(true);
        _activeRight = _rightButtonGroup[0];

        foreach (var button in _leftButtonGroup)
        {
            button.OnChangeState += (state) => OnChangeState(button, true, state);
        }
        foreach (var button in _rightButtonGroup)
        {
            button.OnChangeState += (state) => OnChangeState(button, false, state);
        }
    }

    private void OnChangeState(CockpitButtonSwitch button, bool left, bool state)
    {
        if (!state) return;

        if (left)
        {
            if (_activeLeft != null)
            {
                _activeLeft.SetActive(false);
                _activeLeft.SetState(false);
                _activeLeft.OnChangeActiveEvent();
            }

            _activeLeft = button;
        }
        else
        {
            if (_activeRight != null)
            {
                _activeRight.SetActive(false);
                _activeRight.SetState(false);
                _activeRight.OnChangeActiveEvent();
            }

            _activeRight = button;
        }
    }

    private void OnDestroy()
    {
        foreach (var button in _leftButtonGroup)
        {
            button.OnChangeState -= (state) => OnChangeState(button, true, state);
        }
        foreach (var button in _rightButtonGroup)
        {
            button.OnChangeState -= (state) => OnChangeState(button, false, state);
        }
    }

    public void OnInitAutopilot()
    {
        if (!_activeLeft.IsActivated())
        {
            _activeLeft.SetActive(true);
            _activeLeft.OnChangeActiveEvent();
        }
    }

    public void OnCancelAutopilot()
    {
        if (_activeLeft.IsActivated())
        {
            _activeLeft.SetActive(false);
            _activeLeft.OnChangeActiveEvent();
        }
    }

    public void OnInitMatchVelocity()
    {
        if (!_activeRight.IsActivated())
        {
            _activeRight.SetActive(true);
            _activeRight.OnChangeActiveEvent();
        }
    }

    public void OnCancelMatchVelocity()
    {
        if (_activeRight.IsActivated())
        {
            _activeRight.SetActive(false);
            _activeRight.OnChangeActiveEvent();
        }
    }
}
