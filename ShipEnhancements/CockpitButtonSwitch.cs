﻿using UnityEngine;

namespace ShipEnhancements;

public class CockpitButtonSwitch : CockpitButton
{
    [SerializeField]
    protected string _activateLabel;
    [SerializeField]
    protected string _deactivateLabel;

    public event CockpitButtonEvent OnChangeActive;

    protected bool _activated;

    protected override void Start()
    {
        _offLabel = _activated ? _deactivateLabel : _activateLabel;
        base.Start();
    }

    protected override void OnPressInteract()
    {
        _pressed = true;
        _buttonTransform.localPosition = _initialButtonPosition + _positionOffset;
        _buttonTransform.localRotation = Quaternion.Euler(_initialButtonRotation.eulerAngles + _rotationOffset);
        if (_pressAudio)
        {
            _audioSource.PlayOneShot(_pressAudio, 0.5f);
        }

        if (!_on)
        {
            SetState(true);
            RaiseChangeStateEvent();
            OnChangeStateEvent();
        }
        else
        {
            SetActive(!_activated);
            RaiseChangeActiveEvent();
            OnChangeActiveEvent();
        }

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendButtonSwitchState(id, this, _on, _activated);
            }
        }
    }

    public virtual void SetActive(bool active)
    {
        _activated = active;
        if (!gameObject.activeInHierarchy) return;
        _offLabel = _activated ? _deactivateLabel : _activateLabel;
        if (!_pressed)
        {
            _interactReceiver.ChangePrompt(_on ? _offLabel : _onLabel);
        }
    }

    public virtual void OnChangeActiveEvent() { }

    protected virtual bool CanActivate()
    {
        return true;
    }

    public void RaiseChangeActiveEvent()
    {
        OnChangeActive?.Invoke(_activated);
    }

    public virtual bool IsActivated()
    {
        return _on && _activated;
    }
}
