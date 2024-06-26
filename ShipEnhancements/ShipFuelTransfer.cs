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
    private bool _fuelDepleted = false;

    private void Start()
    {
        _fuelTankComponent = GetComponentInParent<ShipFuelTankComponent>();
        _playerResources = Locator.GetPlayerBody().GetComponent<PlayerResources>();
        _shipResources = Locator.GetShipBody().GetComponent<ShipResources>();

        _fuelTankComponent.OnRepaired += ctx => OnComponentRepaired();
        _fuelTankComponent.OnDamaged += ctx => OnComponentDamaged();
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnReleaseInteract += OnReleaseInteract;
        GlobalMessenger.AddListener("SuitUp", OnSuitUp);
        GlobalMessenger.AddListener("RemoveSuit", OnRemoveSuit);

        _interactReceiver.SetPromptText(UITextType.HoldPrompt, "Transfer jetpack fuel");
    }

    private void Update()
    {
        if (_fuelDepleted)
        {
            if (_playerResources._currentFuel > 0)
            {
                _fuelDepleted = false;
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
                _shipResources.AddFuel(PlayerResources._maxFuel * Time.deltaTime * 10f);
                if (_playerResources._currentFuel <= 0)
                {
                    OnReleaseInteract();
                    _fuelDepleted = true;
                    _interactReceiver.DisableInteraction();
                }
            }
        }
    }

    private void OnPressInteract()
    {
        if (_fuelDepleted || !PlayerState.IsWearingSuit()) return;

        _fuelTankComponent._damageEffect._particleAudioSource.Play();
        _transferring = true;
    }

    private void OnReleaseInteract()
    {
        if (_fuelDepleted || !PlayerState.IsWearingSuit()) return;

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
        _interactReceiver.EnableInteraction();
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
