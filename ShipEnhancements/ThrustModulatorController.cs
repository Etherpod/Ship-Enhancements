using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ThrustModulatorController : ElectricalComponent
{
    [SerializeField]
    private OWAudioSource _audioSource;

    private ThrustModulatorButton[] _modulatorButtons;
    private CockpitButtonPanel _buttonPanel;
    private int _lastLevel;
    private int _focusedButtons;
    private bool _focused = false;
    private ElectricalSystem _electricalSystem;
    private bool _wasDisrupted = false;

    public override void Awake()
    {
        _powered = true;
        _buttonPanel = GetComponentInParent<CockpitButtonPanel>();
        if ((bool)enableThrustModulator.GetProperty())
        {
            _buttonPanel.SetThrustModulatorActive(true);
        }
        else
        {
            _buttonPanel.SetThrustModulatorActive(false);
            return;
        }

        base.Awake();

        _modulatorButtons = GetComponentsInChildren<ThrustModulatorButton>();
        ShipEnhancements.Instance.SetThrustModulatorLevel(5);

        _electricalSystem = Locator.GetShipTransform()
            .Find("Module_Cockpit/Systems_Cockpit/FlightControlsElectricalSystem")
            .GetComponent<ElectricalSystem>();
        List<ElectricalComponent> componentList = [.. _electricalSystem._connectedComponents];
        componentList.Add(this);
        _electricalSystem._connectedComponents = [.. componentList];
    }

    private void Start()
    {
        UpdateModulatorDisplay(5);
    }

    private void Update()
    {
        if (_electricalSystem.IsDisrupted() != _wasDisrupted)
        {
            _wasDisrupted = _electricalSystem.IsDisrupted();
            foreach (ThrustModulatorButton button in _modulatorButtons)
            {
                button.SetInteractable(button.GetModulatorLevel() != _lastLevel && !_wasDisrupted);
            }
        }
    }

    public void UpdateModulatorDisplay(int setLevel, bool disable = true)
    {
        if (setLevel > 0)
        {
            _lastLevel = setLevel;
        }
        foreach (ThrustModulatorButton button in _modulatorButtons)
        {
            button.SetButtonLight(button.GetModulatorLevel() <= setLevel);
            if (disable)
            {
                button.SetInteractable(button.GetModulatorLevel() != setLevel && !_wasDisrupted);
            }
            else
            {
                button.SetInteractable(!_wasDisrupted);
            }
        }
    }

    public void DisableModulatorDisplay()
    {
        foreach (ThrustModulatorButton button in _modulatorButtons)
        {
            button.SetButtonLight(false);
            button.SetInteractable(false);
        }
    }

    public override void SetPowered(bool powered)
    {
        if (!(bool)enableThrustModulator.GetProperty()) return;
        if (!_electricalSystem.IsDisrupted())
        {
            base.SetPowered(powered);
            if (powered)
            {
                UpdateModulatorDisplay(_lastLevel);
            }
            else
            {
                DisableModulatorDisplay();
            }
        }
        else
        {
            foreach (ThrustModulatorButton button in _modulatorButtons)
            {
                button.SetButtonLight(powered && button.GetModulatorLevel() <= _lastLevel, true);
            }
        }
    }

    public void PlayButtonSound(AudioClip clip, float volume, int level)
    {
        _audioSource.pitch = level < _lastLevel ? Random.Range(0.9f, 1f) : Random.Range(1f, 1.1f);
        _audioSource.PlayOneShot(clip, volume);
    }

    public void UpdateFocusedButtons(bool add)
    {
        _focusedButtons = Mathf.Max(_focusedButtons + (add ? 1 : -1), 0);
        if (_focused != _focusedButtons > 0)
        {
            _focused = _focusedButtons > 0;
            _buttonPanel.UpdateFocusedButtons(_focused);
        }
    }
}
