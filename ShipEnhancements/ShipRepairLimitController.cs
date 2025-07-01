using System;

namespace ShipEnhancements;

public static class ShipRepairLimitController
{
    public delegate void RepairLimitEvent();
    public static event RepairLimitEvent OnDisableRepair;
    public static event RepairLimitEvent OnEnableRepair;

    private static int _numberPartsRepaired;
    private static int _partsLimit = -1;
    private static FirstPersonManipulator _manipulator;
    private static string _baseRepairPrompt;

    public static void Initialize()
    {
        _manipulator = SELocator.GetPlayerBody().GetComponentInChildren<FirstPersonManipulator>();
        _baseRepairPrompt = _manipulator._repairScreenPrompt.GetText();
    }

    public static void AddPartRepaired()
    {
        _numberPartsRepaired++;

        if (!CanRepair())
        {
            OnDisableRepair?.Invoke();
        }

        RefreshRepairPrompt();
    }

    public static void SetPartsRepaired(int numParts)
    {
        _numberPartsRepaired = numParts;

        if (!CanRepair())
        {
            OnDisableRepair?.Invoke();
        }
        else
        {
            OnEnableRepair?.Invoke();
        }

        RefreshRepairPrompt();
    }

    public static void SetRepairLimit(int maxParts)
    {
        _partsLimit = maxParts;

        if (!CanRepair())
        {
            OnDisableRepair?.Invoke();
        }
        else
        {
            OnEnableRepair?.Invoke();
        }

        RefreshRepairPrompt();
    }

    public static void RefreshRepairPrompt()
    {
        if (Locator._promptManager == null) return;

        if (_partsLimit - _numberPartsRepaired > 0)
        {
            _manipulator?._repairScreenPrompt.SetText(_baseRepairPrompt + $" ({_partsLimit - _numberPartsRepaired})");
        }
    }

    public static int GetPatsRepaired()
    {
        return _numberPartsRepaired;
    }

    public static int GetRepairLimit()
    {
        return _partsLimit;
    }

    public static bool CanRepair()
    {
        return _partsLimit < 0 || _numberPartsRepaired < _partsLimit;
    }
}
