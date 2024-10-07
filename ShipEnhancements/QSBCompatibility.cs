using System;
using System.Collections.Generic;

namespace ShipEnhancements;

public class QSBCompatibility
{
    private readonly IQSBAPI _api;
    private List<CockpitSwitch> _activeSwitches = [];
    private ShipEngineSwitch _engineSwitch;

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
            //ShipEnhancements.WriteDebugMessage("Sending");
            _api.SendMessage("settings-data", (setting.GetName(), setting.GetProperty()), id, false);
        }
    }

    private void ReceiveSettingsData(uint id, (string, object) data)
    {
        //ShipEnhancements.WriteDebugMessage("Received");
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
        _engineSwitch.UpdateWasPressed(wasPressed);
    }

    private void InitializeEngineSwitch(uint id, bool completedIgnition)
    {
        _engineSwitch.InitializeEngineSwitch(completedIgnition);
    }
}
