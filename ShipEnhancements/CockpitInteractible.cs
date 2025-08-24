using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class CockpitInteractible : ElectricalComponent
{
    [SerializeField]
    protected InteractReceiver _interactReceiver;

    protected ElectricalSystem _electricalSystem;
    protected bool _focused = false;
    protected bool _electricalDisrupted = false;
    protected bool _lastPoweredState = false;
    protected float _baseInteractRange;

    public override void Awake()
    {
        base.Awake();

        _electricalSystem = FindObjectOfType<ShipBody>().transform
            .Find("Module_Cockpit/Systems_Cockpit/FlightControlsElectricalSystem")
            .GetComponent<ElectricalSystem>();
        _baseInteractRange = _interactReceiver._interactRange;

        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;

        if ((bool)buttonsRequireFlightChair.GetProperty())
        {
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
            GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
            _interactReceiver._interactRange = 0f;
        }
    }

    protected virtual void AddToElectricalSystem()
    {
        List<ElectricalComponent> componentList = [.. _electricalSystem._connectedComponents];
        componentList.Add(this);
        _electricalSystem._connectedComponents = [.. componentList];
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

    protected virtual void OnEnterFlightConsole(OWRigidbody body)
    {
        _interactReceiver._interactRange = _baseInteractRange;
    }

    protected virtual void OnExitFlightConsole()
    {
        _interactReceiver._interactRange = 0f;
    }

    public override void SetPowered(bool powered)
    {
        if (_electricalSystem != null && _electricalDisrupted != _electricalSystem.IsDisrupted())
        {
            _electricalDisrupted = _electricalSystem.IsDisrupted();
            _lastPoweredState = _powered;
        }

        base.SetPowered(powered);
    }

    protected virtual void OnDestroy()
    {
        _interactReceiver.OnPressInteract -= OnPressInteract;
        _interactReceiver.OnReleaseInteract -= OnReleaseInteract;
        _interactReceiver.OnGainFocus -= OnGainFocus;
        _interactReceiver.OnLoseFocus -= OnLoseFocus;

        if ((bool)buttonsRequireFlightChair.GetProperty())
        {
            GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
            GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
        }
    }
}
