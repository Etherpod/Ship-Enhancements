using UnityEngine;

namespace ShipEnhancements;

public class ReactorHeatController : MonoBehaviour
{
	private ShipReactorComponent _reactor;
	private float _heatRatio = 0f;
	private float _additiveHeat;
	private float _overloadHeat;
	private float _lastHeat;
	private float _initialCountdown;
	private float _initialArrowAngle;
	private float _minReactorCountdown;

	private void Awake()
	{
		_reactor = GetComponent<ShipReactorComponent>();
		_reactor.OnDamaged += OnReactorDamaged;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        _initialArrowAngle = _reactor._startArrowRotation;
		_reactor._timerArrow.localEulerAngles = new Vector3(_reactor._startArrowRotation, 0f, 0f);
	}

	private void LateUpdate()
	{
        float newStart = Mathf.LerpAngle(_initialArrowAngle, _reactor._endArrowRotation, _heatRatio);
        if (Mathf.Abs(newStart - _reactor._startArrowRotation) < 0.1f)
		{
			_reactor._startArrowRotation = newStart;
		}
		else
		{
			_reactor._startArrowRotation = Mathf.MoveTowardsAngle(_reactor._startArrowRotation, newStart, Time.deltaTime * 3f);
		}
        bool updateArrow = !Mathf.Approximately(_reactor._timerArrow.localEulerAngles.x, _reactor._startArrowRotation);

        if (_reactor.enabled && _lastHeat != _heatRatio)
		{
			float timerRatio = _reactor._criticalTimer / _reactor._criticalCountdown;
            _reactor._criticalCountdown -= (_heatRatio - _lastHeat) * _initialCountdown;
			_reactor._criticalTimer = _reactor._criticalCountdown * timerRatio;
			_lastHeat = _heatRatio;
        }
		else if (!_reactor.enabled && updateArrow)
		{
            _reactor._timerArrow.localEulerAngles = new Vector3(_reactor._startArrowRotation, 0f, 0f);
        }
	}

    private void OnReactorDamaged(ShipComponent component)
    {
		_reactor._criticalCountdown -= _heatRatio * _reactor._criticalCountdown;
		_reactor._criticalTimer = _reactor._criticalCountdown;
		_initialCountdown = _reactor._criticalCountdown;
		_lastHeat = _heatRatio;
    }

    public void SetAdditiveHeat(float heatPercent)
	{
		_additiveHeat = heatPercent;
		_heatRatio = _additiveHeat + _overloadHeat;
		if (_heatRatio >= 1f)
		{
			// Add Ernesto detective
			_reactor._shipDamageController.Explode();
		}
	}

	public void SetOverloadHeat(float heatPercent)
	{
		_overloadHeat = heatPercent;
		_heatRatio = _additiveHeat + _overloadHeat;
		if (_heatRatio >= 1f)
		{
			// Add Ernesto detective
			_reactor._shipDamageController.Explode();
		}
	}

	public bool IsOverloaded()
	{
		return _overloadHeat > 0f;
	}

	private void OnShipSystemFailure()
	{
		enabled = false;
	}

	private void OnDestroy()
	{
		_reactor.OnDamaged -= OnReactorDamaged;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
