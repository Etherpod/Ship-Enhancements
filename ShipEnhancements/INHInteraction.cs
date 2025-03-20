using UnityEngine;

public interface INHInteraction
{
    public void AddTempZoneToNHSuns(GameObject tempZonePrefab);

    public (Transform, Vector3) GetShipSpawnPoint();

    public GameObject GetCenterOfUniverse();
}
