using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class ReactorHeatController : MonoBehaviour
{
	private ShipReactorComponent _reactor;
	private float _targetHeatRatio;
	private float _currentHeatRatio;
	private float _additiveHeat;
	private float _overloadHeat;
	private bool _updateCountdown;
	private float _countdownLerp;
	private List<PortableCampfire> _activeCampfires = [];

	private void Awake()
	{
		_reactor = GetComponent<ShipReactorComponent>();
		_reactor.OnDamaged += OnReactorDamaged;
		_reactor.OnRepaired += OnReactorRepaired;
		ShipEnhancements.Instance.OnEngineStateChanged += OnEngineStateChanged;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
	}

	public void OnReactorUpdate()
	{
		_currentHeatRatio = Mathf.MoveTowards(_currentHeatRatio, _targetHeatRatio, 0.05f * Time.deltaTime);
		
		if (_updateCountdown)
		{
			_reactor._criticalTimer -= Time.deltaTime;
			if (_reactor._criticalTimer <= 0f + _currentHeatRatio * _reactor._criticalCountdown)
			{
				_reactor._shipDamageController.Explode();
				_reactor.enabled = false;
				return;
			}

			_countdownLerp = 1f - _reactor._criticalTimer / _reactor._criticalCountdown;
			float angleLerp = Mathf.LerpAngle(_reactor._startArrowRotation, _reactor._endArrowRotation, _countdownLerp + _currentHeatRatio);

			float noise = Mathf.PerlinNoise(Time.time * 10f, Time.time * 10f) * 2f - 1f;
			angleLerp += noise * 5f;

			_reactor._timerArrow.localEulerAngles = new Vector3(angleLerp, 0f, 0f);
		}
		else
		{
			float speed;
			if (!ShipEnhancements.Instance.engineOn && _reactor.isDamaged)
			{
				speed = 0.02f;
			}
			else
			{
				speed = 0.75f;
			}
			
			_countdownLerp = Mathf.Max(0, _countdownLerp - speed * Time.deltaTime);
			float nextAngle =  Mathf.LerpAngle(_reactor._startArrowRotation, _reactor._endArrowRotation, _countdownLerp + _currentHeatRatio);
			_reactor._timerArrow.localEulerAngles = new Vector3(nextAngle, 0f, 0f);
		}
	}

    private void OnReactorDamaged(ShipComponent component)
    {
		UpdateReactorState();
    }

    private void OnReactorRepaired(ShipComponent component)
    {
	    UpdateReactorState();
    }
    
    private void OnEngineStateChanged(bool state)
    {
		UpdateReactorState();
    }

    private void UpdateReactorState()
    {
	    bool lastState = _updateCountdown;
	    _updateCountdown = _reactor._damaged && ShipEnhancements.Instance.engineOn;
	    if (_updateCountdown && !lastState)
	    {
		    _reactor._criticalTimer = Mathf.Lerp(_reactor._criticalCountdown, 0, _countdownLerp);
	    }
    }

    public void SetAdditiveHeat(float heatPercent)
	{
		_additiveHeat = heatPercent;
		_targetHeatRatio = _additiveHeat + _overloadHeat;
		if (_targetHeatRatio >= 1f)
		{
			_reactor._shipDamageController.Explode();
		}
	}

    public void SetOverloadHeat(float heatPercent)
	{
		_overloadHeat = heatPercent;
		_targetHeatRatio = _additiveHeat + _overloadHeat;
		if (_targetHeatRatio >= 1f)
		{
            ErnestoDetectiveController.SetReactorCause("overloaded");
            _reactor._shipDamageController.Explode();
		}
	}

	public float GetHeatRatio()
	{
		return _currentHeatRatio + _countdownLerp + _activeCampfires.Count * 0.2f;
	}

	public bool IsOverloaded()
	{
		return _overloadHeat > 0f;
	}

	public void AddCampfire(PortableCampfire campfire)
	{
		if (!_activeCampfires.Contains(campfire))
		{
			_activeCampfires.Add(campfire);
		}
	}

	public void RemoveCampfire(PortableCampfire campfire)
	{
		if (_activeCampfires.Contains(campfire))
		{
			_activeCampfires.Remove(campfire);
		}
	}

	private void OnShipSystemFailure()
	{
		enabled = false;
	}

	private void OnDestroy()
	{
		_reactor.OnDamaged -= OnReactorDamaged;
		_reactor.OnRepaired -= OnReactorRepaired;
		ShipEnhancements.Instance.OnEngineStateChanged -= OnEngineStateChanged;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
