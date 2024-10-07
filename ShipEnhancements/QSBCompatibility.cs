using System;

namespace ShipEnhancements;

public class QSBCompatibility
{
    private readonly IQSBAPI _api;

    public QSBCompatibility(IQSBAPI api)
    {
        _api = api;
        _api.OnPlayerJoin().AddListener(OnPlayerJoin);
        _api.RegisterHandler<(string, object)>("settings-data", SettingsDataMessage);
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

    private void SettingsDataMessage(uint id, (string, object) data)
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
}
