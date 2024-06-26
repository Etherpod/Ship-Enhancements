using UnityEngine;

namespace ShipEnhancements;

public class ShipRecoveryPoint : MonoBehaviour
{
    PlayerRecoveryPoint _recoveryPoint;
    ShipResources _shipResources;

    private void Start()
    {
        _recoveryPoint = GetComponent<PlayerRecoveryPoint>();
        _shipResources = Locator.GetShipBody().GetComponent<ShipResources>();
        ShipEnhancements.WriteDebugMessage("started thing");
        if (!_recoveryPoint._refuelsPlayer)
        {
            ShipEnhancements.WriteDebugMessage("bad thing");
            enabled = false;
        }
    }

    private void Update()
    {
        if (_recoveryPoint._refuelsPlayer && _shipResources._currentFuel == 0f)
        {
            _recoveryPoint._refuelsPlayer = false;
            return;
        }
        else if (!_recoveryPoint._refuelsPlayer && _shipResources._currentFuel > 0f)
        {
            _recoveryPoint._refuelsPlayer = true;
        }

        if (_recoveryPoint._recovering && PlayerState.IsWearingSuit())
        {
            ShipEnhancements.WriteDebugMessage("doing thing");
            _shipResources._currentFuel = Mathf.Max(_shipResources._currentFuel - (PlayerResources._maxFuel * 10f * Time.deltaTime * 3f), 0f);
        }
    }
}
