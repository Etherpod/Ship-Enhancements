using System;
using UnityEngine;

namespace ShipEnhancements;

public class HullBreachEntrywayTrigger : MonoBehaviour
{
    [SerializeField]
    private EntrywayTrigger _suppliesEntryway;
    [SerializeField]
    private EntrywayTrigger _engineEntryway;
    [SerializeField]
    private EntrywayTrigger _hatchEntryway;

    private OWTriggerVolume _triggerVolume;
    private HatchController _hatch;
    private OWTriggerVolume _gravityVolume;
    private ShipTractorBeamSwitch _tractorBeam;
    private bool _hullBreached = false;

    private void Awake()
    {
        _triggerVolume = GetComponent<OWTriggerVolume>();
        _hatch = SELocator.GetShipTransform().GetComponentInChildren<HatchController>();
        _tractorBeam = SELocator.GetShipTransform().GetComponentInChildren<ShipTractorBeamSwitch>();
        _gravityVolume = SELocator.GetShipTransform().GetComponentInChildren<ShipDirectionalForceVolume>().GetOWTriggerVolume();

        _suppliesEntryway.OnExit += OnExitHull;
        _engineEntryway.OnExit += OnExitHull;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
    }

    private void Start()
    {
        _suppliesEntryway.SetActivation(false);
        _engineEntryway.SetActivation(false);
    }

    private void OnExitHull(GameObject hitObj)
    {
        if (hitObj.CompareTag("PlayerDetector"))
        {
            if (!(bool)ShipEnhancements.Settings.enableAutoHatch.GetProperty() && !ShipEnhancements.InMultiplayer)
            {
                _hatch.OpenHatch();
                _tractorBeam.ActivateTractorBeam();
            }
        }
    }

    private void OnShipSystemFailure()
    {
        _triggerVolume.SetTriggerActivation(false);
    }

    public void OnHullBreached()
    {
        if (PlayerState.IsInsideShip())
        {
            _triggerVolume.AddObjectToVolume(SELocator.GetPlayerBody().gameObject);
        }

        _hatch._triggerVolume.OnEntry -= _hatch.OnEntry;
        _hatch._triggerVolume.OnExit -= _hatch.OnExit;
        _hatch._triggerVolume = _triggerVolume;
        _triggerVolume.OnEntry += _hatch.OnEntry;
        _triggerVolume.OnExit += _hatch.OnExit;

        _hullBreached = true;
    }

    public void EnableSuppliesEntryway()
    {
        _suppliesEntryway.SetActivation(true);
        _gravityVolume._childEntryways.Add(_suppliesEntryway);
        _suppliesEntryway.Register();
        _suppliesEntryway.OnEntry += _gravityVolume.AddObjectToVolume;
        _suppliesEntryway.OnExit += _gravityVolume.RemoveObjectFromVolume;
    }

    public void EnableEngineEntryway()
    {
        _engineEntryway.SetActivation(true);
        _gravityVolume._childEntryways.Add(_engineEntryway);
        _engineEntryway.Register();
        _engineEntryway.OnEntry += _gravityVolume.AddObjectToVolume;
        _engineEntryway.OnExit += _gravityVolume.RemoveObjectFromVolume;
    }

    private void OnDestroy()
    {
        _suppliesEntryway.OnExit -= OnExitHull;
        _engineEntryway.OnExit -= OnExitHull;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);

        if (_hullBreached)
        {
            _triggerVolume.OnEntry -= _hatch.OnEntry;
            _triggerVolume.OnExit -= _hatch.OnExit;
        }
    }
}
