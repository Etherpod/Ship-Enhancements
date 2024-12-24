using UnityEngine;

namespace ShipEnhancements;

public class ShipWarpCoreReceiver : MonoBehaviour
{
    [SerializeField]
    private Transform _warpDestination;
    [SerializeField]
    private SingularityWarpEffect _warpEffect;

    private OWRigidbody _suspendedBody;
    private FluidDetector _shipFluidDetector;

    private void Awake()
    {
        _shipFluidDetector = SELocator.GetShipDetector().GetComponent<FluidDetector>();
    }

    public Transform GetWarpDestination()
    {
        return _warpDestination;
    }

    public void WarpBodyToReceiver(OWRigidbody body, bool inShip)
    {
        body.WarpToPositionRotation(_warpDestination.position, _warpDestination.rotation);
        OWRigidbody newBody = gameObject.GetAttachedOWRigidbody();
        if (newBody != null)
        {
            body.SetVelocity(newBody.GetPointVelocity(_warpDestination.position));
            body.SetAngularVelocity(newBody.GetAngularVelocity());

            if (!inShip && _suspendedBody == null)
            {
                body.Suspend(newBody);
                _suspendedBody = body;
                if (body is ShipBody)
                {
                    if (_shipFluidDetector.GetShape())
                    {
                        _shipFluidDetector.GetShape().SetActivation(false);
                    }
                    if (_shipFluidDetector.GetCollider())
                    {
                        _shipFluidDetector.GetCollider().enabled = false;
                    }
                    EffectVolume[] volsToRemove = [.. _shipFluidDetector._activeVolumes];
                    foreach (EffectVolume vol in volsToRemove)
                    {
                        vol._triggerVolume.RemoveObjectFromVolume(_shipFluidDetector.gameObject);
                    }
                }
            }
        }

        if (inShip)
        {
            _warpEffect.singularityController.Collapse();
        }
    }

    public void OnCockpitDetached(OWRigidbody body)
    {
        _warpEffect._warpedObjectGeometry = body.gameObject;
    }

    public void PlayRecallEffect(float length, bool inShip)
    {
        if (inShip)
        {
            _warpEffect.singularityController.Create();
        }
        else
        {
            _warpEffect.OnWarpComplete += OnWarpComplete;
            _warpEffect.WarpObjectIn(length);
        }
    }

    private void OnWarpComplete()
    {
        _warpEffect.OnWarpComplete -= OnWarpComplete;
        if (_suspendedBody != null)
        {
            _suspendedBody.Unsuspend();
            if (_suspendedBody is ShipBody)
            {
                if (_shipFluidDetector.GetShape())
                {
                    _shipFluidDetector.GetShape().SetActivation(true);
                }
                if (_shipFluidDetector.GetCollider())
                {
                    _shipFluidDetector.GetCollider().enabled = true;
                }
            }
            _suspendedBody = null;
        }
    }
}
