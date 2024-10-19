using System.Linq;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipFuelTransfer : MonoBehaviour
{
    [SerializeField]
    private InteractReceiver _interactReceiver;

    private ShipFuelTankComponent _fuelTankComponent;
    private bool _transferring = false;
    private bool _jetpackFuelDepleted = false;
    private bool _shipFuelFull = false;
    private bool _shipDestroyed = false;

    private void Start()
    {
        _fuelTankComponent = GetComponentInParent<ShipFuelTankComponent>();
        _shipFuelFull = IsShipFuelFull();

        _fuelTankComponent.OnRepaired += ctx => OnComponentRepaired();
        _fuelTankComponent.OnDamaged += ctx => OnComponentDamaged();
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;
        GlobalMessenger.AddListener("SuitUp", OnSuitUp);
        GlobalMessenger.AddListener("RemoveSuit", OnRemoveSuit);
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _fuelTankComponent._damageEffect._particleSystem.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        _interactReceiver.SetPromptText(UITextType.HoldPrompt, "Transfer jetpack fuel");
        if (_shipFuelFull)
        {
            _interactReceiver.DisableInteraction();
        }
    }

    private void Update()
    {
        if (_shipFuelFull && !IsShipFuelFull())
        {
            _shipFuelFull = false;
            if (!_jetpackFuelDepleted && (!_fuelTankComponent.isDamaged || (bool)disableShipRepair.GetProperty()))
            {
                _interactReceiver.EnableInteraction();
            }
        }

        if (_jetpackFuelDepleted && SELocator.GetPlayerResources()._currentFuel > 0)
        {
            _jetpackFuelDepleted = false;
            if (!_fuelTankComponent.isDamaged || (bool)disableShipRepair.GetProperty())
            {
                _interactReceiver.EnableInteraction();
            }
        }
        else if (_transferring)
        {
            if (!_interactReceiver.IsFocused())
            {
                OnReleaseInteract();
            }
            else
            {
                SELocator.GetPlayerResources()._currentFuel -= (PlayerResources._maxFuel * Time.deltaTime) / 3f;
                float amountToAdd = PlayerResources._maxFuel * Time.deltaTime * 5f * (float)fuelTransferMultiplier.GetProperty();
                SELocator.GetShipResources().AddFuel(amountToAdd);

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendShipFuelDrain(id, -amountToAdd, false);
                    }
                }

                if (SELocator.GetPlayerResources()._currentFuel <= 0)
                {
                    OnReleaseInteract();
                    _jetpackFuelDepleted = true;
                    _interactReceiver.DisableInteraction();
                }
                else if (IsShipFuelFull())
                {
                    OnReleaseInteract();
                    _shipFuelFull = true;
                    _interactReceiver.DisableInteraction();
                    
                    if (ShipEnhancements.InMultiplayer)
                    {
                        foreach (uint id in ShipEnhancements.PlayerIDs)
                        {
                            ShipEnhancements.QSBCompat.SendShipFuelMax(id);
                        }
                    }
                }
            }
        }
    }

    private bool IsShipFuelFull()
    {
        return SELocator.GetShipResources()._currentFuel >= SELocator.GetShipResources()._maxFuel;
    }

    public void UpdateInteractable()
    {
        _shipFuelFull = IsShipFuelFull();
        _jetpackFuelDepleted = SELocator.GetPlayerResources()._currentFuel <= 0;

        if (_shipDestroyed) return;

        if (_shipFuelFull || _jetpackFuelDepleted)
        {
            _interactReceiver.DisableInteraction();
        }
        else if (PlayerState.IsWearingSuit() && !_fuelTankComponent.isDamaged)
        {
            _interactReceiver.EnableInteraction();
        }
    }

    private void OnPressInteract()
    {
        if (_shipFuelFull || _jetpackFuelDepleted || !PlayerState.IsWearingSuit()) return;

        if ((bool)disableShipRepair.GetProperty() && _fuelTankComponent.isDamaged)
        {
            _fuelTankComponent._damageEffect._particleSystem.Stop();
        }

        _fuelTankComponent._damageEffect._particleAudioSource.Play();
        _transferring = true;
    }

    private void OnReleaseInteract()
    {
        if (_shipFuelFull || _jetpackFuelDepleted || !PlayerState.IsWearingSuit()) return;

        if ((bool)disableShipRepair.GetProperty() && _fuelTankComponent.isDamaged)
        {
            _fuelTankComponent._damageEffect._particleSystem.Play();
        }
        else
        {
            _fuelTankComponent._damageEffect._particleAudioSource.Stop();
        }

        SELocator.GetPlayerResources()._playerAudioController.PlayRefuel();
        _transferring = false;
        _interactReceiver.ResetInteraction();
    }

    private void OnComponentRepaired()
    {
        if (_shipDestroyed || (bool)disableShipRepair.GetProperty()) return;
        _interactReceiver.EnableInteraction();
    }

    private void OnComponentDamaged()
    {
        if (_shipDestroyed || (bool)disableShipRepair.GetProperty()) return;
        _interactReceiver.DisableInteraction();
    }

    private void OnSuitUp()
    {
        if (_shipDestroyed) return;

        if (!_shipFuelFull && !_jetpackFuelDepleted)
        {
            _interactReceiver.EnableInteraction();
        }
    }

    private void OnRemoveSuit()
    {
        if (_shipDestroyed) return;

        _interactReceiver.DisableInteraction();
    }

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
        _interactReceiver.DisableInteraction();
        enabled = false;
    }

    private void OnDestroy()
    {
        _fuelTankComponent.OnRepaired -= ctx => OnComponentRepaired();
        _fuelTankComponent.OnDamaged -= ctx => OnComponentDamaged();
        _interactReceiver.OnPressInteract -= OnPressInteract;
        _interactReceiver.OnReleaseInteract -= OnReleaseInteract;
        GlobalMessenger.RemoveListener("SuitUp", OnSuitUp);
        GlobalMessenger.RemoveListener("RemoveSuit", OnRemoveSuit);
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
