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
    private Transform _customDestination;

    private void Start()
    {
        _shipFluidDetector = SELocator.GetShipDetector().GetComponent<FluidDetector>();

        ShipEnhancements.WriteDebugMessage(_warpDestination);
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

    public void SetCustomDestination(Transform transform)
    {
        _customDestination = transform;
        if (_customDestination != null)
        {
            _warpEffect.transform.parent = _customDestination;
            _warpEffect.transform.localPosition = Vector3.zero;
        }
        else
        {
            if (_gravityCannonSocket != null)
            {
                _warpEffect.transform.parent = _gravityCannonSocket;
                _warpEffect.transform.localPosition = Vector3.zero;
            }
            else
            {
                _warpEffect.transform.parent = _warpDestination;
                _warpEffect.transform.localPosition = Vector3.zero;
            }
        }
    }

    public void OnCustomSpawnPoint()
    {
        _warpDestination.transform.localPosition = Vector3.zero;
    }

    public void WarpBodyToReceiver(OWRigidbody body, bool inShip)
    {
        if (_customDestination != null)
        {
            RepositionBody(body, _customDestination, inShip);
        }
        else if (_gravityCannonSocket != null)
        {
            RepositionBody(body, _gravityCannonSocket, inShip);
        }
        else
        {
            RepositionBody(body, _warpDestination, inShip);
        }

        if (inShip)
        {
            _warpEffect.singularityController.Collapse();
        }
        else if ((bool)funnySounds.GetProperty())
        {
            _warpEffect._singularity._owOneShotSource.PlayOneShot(ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/tube_out.ogg"), 0.5f);
        }
    }

    private void RepositionBody(OWRigidbody body, Transform destination, bool inShip)
    {
        body.WarpToPositionRotation(destination.position, destination.rotation);

        if (body is not ShipBody && inShip && PlayerState.IsAttached())
        {
            GlobalMessenger.FireEvent("PlayerRepositioned");
        }

        OWRigidbody newBody = gameObject.GetAttachedOWRigidbody();
        if (newBody != null)
        {
            body.SetVelocity(newBody.GetPointVelocity(destination.position));
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

    public void PlayRecallEffect(float length, bool inShip)
    {
        ShipEnhancements.WriteDebugMessage("recall effect");
        if (inShip)
        {
            _warpEffect.singularityController.Create();
        }
        else
        {
            ShipEnhancements.WriteDebugMessage("scale zero");
            _warpEffect.OnWarpComplete += OnWarpComplete;
            _warpEffect.WarpObjectIn(length);
        }
    }

    public void OnCockpitDetached(OWRigidbody body, Transform cockpitPivot)
    {
        _warpEffect.transform.localPosition = cockpitPivot.localPosition;
        _warpEffect._warpedObjectGeometry = body.gameObject;
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

            if (!SELocator.GetShipDamageController().IsSystemFailed())
            {
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
}
