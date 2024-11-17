using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipRefuelDrain : MonoBehaviour
{
    private PlayerRecoveryPoint _recoveryPoint;
    private ShipResources _shipResources;
    private bool _reverse;

    private void Awake()
    {
        _shipResources = SELocator.GetShipResources();
        _reverse = (float)fuelTransferMultiplier.GetProperty() < 0f;
    }

    private void Start()
    {
        if (!ShipEnhancements.InMultiplayer)
        {
            _recoveryPoint = GetComponent<PlayerRecoveryPoint>();

            if (!_recoveryPoint._refuelsPlayer)
            {
                enabled = false;
            }
        }
    }

    private void Update()
    {
        if (ShipEnhancements.InMultiplayer)
        {
            if (ShipEnhancements.QSBInteraction.IsRecoveringAtShip() && PlayerState.IsWearingSuit())
            {
                float amountToDrain = PlayerResources._maxFuel * 5f * Time.deltaTime * 3f * (float)fuelTransferMultiplier.GetProperty();
                _shipResources._currentFuel = Mathf.Clamp(_shipResources._currentFuel - amountToDrain, 0f, _shipResources._maxFuel);

                foreach (uint id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendShipFuelDrain(id, amountToDrain, false);
                }
            }
        }
        else
        {
            if (_recoveryPoint._refuelsPlayer && (_reverse ? _shipResources.GetFractionalFuel() == 1f : _shipResources.GetFractionalFuel() == 0f))
            {
                _recoveryPoint._refuelsPlayer = false;
                return;
            }
            else if (!_recoveryPoint._refuelsPlayer && (_reverse ? _shipResources.GetFractionalFuel() < 1f : _shipResources.GetFractionalFuel() > 0f))
            {
                _recoveryPoint._refuelsPlayer = true;
            }

            if (_recoveryPoint._recovering && PlayerState.IsWearingSuit())
            {
                float amountToDrain = PlayerResources._maxFuel * 5f * Time.deltaTime * 3f * (float)fuelTransferMultiplier.GetProperty();
                _shipResources._currentFuel = Mathf.Clamp(_shipResources._currentFuel - amountToDrain, 0f, _shipResources._maxFuel);
            }
        }
    }
}
