namespace ShipEnhancements;

public class ShipResourceSyncManager
{
    private readonly int _frameDelay = 120;
    private int _currentFrameDelay;
    private QSBCompatibility _qsbCompat;

    public ShipResourceSyncManager(QSBCompatibility qsbCompatibility)
    {
        _currentFrameDelay = _frameDelay;
        _qsbCompat = qsbCompatibility;
    }

    public void Update()
    {
        if (_currentFrameDelay <= 0)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                _qsbCompat.SendShipOxygenValue(id, SELocator.GetShipResources()._currentOxygen);
                _qsbCompat.SendShipFuelValue(id, SELocator.GetShipResources()._currentFuel);
                if ((string)ShipEnhancements.Settings.temperatureZonesAmount.GetProperty() != "None")
                {
                    _qsbCompat.SendShipHullTemp(id, SELocator.GetShipTemperatureDetector().GetShipTempMeter());
                }
            }
            _currentFrameDelay = _frameDelay;
        }
        else
        {
            _currentFrameDelay--;
        }
    }
}
