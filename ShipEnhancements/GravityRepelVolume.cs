using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class GravityRepelVolume : MonoBehaviour
{
    private bool _repel = false;
    private List<GameObject> _trackedObjects = [];
    private List<GameObject> _trackedFluids = [];

    private void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("PhysicalDetector");
    }

    public bool IsRepelling(bool ignoreFluid)
    {
        if (ignoreFluid)
        {
            return _trackedObjects.Count > 0;
        }
        else
        {
            return _repel;
        }
    }

    private bool IsTrackable(GameObject hitObj)
    {
        return OWLayerMask.IsLayerInMask(hitObj.layer, OWLayerMask.physicalMask)
            || hitObj.GetComponent<FluidVolume>()?.GetFluidType() == FluidVolume.Type.WATER;
    }

    private void OnTriggerEnter(Collider hitCollider)
    {
        CleanTrackedObjects();
        if (IsTrackable(hitCollider.gameObject))
        {
            if (OWLayerMask.IsLayerInMask(hitCollider.gameObject.layer, OWLayerMask.physicalMask))
            {
                _trackedObjects.Add(hitCollider.gameObject);
            }
            else
            {
                _trackedFluids.Add(hitCollider.gameObject);
            }
            _repel = true;
        }
    }

    private void OnTriggerExit(Collider hitCollider)
    {
        CleanTrackedObjects();
        if (IsTrackable(hitCollider.gameObject))
        {
            if (_trackedObjects.Contains(hitCollider.gameObject))
            {
                _trackedObjects.Remove(hitCollider.gameObject);
            }
            else if (_trackedFluids.Contains(hitCollider.gameObject))
            {
                _trackedFluids.Remove(hitCollider.gameObject);
            }

            if (_trackedObjects.Count == 0 && _trackedFluids.Count == 0)
            {
                _repel = false;
            }
        }
    }

    private void CleanTrackedObjects()
    {
        for (int i = 0; i < _trackedObjects.Count; i++)
        {
            if (!_trackedObjects[i].activeInHierarchy)
            {
                _trackedObjects.RemoveAt(i);
                i--;
            }
        }
        for (int k = 0; k < _trackedFluids.Count; k++)
        {
            if (!_trackedFluids[k].activeInHierarchy)
            {
                _trackedFluids.RemoveAt(k);
                k--;
            }
        }
    }
}
