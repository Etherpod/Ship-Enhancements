using ShipEnhancements;
using UnityEngine;

public interface IQSBInteraction
{
    bool FlightConsoleOccupied();

    Vector3 GetShipAcceleration();

    int GetPlayersInShip();

    GameObject GetShipRecoveryPoint();

    bool IsRecoveringAtShip();

    void SetHullDamaged(ShipHull shipHull, bool newlyDamaged);

    int GetIDFromItem(OWItem item);

    OWItem GetItemFromID(int itemID);

    bool WorldObjectsLoaded();

    void OnDetachAllPlayers(Vector3 velocity);

    void UpdateShipThrusterSync();
}
