using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipWarpCoreReceiver : MonoBehaviour
{
    [SerializeField]
    private Transform _warpDestination;
    [SerializeField]
    private SingularityWarpEffect _warpEffect;

    private OWRigidbody _suspendedBody;
    private FluidDetector _shipFluidDetector;
    private Transform _gravityCannonSocket;

    private void Awake()
    {
        _shipFluidDetector = SELocator.GetShipDetector().GetComponent<FluidDetector>();
    }

    public void SetGravityCannonSocket(Transform destination)
    {
        _gravityCannonSocket = destination;
        if (destination != null)
        {
            _warpEffect.transform.parent = destination;
            _warpEffect.transform.localPosition = Vector3.zero;
        }
        else
        {
            _warpEffect.transform.parent = _warpDestination;
            _warpEffect.transform.localPosition = Vector3.zero;
        }
    }

    public void WarpBodyToReceiver(OWRigidbody body, bool inShip)
    {
        if (_gravityCannonSocket != null)
        {
            body.WarpToPositionRotation(_gravityCannonSocket.position + _gravityCannonSocket.up * 4f, _gravityCannonSocket.rotation);
            OWRigidbody newBody = _gravityCannonSocket.GetAttachedOWRigidbody();
            if (newBody != null)
            {
                body.SetVelocity(newBody.GetPointVelocity(_gravityCannonSocket.position));
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
        }
        else
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

            HatchController hatch = SELocator.GetShipBody().GetComponentInChildren<HatchController>();
            hatch._triggerVolume.SetTriggerActivation(true);
            if (!(bool)enableAutoHatch.GetProperty() && !ShipEnhancements.InMultiplayer)
            {
                hatch.OpenHatch();
                if (!(bool)singleUseTractorBeam.GetProperty())
                {
                    SELocator.GetShipBody().GetComponentInChildren<ShipTractorBeamSwitch>().ActivateTractorBeam();
                }
            }
        }
    }
}
