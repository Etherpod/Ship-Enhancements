using UnityEngine;

namespace ShipEnhancements;

public interface INHInteraction
{
    public void AddTempZoneToNHSuns(GameObject tempZonePrefab);

    public (Transform, Vector3) GetShipSpawnPoint();

    public GameObject GetCenterOfUniverse();
}
