using ShipEnhancements;
using UnityEngine;

namespace ShipEnhancements;

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

    int GetIDFromSocket(OWItemSocket socket);

    OWItemSocket GetSocketFromID(int socketID);

    bool WorldObjectsLoaded();

    void OnDetachAllPlayers(Vector3 velocity);

    void UpdateShipThrusterSync();

    int GetIDFromAngler(AnglerfishController angler);

    AnglerfishController GetAnglerFromID(int id);

    int GetIDFromSwitch(CockpitSwitch cockpitSwitch);

    CockpitSwitch GetSwitchFromID(int id);

    int GetIDFromButton(CockpitButton cockpitSwitch);

    CockpitButton GetButtonFromID(int id);

    CockpitButtonSwitch GetButtonSwitchFromID(int id);

    int GetIDFromBody(OWRigidbody body);

    OWRigidbody GetBodyFromID(int id);
}
