using System;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public static class ShipNotifications
{
    private static NotificationData _oxygenDepletedNotification = new NotificationData(NotificationTarget.Ship, "SHIP OXYGEN DEPLETED", 5f, true);

    private static NotificationData _oxygenLowNotification = new NotificationData(NotificationTarget.Ship, "SHIP OXYGEN LOW", 5f, true);
    private static NotificationData _oxygenCriticalNotification = new NotificationData(NotificationTarget.Ship, "SHIP OXYGEN CRITICAL", 5f, true);

    private static NotificationData _oxygenRefillingNotification = new NotificationData(NotificationTarget.Ship, "REFILLING OXYGEN TANK", 5f, true);
    private static NotificationData _oxygenNotRefillingNotification = new NotificationData(NotificationTarget.Ship, "DRAINING OXYGEN TANK >:)", 5F, true);

    private static NotificationData _fuelLowNotification = new NotificationData(NotificationTarget.Ship, "SHIP FUEL LOW", 5f, true);
    private static NotificationData _fuelCriticalNotification = new NotificationData(NotificationTarget.Ship, "SHIP FUEL CRITICAL", 5f, true);

    private static NotificationData _spinSpeedHighNotification = new NotificationData(NotificationTarget.Ship, "HULL INTEGRITY LOW", 5f, true);
    private static NotificationData _spinSpeedCriticalNotification = new NotificationData(NotificationTarget.Ship, "HULL INTEGRITY CRITICAL", 5f, true);

    private static NotificationData _temperatureHighNotification = new NotificationData(NotificationTarget.Ship, "HULL TEMPERATURE INCREASING", 5f, true);
    private static NotificationData _temperatureLowNotification = new NotificationData(NotificationTarget.Ship, "HULL TEMPERATURE DECREASING", 5f, true);
    private static NotificationData _temperatureCriticalNotification = new NotificationData(NotificationTarget.Ship, "HULL TEMPERATURE CRITICAL", 5f, true);

    private static NotificationData _scoutInShipNotification = new NotificationData(NotificationTarget.Player, "SCOUT DOCKED IN SHIP", 5f, true);
    private static NotificationData _noScoutInShipNotification = new NotificationData(NotificationTarget.Ship, "SCOUT LAUNCHER EMPTY", 5f, true);

    private static NotificationData _playerRefueling = new NotificationData(NotificationTarget.Player, "REFUELING", 5f, true);

    private static bool _oxygenLow = false;
    private static bool _oxygenCritical = false;
    private static bool _fuelLow = false;
    private static bool _fuelCritical = false;
    private static bool _hullIntegrityLow = false;
    private static bool _hullIntegrityCritical = false;
    private static bool _hullTemperatureHigh = false;
    private static bool _hullTemperatureCritical = false;

    private static bool _refillingShipOxygen = false;
    private static float _lastShipOxygen;
    private static float _lastShipFuel;

    public static void Initialize()
    {
        _oxygenLow = false;
        _oxygenCritical = false;
        _fuelLow = false;
        _fuelCritical = false;
        _hullIntegrityLow = false;
        _hullIntegrityCritical = false;
        _hullTemperatureHigh = false;
        _hullTemperatureCritical = false;
        _refillingShipOxygen = false;
        _lastShipOxygen = SELocator.GetShipResources().GetFractionalOxygen();
        _lastShipFuel = SELocator.GetShipResources()._currentFuel;
    }

    public static void UpdateNotifications()
    {
        if ((bool)shipOxygenRefill.GetProperty())
        {
            bool negativeRefill = (float)oxygenRefillMultiplier.GetProperty() < 0f;
            if (!_refillingShipOxygen && ShipEnhancements.Instance.IsShipInOxygen())
            {
                _refillingShipOxygen = true;
                if (negativeRefill)
                {
                    NotificationManager.SharedInstance.PostNotification(_oxygenNotRefillingNotification, false);
                }
                else
                {
                    NotificationManager.SharedInstance.PostNotification(_oxygenRefillingNotification, false);
                }
            }
            else if (_refillingShipOxygen && !ShipEnhancements.Instance.IsShipInOxygen() 
                && (negativeRefill ? SELocator.GetShipResources().GetFractionalOxygen() > 0.01f
                : SELocator.GetShipResources().GetFractionalOxygen() < 0.99f))
            {
                _refillingShipOxygen = false;
            }
        }

        if (SELocator.GetShipResources().GetFractionalOxygen() < _lastShipOxygen)
        {
            if (!_oxygenCritical && SELocator.GetShipResources().GetFractionalOxygen() < 0.15f)
            {
                _oxygenCritical = true;
                NotificationManager.SharedInstance.PostNotification(_oxygenCriticalNotification, false);
            }
            else if (!_oxygenLow && SELocator.GetShipResources().GetFractionalOxygen() < 0.3f)
            {
                _oxygenLow = true;
                NotificationManager.SharedInstance.PostNotification(_oxygenLowNotification, false);
            }
        }
        else
        {
            if (_oxygenCritical && SELocator.GetShipResources().GetFractionalOxygen() > 0.15f)
            {
                _oxygenCritical = false;
            }
            else if (_oxygenLow && SELocator.GetShipResources().GetFractionalOxygen() > 0.3f)
            {
                _oxygenLow = false;
            }
        }

        if (SELocator.GetShipResources()._currentFuel < _lastShipFuel)
        {
            if (!_fuelCritical && SELocator.GetShipResources().GetFractionalFuel() < 0.15f)
            {
                _fuelCritical = true;
                NotificationManager.SharedInstance.PostNotification(_fuelCriticalNotification, false);
            }
            else if (!_fuelLow && SELocator.GetShipResources().GetFractionalFuel() < 0.3f)
            {
                _fuelLow = true;
                NotificationManager.SharedInstance.PostNotification(_fuelLowNotification, false);
            }
        }
        else
        {
            if (_fuelCritical && SELocator.GetShipResources().GetFractionalFuel() > 0.15f)
            {
                _fuelCritical = false;
            }
            else if (_fuelLow && SELocator.GetShipResources().GetFractionalFuel() > 0.3f)
            {
                _fuelLow = false;
            }
        }

        if ((bool)disableRotationSpeedLimit.GetProperty())
        {
            if (!_hullIntegrityCritical && SELocator.GetShipBody().GetAngularVelocity().sqrMagnitude 
                > ShipEnhancements.Instance.levelTwoSpinSpeed * ShipEnhancements.Instance.levelTwoSpinSpeed)
            {
                _hullIntegrityCritical = true;
                NotificationManager.SharedInstance.PostNotification(_spinSpeedCriticalNotification, false);
            }
            else if (_hullIntegrityCritical && SELocator.GetShipBody().GetAngularVelocity().sqrMagnitude 
                < ShipEnhancements.Instance.levelTwoSpinSpeed * ShipEnhancements.Instance.levelTwoSpinSpeed)
            {
                _hullIntegrityCritical = false;
            }

            if (!_hullIntegrityLow && SELocator.GetShipBody().GetAngularVelocity().sqrMagnitude 
                > ShipEnhancements.Instance.levelOneSpinSpeed * ShipEnhancements.Instance.levelOneSpinSpeed)
            {
                _hullIntegrityLow = true;
                NotificationManager.SharedInstance.PostNotification(_spinSpeedHighNotification, false);
            }
            else if (_hullIntegrityLow && SELocator.GetShipBody().GetAngularVelocity().sqrMagnitude 
                < ShipEnhancements.Instance.levelOneSpinSpeed * ShipEnhancements.Instance.levelOneSpinSpeed)
            {
                _hullIntegrityLow = false;
            }
        }

        if (temperatureZonesAmount.GetProperty().ToString() != "None" && SELocator.GetShipTemperatureDetector() != null)
        {
            float hullTempRatio = Mathf.Abs(SELocator.GetShipTemperatureDetector().GetShipTemperatureRatio() - 0.5f);
            if (!_hullTemperatureCritical && hullTempRatio > 0.35f)
            {
                _hullTemperatureCritical = true;
                NotificationManager.SharedInstance.PostNotification(_temperatureCriticalNotification, false);
            }
            else if (_hullTemperatureCritical && hullTempRatio < 0.35f)
            {
                _hullTemperatureCritical = false;
            }

            if (!_hullTemperatureHigh && hullTempRatio > 0.15f)
            {
                _hullTemperatureHigh = true;
                if (SELocator.GetShipTemperatureDetector().GetTemperatureRatio() > 0)
                {
                    NotificationManager.SharedInstance.PostNotification(_temperatureHighNotification, false);
                }
                else
                {
                    NotificationManager.SharedInstance.PostNotification(_temperatureLowNotification, false);
                }
            }
            else if (_hullTemperatureHigh && hullTempRatio < 0.15f)
            {
                _hullTemperatureHigh = false;
            }
        }

        _lastShipOxygen = SELocator.GetShipResources().GetFractionalOxygen();
        _lastShipFuel = SELocator.GetShipResources()._currentFuel;
    }

    public static void OnOxygenDepleted()
    {
        NotificationManager.SharedInstance.PostNotification(_oxygenDepletedNotification, true);
    }

    public static void OnOxygenRestored()
    {
        NotificationManager.SharedInstance.UnpinNotification(_oxygenDepletedNotification);
    }

    public static void PostScoutInShipNotification()
    {
        NotificationManager.SharedInstance.PostNotification(_scoutInShipNotification, false);
    }

    public static void PostScoutLauncherEmptyNotification()
    {
        NotificationManager.SharedInstance.PostNotification(_noScoutInShipNotification, false);
    }

    public static void PostRefuelingNotification()
    {
        if (!NotificationManager.SharedInstance.IsPinnedNotification(_playerRefueling))
        {
            NotificationManager.SharedInstance.PostNotification(_playerRefueling, true);
        }
    }

    public static void RemoveRefuelingNotification()
    {
        NotificationManager.SharedInstance.UnpinNotification(_playerRefueling);
    }
}
