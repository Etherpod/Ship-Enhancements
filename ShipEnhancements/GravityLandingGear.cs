using System;
using UnityEngine;

namespace ShipEnhancements;

public class GravityLandingGear : MonoBehaviour
{
    private float _gravityMagnitude = 10f;
    private bool _gravityEnabled = false;

    public void SetGravityEnabled(bool enabled)
    {
        _gravityEnabled = enabled;
    }

    private void OnTriggerStay(Collider hitCollider)
    {
        if (_gravityEnabled && hitCollider.attachedRigidbody != null)
        {
            Locator.GetShipBody().AddAcceleration(-transform.up * _gravityMagnitude);
        }
    }
}
