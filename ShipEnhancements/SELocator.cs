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
    private static ShipTemperatureDamageController _shipTempDamageController;
    private static ShipDamageController _shipDamageController;
    private static ShipOverdriveController _shipOverdriveController;
    private static SignalscopeComponent _signalscopeComponent;
    private static CockpitButtonPanel _buttonPanel;
    private static ThrustModulatorController _modulatorController;
    private static CockpitEffectController _cockpitFilthController;
    private static FlightConsoleInteractController _consoleInteractController;
    private static CockpitErnesto _ernesto;
    private static ShipWarpCoreComponent _warpCoreComponent;
    private static AutopilotPanelController _autopilotPanelController;
    private static ShipWaterResource _waterResource;
    private static ShipCockpitController _shipCockpitController;
    private static ShipRemoteControl _remoteControl;

    private static ReferenceFrame _shipTargetRF;
    private static ReferenceFrame _playerTargetRF;
    private static ReferenceFrame _playerRF;
    private static ReferenceFrame _probeRF;

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
        _shipCockpitController = _shipTransform.GetComponentInChildren<ShipCockpitController>();

        _shipTargetRF = null;
        _playerTargetRF = null;
        _playerRF = new PlayerReferenceFrame(_playerBody);
        _probeRF = new ProbeReferenceFrame(_probe._owRigidbody);

        if ((bool)shipOxygenRefill.GetProperty())
        {
            _shipOxygenDetector = _shipDetector.gameObject.AddComponent<OxygenDetector>();
        }
        if ((bool)enableShipTemperature.GetProperty())
        {
            _shipTemperatureDetector = _shipDetector.gameObject.AddComponent<ShipTemperatureDetector>();
            _shipTempDamageController = _shipDetector.gameObject.AddComponent<ShipTemperatureDamageController>();
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
            _cockpitFilthController = _shipTransform.GetComponentInChildren<CockpitEffectController>(true);
        }
        if ((bool)addErnesto.GetProperty())
        {
            _ernesto = _shipTransform.GetComponentInChildren<CockpitErnesto>();
        }
        if ((bool)enableEnhancedAutopilot.GetProperty())
        {
            _autopilotPanelController = _shipTransform.GetComponentInChildren<AutopilotPanelController>();
        }
        if ((bool)addWaterTank.GetProperty())
        {
            _waterResource = _shipBody.GetComponent<ShipWaterResource>();
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
    
    public static ShipTemperatureDamageController GetShipTemperatureDamageController()
    {
        return _shipTempDamageController;
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

    public static CockpitEffectController GetCockpitFilthController()
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

    public static ShipWaterResource GetShipWaterResource()
    {
        return _waterResource;
    }

    public static ShipCockpitController GetShipCockpitController()
    {
        return _shipCockpitController;
    }

    public static void SetRemoteControl(ShipRemoteControl remoteControl)
    {
        _remoteControl = remoteControl;
    }

    public static ShipRemoteControl GetRemoteControl() => _remoteControl;

    public static ReferenceFrame GetReferenceFrame(bool shipFrame = true, bool ignorePassiveFrame = true)
    {
        if (((bool)splitLockOn.GetProperty() || _shipTargetRF != null) && (shipFrame || PlayerState.AtFlightConsole()))
        {
            bool targetingDockedProbe = IsShipTargetingProbe() && _probe != null && !_probe.IsLaunched();
            bool targetingDestroyedProbe = IsShipTargetingProbe() && _probe == null;
            if ((_shipTargetRF == null || ((IsShipTargetingPlayer() || targetingDockedProbe) && 
                        PlayerState.IsInsideShip()) || targetingDestroyedProbe) && 
                !ignorePassiveFrame)
            {
                return GetShipBody().GetComponentInChildren<SectorDetector>().GetPassiveReferenceFrame();
            }
            
            return _shipTargetRF;
        }
        
        return Locator.GetReferenceFrame(ignorePassiveFrame);
    }

    public static void SetShipReferenceFrame(ReferenceFrame rf)
    {
        if (rf == _playerRF || rf == _probeRF)
        {
            _shipTargetRF = rf;
            _playerTargetRF = null;
            return;
        }
        
        PatchClass.SkipNextRFUpdate = true;
        
        if ((bool)splitLockOn.GetProperty())
        {
            _shipTargetRF = rf;
        }
        else
        {
            _playerTargetRF = rf;
            _shipTargetRF = null;
        }
        
        if (!(bool)splitLockOn.GetProperty() || Locator._rfTracker._shipTargetingActive)
        {
            Locator._rfTracker.TargetReferenceFrame(rf);
        }
    }

    public static void TargetPlayerWithShip() => SetShipReferenceFrame(_playerRF);
    
    public static void TargetProbeWithShip() => SetShipReferenceFrame(_probeRF);

    public static bool IsShipTargetingPlayer() => _shipTargetRF == _playerRF;
    
    public static bool IsShipTargetingProbe() => _shipTargetRF == _probeRF;

    public static void OnTargetReferenceFrame(ReferenceFrame rf)
    {
        if (rf == _playerRF || rf == _probeRF) return;

        if (!(bool)splitLockOn.GetProperty())
        {
            _shipTargetRF = null;
            _playerTargetRF = rf;
            return;
        }
        
        if (Locator._rfTracker?._shipTargetingActive ?? false)
        {
            _shipTargetRF = rf;
        }
        else
        {
            _playerTargetRF = rf;
        }
    }

    public static void OnUntargetReferenceFrame()
    {
        if (!(bool)splitLockOn.GetProperty())
        {
            _shipTargetRF = null;
            _playerTargetRF = null;
            return;
        }
        
        if (Locator._rfTracker?._shipTargetingActive ?? false)
        {
            _shipTargetRF = null;
        }
        else
        {
            _playerTargetRF = null;
        }
    }

    public static void OnEnterShip()
    {
        PatchClass.SkipNextRFUpdate = true;
        ShipEnhancements.WriteDebugMessage(_shipTargetRF);
        if (_shipTargetRF != null && _shipTargetRF != _playerRF && 
            _shipTargetRF != _probeRF)
        {
            Locator._rfTracker.TargetReferenceFrame(_shipTargetRF);
        }
        else
        {
            Locator._rfTracker.UntargetReferenceFrame(false);
        }
    }

    public static void OnExitShip()
    {
        PatchClass.SkipNextRFUpdate = true;
        ShipEnhancements.WriteDebugMessage(_playerTargetRF);
        if (_playerTargetRF != null)
        {
            Locator._rfTracker.TargetReferenceFrame(_playerTargetRF);
        }
        else
        {
            Locator._rfTracker.UntargetReferenceFrame(false);
        }
    }
}
