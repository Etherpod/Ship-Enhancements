using UnityEngine;

namespace ShipEnhancements.Interaction;

public interface INHInteraction
{
    public void AddTempZoneToNHSuns(GameObject tempZonePrefab);

    public (Transform, Vector3) GetShipSpawnPoint();

    public GameObject GetCenterOfUniverse();

    public bool IsWarpingBackToEye();
}
