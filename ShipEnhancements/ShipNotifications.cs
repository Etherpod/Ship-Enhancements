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
    private static NotificationData _oxygenNotRefillingNotification = new NotificationData(NotificationTarget.Ship, "DRAINING OXYGEN TANK >:)", 5f, true);

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

    private static NotificationData _digestionNotification = new NotificationData(NotificationTarget.Ship, "ACIDITY LEVELS CRITICAL", 5f, true);
    
    private static NotificationData _orbitAutopilotNoTargetNotification = new NotificationData(NotificationTarget.Ship, "AUTOPILOT: NO TARGET TO ORBIT", 5f, true);
    
    private static NotificationData _orbitAutopilotActiveNotification = new NotificationData(NotificationTarget.Ship, "AUTOPILOT: ORBITING", 5f, true);
    
    private static NotificationData _orbitAutopilotDisabledNotification = new NotificationData(NotificationTarget.Ship, "AUTOPILOT: ORBIT DISABLED", 5f, true);
    
    private static NotificationData _holdPositionAutopilotActiveNotification = new NotificationData(NotificationTarget.Ship, "AUTOPILOT: HOLDING POSITION", 5f, true);
    
    private static NotificationData _holdPositionAutopilotDisabledNotification = new NotificationData(NotificationTarget.Ship, "AUTOPILOT: HOLD POSITION DISABLED", 5f, true);

    private static NotificationData _fragileShipNotification = new NotificationData(NotificationTarget.Ship, "SHIP DAMAGED: CONTROLS OFFLINE", 5f, true);
    private static NotificationData _stunDamageNotification = new NotificationData(NotificationTarget.Ship, "SHIP CONTROLS OVERLOADED", 5f, true);

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

        if ((bool)enableShipTemperature.GetProperty() && SELocator.GetShipTemperatureDetector() != null)
        {
            float hullTempRatio = SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio() - 0.5f;
            float hullTempAbs = Mathf.Abs(hullTempRatio);
            if (!_hullTemperatureCritical && hullTempAbs > 0.35f)
            {
                _hullTemperatureCritical = true;
                NotificationManager.SharedInstance.PostNotification(_temperatureCriticalNotification, false);
            }
            else if (_hullTemperatureCritical && hullTempAbs < 0.35f)
            {
                _hullTemperatureCritical = false;
            }

            if (!_hullTemperatureHigh && hullTempAbs > 0.15f)
            {
                _hullTemperatureHigh = true;
                if (hullTempRatio > 0)
                {
                    NotificationManager.SharedInstance.PostNotification(_temperatureHighNotification, false);
                }
                else
                {
                    NotificationManager.SharedInstance.PostNotification(_temperatureLowNotification, false);
                }
            }
            else if (_hullTemperatureHigh && hullTempAbs < 0.15f)
            {
                _hullTemperatureHigh = false;
            }
        }

        bool shouldPinFragile = (bool)enableFragileShip.GetProperty() && ShipEnhancements.Instance.anyPartDamaged;
        bool fragilePinned = NotificationManager.SharedInstance.IsPinnedNotification(_fragileShipNotification);
        if (shouldPinFragile && !fragilePinned)
        {
            NotificationManager.SharedInstance.PostNotification(_fragileShipNotification, true);
        }
        else if (!shouldPinFragile && fragilePinned)
        {
            NotificationManager.SharedInstance.UnpinNotification(_fragileShipNotification);
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

    public static void PostDigestionNotification()
    {
        NotificationManager.SharedInstance.PostNotification(_digestionNotification, false);
    }

    public static void PostOrbitAutopilotActiveNotification(float radius)
    {
        _orbitAutopilotActiveNotification.displayMessage = "AUTOPILOT: ORBITING AT " + Mathf.Round(radius) + "m";
        NotificationManager.SharedInstance.PostNotification(_orbitAutopilotActiveNotification, true);
    }
    
    public static void RemoveOrbitAutopilotActiveNotification() =>
        NotificationManager.SharedInstance.UnpinNotification(_orbitAutopilotActiveNotification);

    public static void PostOrbitAutopilotNoTargetNotification() =>
        NotificationManager.SharedInstance.PostNotification(_orbitAutopilotNoTargetNotification, false);

    public static void PostOrbitAutopilotDisabledNotification() =>
        NotificationManager.SharedInstance.PostNotification(_orbitAutopilotDisabledNotification, false);

    public static void PostHoldPositionAutopilotNotification() =>
        NotificationManager.SharedInstance.PostNotification(_holdPositionAutopilotActiveNotification, true);

    public static void RemoveHoldPositionAutopilotNotification() =>
        NotificationManager.SharedInstance.UnpinNotification(_holdPositionAutopilotActiveNotification);

    public static void PostHoldPositionAutopilotDisabledNotification() =>
        NotificationManager.SharedInstance.PostNotification(_holdPositionAutopilotDisabledNotification, false);

    public static void PostStunDamageNotification(float time)
    {
        _stunDamageNotification.minDuration = Mathf.Max(_stunDamageNotification.minDuration, time);
        NotificationManager.SharedInstance.PostNotification(_stunDamageNotification, false);
    }
}
