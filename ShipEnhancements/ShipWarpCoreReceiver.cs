using UnityEngine;

namespace ShipEnhancements;

public class ShipWarpCoreReceiver : MonoBehaviour
{
    [SerializeField]
    private Transform _warpDestination;
    [SerializeField]
    private SingularityWarpEffect _warpEffect;

    private OWRigidbody _suspendedBody;

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
            _suspendedBody = null;
        }
    }
}
