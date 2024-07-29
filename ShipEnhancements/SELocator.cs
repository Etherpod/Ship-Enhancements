using System;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public static class SELocator
{
    private static OxygenDetector _shipOxygenDetector;
    private static ShipResources _shipResources;
    private static OxygenVolume _shipOxygenVolume;
    private static PlayerResources _playerResources;
    private static ProbeLauncherComponent _probeLauncherComponent;
    private static ShipTemperatureDetector _shipTemperatureDetector;
    private static ShipDamageController _shipDamageController;

    public static void Initalize()
    {
        _shipResources = Locator.GetShipBody().GetComponent<ShipResources>();
        _shipOxygenVolume = Locator.GetShipBody().GetComponentInChildren<OxygenVolume>();
        _playerResources = Locator.GetPlayerBody().GetComponent<PlayerResources>();

        if ((bool)shipOxygenRefill.GetValue())
        {
            _shipOxygenDetector = Locator.GetShipDetector().gameObject.AddComponent<OxygenDetector>();
        }
        if ((bool)hullTemperatureDamage.GetValue() || (bool)componentTemperatureDamage.GetValue())
        {
            _shipTemperatureDetector = Locator.GetShipDetector().gameObject.AddComponent<ShipTemperatureDetector>();
        }
    }

    public static OxygenDetector GetShipOxygenDetector()
    {
        return _shipOxygenDetector;
    }

    public static ShipResources GetShipResources()
    {
        return _shipResources;
    }

    public static OxygenVolume GetShipOxygenVolume()
    {
        return _shipOxygenVolume;
    }

    public static PlayerResources GetPlayerResources()
    {
        return _playerResources;
    }

    public static ProbeLauncherComponent GetProbeLauncherComponent()
    {
        return _probeLauncherComponent;
    }

    public static void SetProbeLauncherComponent(ProbeLauncherComponent obj)
    {
        _probeLauncherComponent = obj;
    }

    public static ShipTemperatureDetector GetShipTemperatureDetector()
    {
        return _shipTemperatureDetector;
    }

    public static ShipDamageController GetShipDamageController()
    {
        return _shipDamageController;
    }
}
