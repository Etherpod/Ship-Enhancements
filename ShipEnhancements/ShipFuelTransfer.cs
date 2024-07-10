using UnityEngine;

namespace ShipEnhancements;

public class ShipFuelTransfer : MonoBehaviour
{
    [SerializeField]
    private InteractReceiver _interactReceiver;

    private PlayerResources _playerResources;
    private ShipResources _shipResources;
    private ShipFuelTankComponent _fuelTankComponent;
    private bool _transferring = false;
    private bool _jetpackFuelDepleted = false;
    private bool _shipFuelFull = false;

    private void Start()
    {
        _fuelTankComponent = GetComponentInParent<ShipFuelTankComponent>();
        _playerResources = Locator.GetPlayerBody().GetComponent<PlayerResources>();
        _shipResources = Locator.GetShipBody().GetComponent<ShipResources>();
        _shipFuelFull = IsShipFuelFull();

        _fuelTankComponent.OnRepaired += ctx => OnComponentRepaired();
        _fuelTankComponent.OnDamaged += ctx => OnComponentDamaged();
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;
        GlobalMessenger.AddListener("SuitUp", OnSuitUp);
        GlobalMessenger.AddListener("RemoveSuit", OnRemoveSuit);

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
            if (!_jetpackFuelDepleted)
            {
                _interactReceiver.EnableInteraction();
            }
        }

        if (_jetpackFuelDepleted)
        {
            if (_playerResources._currentFuel > 0)
            {
                _jetpackFuelDepleted = false;
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
                _playerResources._currentFuel -= (PlayerResources._maxFuel * Time.deltaTime) / 3f;
                _shipResources.AddFuel(PlayerResources._maxFuel * Time.deltaTime * 5f * ShipEnhancements.Instance.FuelTransferMultiplier);
                if (_playerResources._currentFuel <= 0)
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
                }
            }
        }
    }

    private bool IsShipFuelFull()
    {
        return _shipResources._currentFuel >= _shipResources._maxFuel;
    }

    private void OnPressInteract()
    {
        if (_shipFuelFull || _jetpackFuelDepleted || !PlayerState.IsWearingSuit()) return;

        _fuelTankComponent._damageEffect._particleAudioSource.Play();
        _transferring = true;
    }

    private void OnReleaseInteract()
    {
        if (_shipFuelFull || _jetpackFuelDepleted || !PlayerState.IsWearingSuit()) return;

        _playerResources._playerAudioController.PlayRefuel();
        _fuelTankComponent._damageEffect._particleAudioSource.Stop();
        _transferring = false;
        _interactReceiver.ResetInteraction();
    }

    private void OnComponentRepaired()
    {
        _interactReceiver.EnableInteraction();
    }

    private void OnComponentDamaged()
    {
        _interactReceiver.DisableInteraction();
    }

    private void OnSuitUp()
    {
        if (!_shipFuelFull && !_jetpackFuelDepleted)
        {
            _interactReceiver.EnableInteraction();
        }
    }

    private void OnRemoveSuit()
    {
        _interactReceiver.DisableInteraction();
    }

    private void OnDestroy()
    {
        _fuelTankComponent.OnRepaired -= ctx => OnComponentRepaired();
        _fuelTankComponent.OnDamaged -= ctx => OnComponentDamaged();
        _interactReceiver.OnPressInteract -= OnPressInteract;
        _interactReceiver.OnReleaseInteract -= OnReleaseInteract;
        GlobalMessenger.RemoveListener("SuitUp", OnSuitUp);
        GlobalMessenger.RemoveListener("RemoveSuit", OnRemoveSuit);
    }
}
