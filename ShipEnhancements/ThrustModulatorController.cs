﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ThrustModulatorController : ElectricalComponent
{
    [SerializeField]
    private OWAudioSource _audioSource;

    private ThrustModulatorButton[] _modulatorButtons;
    private int _lastLevel;

    public override void Awake()
    {
        _powered = true;
        if (!(bool)enableThrustModulator.GetProperty())
        {
            gameObject.SetActive(false);
            return;
        }

        _modulatorButtons = GetComponentsInChildren<ThrustModulatorButton>();

        GetComponentInParent<CockpitButtonPanel>().AddButton();
        ShipEnhancements.Instance.SetThrustModulatorLevel(5);
        _lastLevel = 5;

        ElectricalSystem cockpitElectricalSystem = Locator.GetShipBody().transform
            .Find("Module_Cockpit/Systems_Cockpit/FlightControlsElectricalSystem")
            .GetComponent<ElectricalSystem>();
        List<ElectricalComponent> componentList = cockpitElectricalSystem._connectedComponents.ToList();
        componentList.Add(this);
        cockpitElectricalSystem._connectedComponents = componentList.ToArray();
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
        }
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        if (powered)
        {
            UpdateModulatorDisplay(_lastLevel);
            foreach (ThrustModulatorButton button in _modulatorButtons)
            {
                button.SetInteractable(true);
            }
        }
        else
        {
            UpdateModulatorDisplay(0);
            foreach (ThrustModulatorButton button in _modulatorButtons)
            {
                button.SetInteractable(false);
            }
        }
    }

    public void PlayButtonSound(AudioClip clip, float volume)
    {
        _audioSource.pitch = Random.Range(0.9f, 1.1f);
        _audioSource.PlayOneShot(clip, volume);
    }
}
