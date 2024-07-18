using UnityEngine;

namespace ShipEnhancements;

public class ShipRecoveryPoint : MonoBehaviour
{
    PlayerRecoveryPoint _recoveryPoint;

    private void Start()
    {
        _recoveryPoint = GetComponent<PlayerRecoveryPoint>();

        if (!_recoveryPoint._refuelsPlayer)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (_recoveryPoint._refuelsPlayer && ShipEnhancements.Instance.GetShipResources()._currentFuel == 0f)
        {
            _recoveryPoint._refuelsPlayer = false;
            return;
        }
        else if (!_recoveryPoint._refuelsPlayer && ShipEnhancements.Instance.GetShipResources()._currentFuel > 0f)
        {
            _recoveryPoint._refuelsPlayer = true;
        }

        if (_recoveryPoint._recovering && PlayerState.IsWearingSuit())
        {
            ShipEnhancements.Instance.GetShipResources()._currentFuel = Mathf.Max(ShipEnhancements.Instance.GetShipResources()._currentFuel 
                - (PlayerResources._maxFuel * 5f * Time.deltaTime * 3f * ShipEnhancements.Instance.FuelTransferMultiplier), 0f);
        }
    }
}
