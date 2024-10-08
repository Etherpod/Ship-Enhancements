using System;
using System.Linq;

namespace ShipEnhancements;

public class ShipResourceSyncManager
{
    private readonly int _frameDelay = 50;
    private int _currentFrameDelay;
    private QSBCompatibility _qsbCompat;
    private IQSBAPI _api;

    public ShipResourceSyncManager(IQSBAPI api, QSBCompatibility qsbCompatibility)
    {
        _currentFrameDelay = _frameDelay;
        _api = api;
        _qsbCompat = qsbCompatibility;
    }

    public void Update()
    {
        if (_currentFrameDelay <= 0)
        {
            foreach (uint id in _api.GetPlayerIDs().Where(id => id != ShipEnhancements.QSBAPI.GetLocalPlayerID()))
            {
                _qsbCompat.SendShipOxygenValue(id, SELocator.GetShipResources()._currentOxygen);
                _qsbCompat.SendShipFuelValue(id, SELocator.GetShipResources()._currentFuel);
            }
            _currentFrameDelay = _frameDelay;
        }
        else
        {
            _currentFrameDelay--;
        }
    }
}
