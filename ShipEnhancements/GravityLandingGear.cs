using System;
using UnityEngine;

namespace ShipEnhancements;

public class GravityLandingGear : MonoBehaviour
{
    private float _gravityMagnitude = 10f;
    private bool _gravityEnabled = false;

    private void Start()
    {
        ShipEnhancements.Instance.OnGravityLandingGearSwitch += SetGravityEnabled;
    }

    public void SetGravityEnabled(bool enabled)
    {
        _gravityEnabled = enabled;
    }

    private void OnTriggerStay(Collider hitCollider)
    {
        bool damaged = GetComponentInParent<ShipLandingGear>().isDamaged;
        if (!damaged && _gravityEnabled && hitCollider.attachedRigidbody != null)
        {
            if (hitCollider.attachedRigidbody.isKinematic)
            {
                Locator.GetShipBody().AddAcceleration(-transform.up * _gravityMagnitude);
            }
            else
            {
                Locator.GetShipBody().AddAcceleration(-transform.up * (_gravityMagnitude / 10) * (1 / Locator.GetShipBody().GetMass()));
                hitCollider.attachedRigidbody.GetAttachedOWRigidbody().AddAcceleration(transform.up * (_gravityMagnitude / 10) * (1 / hitCollider.attachedRigidbody.mass));
            }
        }
    }
}
