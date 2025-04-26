using System.Linq;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public static class SELocator
{
    private static ShipBody _shipBody;
    private static Transform _shipTransform;
    private static GameObject _shipDetector;
    private static Sector _shipSector;
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
    private static CockpitFilthController _cockpitFilthController;
    private static FlightConsoleInteractController _consoleInteractController;
    private static CockpitErnesto _ernesto;
    private static ShipWarpCoreComponent _warpCoreComponent;
    private static AutopilotPanelController _autopilotPanelController;

    private static ReferenceFrame _shipRF;
    private static ReferenceFrame _playerRF;

    public static void Initalize()
    {
        _shipBody = Object.FindObjectOfType<ShipBody>();
        _shipTransform = _shipBody.transform;
        _shipDetector = _shipBody.GetComponentInChildren<ShipFluidDetector>().gameObject;
        _shipSector = _shipBody.GetComponentInChildren<Sector>();
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
        _buttonPanel = _shipTransform.GetComponentInChildren<CockpitButtonPanel>(true);

        if ((bool)enableThrustModulator.GetProperty())
        {
            _modulatorController = _shipTransform.GetComponentInChildren<ThrustModulatorController>(true);
            _shipOverdriveController = _shipTransform.GetComponentInChildren<ShipOverdriveController>(true);
        }
        if ((float)rustLevel.GetProperty() > 0 || (float)dirtAccumulationTime.GetProperty() > 0f)
        {
            _cockpitFilthController = _shipTransform.GetComponentInChildren<CockpitFilthController>(true);
        }
        if ((bool)addErnesto.GetProperty())
        {
            _ernesto = _shipTransform.GetComponentInChildren<CockpitErnesto>();
        }
        if ((bool)enableEnhancedAutopilot.GetProperty())
        {
            _autopilotPanelController = _shipTransform.GetComponentInChildren<AutopilotPanelController>();
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
    
    public static Sector GetShipSector()
    {
        return _shipSector;
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

    public static ShipWarpCoreComponent GetShipWarpCoreComponent()
    {
        return _warpCoreComponent;
    }

    public static void SetShipWarpCoreComponent(ShipWarpCoreComponent obj)
    {
        _warpCoreComponent = obj;
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

    public static CockpitErnesto GetErnesto()
    {
        return _ernesto;
    }

    public static AutopilotPanelController GetAutopilotPanelController()
    {
        return _autopilotPanelController;
    }

    public static ReferenceFrame GetReferenceFrame(bool shipFrame = true, bool ignorePassiveFrame = true)
    {
        if ((bool)splitLockOn.GetProperty() && (shipFrame || PlayerState.AtFlightConsole()))
        {
            if (_shipRF == null && !ignorePassiveFrame)
            {
                return GetShipBody().GetComponentInChildren<SectorDetector>().GetPassiveReferenceFrame();
            }

            return _shipRF;
        }
        else
        {
            return Locator.GetReferenceFrame(ignorePassiveFrame);
        }
    }

    public static void OnTargetReferenceFrame(ReferenceFrame rf)
    {
        if (Locator._rfTracker._shipTargetingActive)
        {
            _shipRF = rf;
        }
        else
        {
            _playerRF = rf;
        }
    }

    public static void OnUntargetReferenceFrame()
    {
        if (Locator._rfTracker._shipTargetingActive)
        {
            _shipRF = null;
        }
        else
        {
            _playerRF = null;
        }
    }

    public static void OnEnterShip()
    {
        PatchClass.SkipNextRFUpdate = true;
        ShipEnhancements.WriteDebugMessage(_shipRF);
        if (_shipRF != null)
        {
            Locator._rfTracker.TargetReferenceFrame(_shipRF);
        }
        else
        {
            Locator._rfTracker.UntargetReferenceFrame();
        }
    }

    public static void OnExitShip()
    {
        PatchClass.SkipNextRFUpdate = true;
        ShipEnhancements.WriteDebugMessage(_playerRF);
        if (_playerRF != null)
        {
            Locator._rfTracker.TargetReferenceFrame(_playerRF);
        }
        else
        {
            Locator._rfTracker.UntargetReferenceFrame();
        }
    }
}
