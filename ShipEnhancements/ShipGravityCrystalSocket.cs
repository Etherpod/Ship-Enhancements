using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class ShipGravityCrystalSocket : OWItemSocket
{
    private List<GameObject> _componentMeshes = [];

    public override void Awake()
    {
        Reset();
        _sector = SELocator.GetShipSector();
        base.Awake();
        _acceptableType = ShipGravityCrystalItem.ItemType;
    }

    public void AddComponentMeshes(GameObject[] meshes)
    {
        _componentMeshes.AddRange(meshes);
    }

    public override bool PlaceIntoSocket(OWItem item)
    {
        bool result = base.PlaceIntoSocket(item);
        if (result)
        {
            foreach (GameObject obj in _componentMeshes)
            {
                obj.SetActive(true);
            }
        }
        return result;
    }

    public override OWItem RemoveFromSocket()
    {
        OWItem result = base.RemoveFromSocket();
        if (result != null)
        {
            foreach (GameObject obj in _componentMeshes)
            {
                obj.SetActive(false);
            }
        }
        return result;
    }
}
