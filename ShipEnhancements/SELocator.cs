using System.Linq;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public static class SELocator
{
    private static ShipBody _shipBody;
    private static Transform _shipTransform;
    private static GameObject _shipDetector;
    private static PlayerBody _playerBody;
    private static SurveyorProbe _probe;
    private static OxygenDetector _shipOxygenDetector;
    private static ShipResources _shipResources;
    private static OxygenVolume _shipOxygenVolume;
    private static PlayerResources _playerResources;
    private static ProbeLauncherComponent _probeLauncherComponent;
    private static ShipTemperatureDetector _shipTemperatureDetector;
    private static ShipDamageController _shipDamageController;
    private static ShipOverdriveController _shipOverdriveController;
    private static SignalscopeComponent _signalscopeComponent;
    private static CockpitButtonPanel _buttonPanel;
    private static ThrustModulatorController _modulatorController;
    private static PortableCampfire _portableCampfire;
    private static CockpitFilthController _cockpitFilthController;
    private static FlightConsoleInteractController _consoleInteractController;

    public static void Initalize()
    {
        _shipBody = Object.FindObjectOfType<ShipBody>();
        _shipTransform = _shipBody.transform;
        _shipDetector = _shipBody.GetComponentInChildren<ShipFluidDetector>().gameObject;
        _playerBody = Object.FindObjectOfType<PlayerBody>();
        _probe = Object.FindObjectsOfType<SurveyorProbe>().Where(obj => obj.name == "Probe_Body").ToArray()[0];
        _shipResources = _shipBody.GetComponent<ShipResources>();
        _shipOxygenVolume = _shipBody.GetComponentInChildren<OxygenVolume>();
        _playerResources = _playerBody.GetComponent<PlayerResources>();
        _shipDamageController = _shipTransform.GetComponent<ShipDamageController>();

        if ((bool)shipOxygenRefill.GetProperty())
        {
            _shipOxygenDetector = _shipDetector.gameObject.AddComponent<OxygenDetector>();
        }
        if (temperatureZonesAmount.GetProperty().ToString() != "None")
        {
            _shipTemperatureDetector = _shipDetector.gameObject.AddComponent<ShipTemperatureDetector>();
        }
    }

    public static void LateInitialize()
    {
        _buttonPanel = _shipTransform.GetComponentInChildren<CockpitButtonPanel>();

        if ((bool)enableThrustModulator.GetProperty())
        {
            _modulatorController = _shipTransform.GetComponentInChildren<ThrustModulatorController>();
            _shipOverdriveController = _shipTransform.GetComponentInChildren<ShipOverdriveController>();
        }
        if ((bool)addPortableCampfire.GetProperty())
        {
            _portableCampfire = _shipTransform.GetComponentInChildren<PortableCampfire>(true);
        }
        if ((float)rustLevel.GetProperty() > 0 || (float)dirtAccumulationTime.GetProperty() > 0f)
        {
            _cockpitFilthController = _shipTransform.GetComponentInChildren<CockpitFilthController>();
        }
    }

    public static ShipBody GetShipBody()
    {
        return _shipBody;
    }

    public static Transform GetShipTransform()
    {
        return _shipTransform;
    }

    public static GameObject GetShipDetector()
    {
        return _shipDetector;
    }

    public static PlayerBody GetPlayerBody()
    {
        return _playerBody;
    }

    public static SurveyorProbe GetProbe()
    {
        return _probe;
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

    public static SignalscopeComponent GetSignalscopeComponent()
    {
        return _signalscopeComponent;
    }

    public static void SetSignalscopeComponent(SignalscopeComponent obj)
    {
        _signalscopeComponent = obj;
    }

    public static ShipTemperatureDetector GetShipTemperatureDetector()
    {
        return _shipTemperatureDetector;
    }

    public static ShipDamageController GetShipDamageController()
    {
        return _shipDamageController;
    }

    public static ShipOverdriveController GetShipOverdriveController()
    {
        return _shipOverdriveController;
    }

    public static CockpitButtonPanel GetButtonPanel()
    {
        return _buttonPanel;
    }

    public static ThrustModulatorController GetThrustModulatorController()
    {
        return _modulatorController;
    }

    public static PortableCampfire GetPortableCampfire()
    {
        return _portableCampfire;
    }

    public static CockpitFilthController GetCockpitFilthController()
    {
        return _cockpitFilthController;
    }

    public static FlightConsoleInteractController GetFlightConsoleInteractController()
    {
        return _consoleInteractController;
    }

    public static void SetFlightConsoleInteractController(FlightConsoleInteractController controller)
    {
        _consoleInteractController = controller;
    }
}
