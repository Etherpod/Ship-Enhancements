using System;
using UnityEngine;

namespace ShipEnhancements;

public class ShipFuelTransfer : MonoBehaviour
{
    [SerializeField]
    private InteractReceiver _interactReceiver;

    private PlayerResources _playerResources;
    private ShipResources _shipResources;
    private bool _transferring = false;
    private bool _fuelDepleted = false;

    private void Start()
    {
        ShipFuelTankComponent fuelTankComponent = GetComponentInParent<ShipFuelTankComponent>();
        _playerResources = Locator.GetPlayerBody().GetComponent<PlayerResources>();
        _shipResources = Locator.GetShipBody().GetComponent<ShipResources>();

        fuelTankComponent.OnRepaired += ctx => OnComponentRepaired();
        fuelTankComponent.OnDamaged += ctx => OnComponentDamaged();
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;

        _interactReceiver.ChangePrompt("Transfer jetpack fuel (Hold)");
    }

    private void Update()
    {
        if (_fuelDepleted)
        {
            if (_playerResources._currentFuel > 0)
            {
                _fuelDepleted = false;
            }
        }
        else if (_transferring)
        {
            _playerResources._currentFuel -= PlayerResources._maxFuel * Time.deltaTime * 0.5f;
            _shipResources.AddFuel(PlayerResources._maxFuel * Time.deltaTime * 10f);
            if (_playerResources._currentFuel <= 0)
            {
                _fuelDepleted = true;
            }
        }
    }

    private void OnPressInteract()
    {
        _transferring = true;
    }

    private void OnReleaseInteract()
    {
        _transferring = false;
    }

    private void OnComponentRepaired()
    {
        _interactReceiver.EnableInteraction();
    }

    private void OnComponentDamaged()
    {
        _interactReceiver.DisableInteraction();
    }
}
