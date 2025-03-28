using UnityEngine;

namespace ShipEnhancements;

public class CockpitButtonSwitch : CockpitButton
{
    [SerializeField]
    protected string _activateLabel;
    [SerializeField]
    protected string _deactivateLabel;

    protected bool _activated;

    protected override void Start()
    {
        base.Start();
        _offLabel = _activateLabel;
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
            OnChangeState();
        }
        else
        {
            SetActive(!_activated);
            OnChangeActive();
        }
    }

    public virtual void SetActive(bool active)
    {
        _activated = active;
        _offLabel = _activated ? _deactivateLabel : _activateLabel;
        if (!_pressed)
        {
            _interactReceiver.ChangePrompt(_on ? _offLabel : _onLabel);
        }
    }

    public virtual void OnChangeActive() { }

    public virtual bool IsActivated()
    {
        return _activated;
    }
}
