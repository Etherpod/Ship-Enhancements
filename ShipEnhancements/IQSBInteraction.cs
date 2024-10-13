using ShipEnhancements;
using UnityEngine;

public interface IQSBInteraction
{
    bool FlightConsoleOccupied();

    Vector3 GetShipAcceleration();

    int GetPlayersInShip();

    GameObject GetShipRecoveryPoint();

    bool IsRecoveringAtShip();

    void SetHullDamaged(ShipHull shipHull);

    int GetIDFromTetherHook(TetherHookItem hookItem);

    TetherHookItem GetTetherHookFromID(int hookID);

}
