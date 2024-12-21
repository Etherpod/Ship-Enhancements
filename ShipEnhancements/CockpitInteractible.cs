using UnityEngine;

namespace ShipEnhancements;

public class CockpitInteractible : ElectricalComponent
{
    [SerializeField]
    protected InteractReceiver _interactReceiver;

    protected bool _focused = false;

    public override void Awake()
    {
        base.Awake();
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;
    }

    protected virtual void OnPressInteract() { }

    protected virtual void OnReleaseInteract() { }

    protected virtual void OnGainFocus()
    {
        if (!_focused)
        {
            _focused = true;
            SELocator.GetFlightConsoleInteractController().AddInteractible();
        }
    }

    protected virtual void OnLoseFocus()
    {
        if (_focused)
        {
            _focused = false;
            SELocator.GetFlightConsoleInteractController().RemoveInteractible();
        }
    }

    protected virtual void OnDestroy()
    {
        _interactReceiver.OnPressInteract -= OnPressInteract;
        _interactReceiver.OnReleaseInteract -= OnReleaseInteract;
        _interactReceiver.OnGainFocus -= OnGainFocus;
        _interactReceiver.OnLoseFocus -= OnLoseFocus;
    }
}
