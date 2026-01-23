using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipResourceSyncManager
{
    private readonly int _frameDelay = 120;
    private int _currentFrameDelay;
    private QSBCompatibility _qsbCompat;

    private bool TempSync => (bool)enableShipTemperature.GetProperty();

    public ShipResourceSyncManager(QSBCompatibility qsbCompatibility)
    {
        _currentFrameDelay = _frameDelay;
        _qsbCompat = qsbCompatibility;
    }

    public void Update()
    {
        if (LoadManager.GetCurrentScene() != OWScene.SolarSystem || 
            ShipEnhancements.Instance.IsWarpingBackToEye)
        {
            return;
        }

        if (_currentFrameDelay <= 0)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                _qsbCompat.SendShipOxygenValue(id, SELocator.GetShipResources()._currentOxygen);
                _qsbCompat.SendShipFuelValue(id, SELocator.GetShipResources()._currentFuel);
                SELocator.GetCockpitFilthController()?.BroadcastCurrentEffectState();
                
                if (TempSync)
                {
                    _qsbCompat.SendShipHullTemp(id, SELocator.GetShipTemperatureDetector().GetCurrentInternalTemperature());
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
