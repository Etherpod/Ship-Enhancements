using System;
using UnityEngine;

namespace ShipEnhancements;

public class ThrustModulatorController : MonoBehaviour
{
    private ThrustModulatorButton[] _modulatorButtons;

    private void Awake()
    {
        if (!ShipEnhancements.Instance.ThrustModulatorEnabled)
        {
            gameObject.SetActive(false);
            return;
        }

        _modulatorButtons = GetComponentsInChildren<ThrustModulatorButton>();
        GetComponentInParent<CockpitButtonPanel>().AddButton();
    }

    public void UpdateModulatorDisplay(int setLevel)
    {
        foreach (ThrustModulatorButton button in _modulatorButtons)
        {
            button.SetButtonLight(button.GetModulatorLevel() <= setLevel);
        }
    }
}
