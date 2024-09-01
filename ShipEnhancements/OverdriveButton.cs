using System;
using System.Security.Cryptography;
using UnityEngine;

namespace ShipEnhancements;

public class OverdriveButton : MonoBehaviour
{
    [SerializeField]
    [ColorUsage(true, true)]
    private Color _inactiveColor = Color.black;
    [SerializeField]
    [ColorUsage(true, true)]
    private Color _offColor = Color.red;
    [SerializeField]
    [ColorUsage(true, true)]
    private Color _onColor = Color.green;
    [SerializeField]
    private bool _isPrimeButton;
    [SerializeField]
    private OWEmissiveRenderer _emissiveRenderer;

    private InteractReceiver _interactReceiver;
    private ShipOverdriveController _overdriveController;
    private bool _active;
    private bool _on;
    private bool _powered;

    private void Awake()
    {
        _interactReceiver = GetComponent<InteractReceiver>();
        _overdriveController = GetComponentInParent<ShipOverdriveController>();
    }

    private void Start()
    {
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.ChangePrompt(_isPrimeButton ? "Disable safeties" : "Activate Overdrive");
    }

    private void OnPressInteract()
    {
        if (!_active || !_powered) return;

        _on = !_on;
        _emissiveRenderer.SetEmissionColor(_on ? _onColor : _offColor);
        if (!_isPrimeButton)
        {
            _interactReceiver.DisableInteraction();
        }
        else
        {
            _interactReceiver.ChangePrompt(_on ? "Enable safeties" : "Disable safeties");
        }
        _overdriveController.OnPressInteract(_isPrimeButton, _on);
    }

    public bool IsOn()
    {
        return _active && _on;
    }

    public bool IsActive()
    {
        return _active;
    }

    public void OnDisruptedEvent(bool disrupted)
    {
        _interactReceiver.SetInteractionEnabled(!disrupted);
    }

    public void SetButtonActive(bool active)
    {
        _active = active;
        if (!_powered) return;
        if (active)
        {
            _emissiveRenderer.SetEmissionColor(_on ? _onColor : _offColor);
            _interactReceiver.EnableInteraction();
        }
        else
        {
            _emissiveRenderer.SetEmissionColor(_inactiveColor);
            _interactReceiver.DisableInteraction();
            _on = false;
        }
    }

    public void SetButtonOn(bool on)
    {
        _active = true;
        _on = on;
        if (!_powered) return;
        _emissiveRenderer.SetEmissionColor(_on ? _onColor : _offColor);
    }

    public void SetPowered(bool powered, bool disrupted)
    {
        _powered = disrupted ? _powered : powered;
        if (powered)
        {
            if (_active)
            {
                _emissiveRenderer.SetEmissionColor(_on ? _onColor : _offColor);
                if ((_isPrimeButton || !_on) && !disrupted)
                {
                    _interactReceiver.EnableInteraction();
                }
            }
            else
            {
                _emissiveRenderer.SetEmissionColor(_inactiveColor);
            }
        }
        else
        {
            _emissiveRenderer.SetEmissionColor(_inactiveColor);
            if (!disrupted)
            {
                _interactReceiver.DisableInteraction();
            }
        }
    }

    private void OnDestroy()
    {
        _interactReceiver.OnPressInteract -= OnPressInteract;
    }
}
