using System;
using UnityEngine;

namespace ShipEnhancements;

public class GravityLandingGear : MonoBehaviour
{
    ShipDamageController _damageController;
    private float _gravityMagnitude = 10f;
    private bool _gravityEnabled = false;
    private bool _shipDestroyed = false;

    private void Start()
    {
        ShipEnhancements.Instance.OnGravityLandingGearSwitch += SetGravityEnabled;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _damageController = Locator.GetShipBody().GetComponent<ShipDamageController>();
    }

    public void SetGravityEnabled(bool enabled)
    {
        _gravityEnabled = enabled;
    }

    private void OnTriggerStay(Collider hitCollider)
    {
        if (_shipDestroyed) return;

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

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
    }
}
