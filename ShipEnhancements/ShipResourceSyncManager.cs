namespace ShipEnhancements;

public class ShipResourceSyncManager
{
    private readonly int _frameDelay = 120;
    private int _currentFrameDelay;
    private QSBCompatibility _qsbCompat;

    private bool _tempSync = false;
    private bool _filthSync = false;

    public ShipResourceSyncManager(QSBCompatibility qsbCompatibility)
    {
        _currentFrameDelay = _frameDelay;
        _qsbCompat = qsbCompatibility;
        _tempSync = (string)ShipEnhancements.Settings.temperatureZonesAmount.GetProperty() != "None";
        _filthSync = (float)ShipEnhancements.Settings.dirtAccumulationTime.GetProperty() > 0f;
    }

    public void Update()
    {
        if (_currentFrameDelay <= 0)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                _qsbCompat.SendShipOxygenValue(id, SELocator.GetShipResources()._currentOxygen);
                _qsbCompat.SendShipFuelValue(id, SELocator.GetShipResources()._currentFuel);
                if (_tempSync)
                {
                    _qsbCompat.SendShipHullTemp(id, SELocator.GetShipTemperatureDetector().GetShipTempMeter());
                }
                if (_filthSync)
                {
                    SELocator.GetCockpitFilthController().BroadcastDirtState();
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
