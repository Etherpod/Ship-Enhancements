using System;

namespace ShipEnhancements;

public static class ShipRepairLimitController
{
    public delegate void RepairLimitEvent();
    public static event RepairLimitEvent OnDisableRepair;
    public static event RepairLimitEvent OnEnableRepair;

    private static int _numberPartsRepaired;
    private static int _partsLimit = -1;

    public static void AddPartRepaired()
    {
        _numberPartsRepaired++;

        if (!CanRepair())
        {
            OnDisableRepair?.Invoke();
        }
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
