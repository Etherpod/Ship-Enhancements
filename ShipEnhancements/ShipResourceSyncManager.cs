namespace ShipEnhancements;

public class ShipResourceSyncManager
{
    private readonly int _frameDelay = 120;
    private int _currentFrameDelay;
    private QSBCompatibility _qsbCompat;

    private bool TempSync => (string)ShipEnhancements.Settings.temperatureZonesAmount.GetProperty() != "None";
    private bool FilthSync => (float)ShipEnhancements.Settings.dirtAccumulationTime.GetProperty() > 0f;

    public ShipResourceSyncManager(QSBCompatibility qsbCompatibility)
    {
        _currentFrameDelay = _frameDelay;
        _qsbCompat = qsbCompatibility;
    }

    public void Update()
    {
        if (LoadManager.GetCurrentScene() != OWScene.SolarSystem)
        {
            return;
        }

        if (_currentFrameDelay <= 0)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                _qsbCompat.SendShipOxygenValue(id, SELocator.GetShipResources()._currentOxygen);
                _qsbCompat.SendShipFuelValue(id, SELocator.GetShipResources()._currentFuel);
                if (TempSync)
                {
                    _qsbCompat.SendShipHullTemp(id, SELocator.GetShipTemperatureDetector().GetShipTempMeter());
                }
                if (FilthSync)
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
