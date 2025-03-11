using UnityEngine;

namespace ShipEnhancements;

public class GravityRepelVolume : MonoBehaviour
{
    private bool _repel = false;
    private int _trackedColliders = 0;

    private void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("PhysicalDetector");
    }

    public bool IsRepelling()
    {
        return _repel;
    }

    private void OnTriggerEnter(Collider hitCollider)
    {
        if (OWLayerMask.IsLayerInMask(hitCollider.gameObject.layer, OWLayerMask.physicalMask)
            || hitCollider.GetComponent<FluidVolume>()?.GetFluidType() == FluidVolume.Type.WATER)
        {
            _trackedColliders++;
            _repel = true;
        }
    }

    private void OnTriggerExit(Collider hitCollider)
    {
        if (OWLayerMask.IsLayerInMask(hitCollider.gameObject.layer, OWLayerMask.physicalMask)
            || hitCollider.GetComponent<FluidVolume>()?.GetFluidType() == FluidVolume.Type.WATER)
        {
            _trackedColliders = Mathf.Max(_trackedColliders - 1, 0);
            if (_trackedColliders == 0)
            {
                _repel = false;
            }
        }
    }
}
