﻿using System;
using System.Collections.Generic;

namespace ShipEnhancements;

public class QSBCompatibility
{
    private readonly IQSBAPI _api;
    private List<CockpitSwitch> _activeSwitches = [];
    private ShipEngineSwitch _engineSwitch;

    [Serializable]
    private struct NoData { }

    public QSBCompatibility(IQSBAPI api)
    {
        _api = api;
        _api.OnPlayerJoin().AddListener(OnPlayerJoin);
        _api.RegisterHandler<(string, object)>("settings-data", ReceiveSettingsData);
        _api.RegisterHandler<(string, bool)>("switch-state", ReceiveSwitchState);
        _api.RegisterHandler<NoData>("ship-initialized", ReceiveInitializedShip);
        _api.RegisterHandler<bool>("engine-switch-state", ReceiveEngineSwitchState);
        _api.RegisterHandler<bool>("initialize-engine-switch", InitializeEngineSwitch);
        _api.RegisterHandler<(float, bool)>("ship-oxygen-drain", ReceiveShipOxygenDrain);
        _api.RegisterHandler<(float, bool)>("ship-fuel-drain", ReceiveShipFuelDrain);
        _api.RegisterHandler<float>("set-ship-oxygen", ReceiveShipOxygenValue);
        _api.RegisterHandler<float>("set-ship-fuel", ReceiveShipFuelValue);
    }

    private void OnPlayerJoin(uint playerID)
    {
        if (!_api.GetIsHost() || _api.GetLocalPlayerID() == playerID)
        {
            return;
        }

        SendSettingsData(playerID);
    }

    public void SendSettingsData(uint id)
    {
        var allSettings = Enum.GetValues(typeof(ShipEnhancements.Settings)) as ShipEnhancements.Settings[];
        foreach (var setting in allSettings)
        {
            _api.SendMessage("settings-data", (setting.GetName(), setting.GetProperty()), id, false);
        }
    }

    private void ReceiveSettingsData(uint id, (string, object) data)
    {
        var allSettings = Enum.GetValues(typeof(ShipEnhancements.Settings)) as ShipEnhancements.Settings[];
        foreach (var setting in allSettings)
        {
            if (setting.GetName() == data.Item1)
            {
                setting.SetProperty(data.Item2);
                return;
            }
        }
        ShipEnhancements.WriteDebugMessage($"Setting {data.Item1} not found", error: true);
    }

    public void SendInitializedShip(uint id)
    {
        _api.SendMessage("ship-initialized", new NoData(), id, false);
    }

    private void ReceiveInitializedShip(uint id, NoData noData)
    {
        if (!_api.GetIsHost())
        {
            return;
        }

        if (_activeSwitches.Count > 0)
        {
            foreach (CockpitSwitch cockpitSwitch in _activeSwitches)
            {
                SendSwitchState(id, (cockpitSwitch.GetType().Name, cockpitSwitch.IsOn()));
            }
        }
        if (_engineSwitch != null)
        {
            _api.SendMessage("initialize-engine-switch", ShipEnhancements.Instance.engineOn, id, false);
        }
    }

    public void AddActiveSwitch(CockpitSwitch switchToAdd)
    {
        _activeSwitches.Add(switchToAdd);
    }

    public void RemoveActiveSwitch(CockpitSwitch switchToRemove)
    {
        _activeSwitches.Remove(switchToRemove);
    }

    public void SendSwitchState(uint id, (string, bool) data)
    {
        _api.SendMessage("switch-state", data, id, false);
    }

    private void ReceiveSwitchState(uint id, (string, bool) data)
    {
        foreach (CockpitSwitch cockpitSwitch in _activeSwitches)
        {
            if (cockpitSwitch.GetType().Name == data.Item1)
            {
                cockpitSwitch.ChangeSwitchState(data.Item2);
            }
        }
    }

    public void SetEngineSwitch(ShipEngineSwitch engineSwitch)
    {
        _engineSwitch = engineSwitch;
    }

    public void RemoveEngineSwitch()
    {
        _engineSwitch = null;
    }

    public void SendEngineSwitchState(uint id, bool wasPressed)
    {
        _api.SendMessage("engine-switch-state", wasPressed, id, false);
    }

    private void ReceiveEngineSwitchState(uint id, bool wasPressed)
    {
        _engineSwitch?.UpdateWasPressed(wasPressed);
    }

    private void InitializeEngineSwitch(uint id, bool completedIgnition)
    {
        _engineSwitch.InitializeEngineSwitch(completedIgnition);
    }

    public void SendShipOxygenDrain(uint id, float drainAmount, bool applyMultipliers)
    {
        _api.SendMessage("ship-oxygen-drain", (drainAmount, applyMultipliers), id, false);
    }

    private void ReceiveShipOxygenDrain(uint id, (float, bool) data)
    {
        if (SELocator.GetShipResources() == null) return;

        if (data.Item1 >= 0)
        {
            if (data.Item2)
            {
                SELocator.GetShipResources().DrainOxygen(data.Item1);
            }
            else
            {
                SELocator.GetShipResources()._currentOxygen -= data.Item1;
            }
        }
        else
        {
            SELocator.GetShipResources().AddOxygen(-data.Item1);
        }
    }

    public void SendShipFuelDrain(uint id, float drainAmount, bool applyMultipliers)
    {
        _api.SendMessage("ship-fuel-drain", (drainAmount, applyMultipliers), id, false);
    }

    private void ReceiveShipFuelDrain(uint id, (float, bool) data)
    {
        if (SELocator.GetShipResources() == null) return;

        if (data.Item1 >= 0)
        {
            if (data.Item2)
            {
                SELocator.GetShipResources().DrainFuel(data.Item1);
            }
            else
            {
                SELocator.GetShipResources()._currentFuel -= data.Item1;
            }
        }
        else
        {
            SELocator.GetShipResources().AddFuel(-data.Item1);
        }
    }

    public void SendShipOxygenValue(uint id, float newValue)
    {
        _api.SendMessage("set-ship-oxygen", newValue, id, false);
    }

    public void ReceiveShipOxygenValue(uint id, float newValue)
    {
        SELocator.GetShipResources()?.SetOxygen(newValue);
    }

    public void SendShipFuelValue(uint id, float newValue)
    {
        _api.SendMessage("set-ship-fuel", newValue, id, false);
    }

    public void ReceiveShipFuelValue(uint id, float newValue)
    {
        SELocator.GetShipResources()?.SetFuel(newValue);
    }
}
