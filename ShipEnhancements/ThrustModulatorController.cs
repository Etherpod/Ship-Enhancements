using System.Collections.Generic;
using System.Linq;
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

    public override void Awake()
    {
        _powered = true;
        _buttonPanel = GetComponentInParent<CockpitButtonPanel>();
        _buttonPanel.SetThrustModulatorActive((bool)enableThrustModulator.GetProperty());

        _modulatorButtons = GetComponentsInChildren<ThrustModulatorButton>();
        ShipEnhancements.Instance.SetThrustModulatorLevel(5);

        ElectricalSystem cockpitElectricalSystem = Locator.GetShipBody().transform
            .Find("Module_Cockpit/Systems_Cockpit/FlightControlsElectricalSystem")
            .GetComponent<ElectricalSystem>();
        List<ElectricalComponent> componentList = cockpitElectricalSystem._connectedComponents.ToList();
        componentList.Add(this);
        cockpitElectricalSystem._connectedComponents = componentList.ToArray();
    }

    private void Start()
    {
        UpdateModulatorDisplay(5);
    }

    public void UpdateModulatorDisplay(int setLevel)
    {
        if (setLevel > 0)
        {
            _lastLevel = setLevel;
        }
        foreach (ThrustModulatorButton button in _modulatorButtons)
        {
            button.SetButtonLight(button.GetModulatorLevel() <= setLevel);
            button.SetInteractable(button.GetModulatorLevel() != setLevel);
        }
    }

    public override void SetPowered(bool powered)
    {
        if (!(bool)enableThrustModulator.GetProperty()) return;
        base.SetPowered(powered);
        if (powered)
        {
            UpdateModulatorDisplay(_lastLevel);
        }
        else
        {
            UpdateModulatorDisplay(0);
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
