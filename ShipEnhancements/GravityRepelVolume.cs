using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class GravityRepelVolume : MonoBehaviour
{
    private bool _repel = false;
    private List<GameObject> _trackedObjects = [];

    private void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("PhysicalDetector");
    }

    public bool IsRepelling()
    {
        return _repel;
    }

    private bool IsTrackable(GameObject hitObj)
    {
        return OWLayerMask.IsLayerInMask(hitObj.layer, OWLayerMask.physicalMask)
            || hitObj.GetComponent<FluidVolume>()?.GetFluidType() == FluidVolume.Type.WATER;
    }

    private void OnTriggerEnter(Collider hitCollider)
    {
        if (IsTrackable(hitCollider.gameObject))
        {
            _trackedObjects.Add(hitCollider.gameObject);
            _repel = true;
        }
    }

    private void OnTriggerExit(Collider hitCollider)
    {
        if (IsTrackable(hitCollider.gameObject))
        {
            if (_trackedObjects.Contains(hitCollider.gameObject))
            {
                _trackedObjects.Remove(hitCollider.gameObject);
                if (_trackedObjects.Count == 0)
                {
                    _repel = false;
                }
            }
        }
    }
}
