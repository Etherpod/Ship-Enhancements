using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShipEnhancements;

public class OrbitAutopilotTest : ThrusterController
{
    private OWRigidbody _owRigidbody;
    private ReferenceFrame _referenceFrame;
    private Vector3 _initRelativeVelocity;
    private float _orbitRadius;
    private float _orbitSpeed;

    public override void Awake()
    {
        base.Awake();
        _owRigidbody = GetComponent<OWRigidbody>();
        enabled = false;
    }

    public void ToggleOrbitEnabled()
    {
        if (!enabled && Locator.GetReferenceFrame() != null)
        {
            _referenceFrame = Locator.GetReferenceFrame();
            _initRelativeVelocity = _owRigidbody.GetRelativeVelocity(_referenceFrame);
            _orbitRadius = (_referenceFrame.GetPosition() - _owRigidbody.GetWorldCenterOfMass()).magnitude;
            _orbitSpeed = _referenceFrame.GetOrbitSpeed(_orbitRadius);
            enabled = true;
        }
        else if (enabled)
        {
            enabled = false;
        }
    }

    public override Vector3 ReadTranslationalInput()
    {
        if (_referenceFrame == null)
        {
            enabled = false;
            return Vector3.zero;
        }

        Vector3 toTarget = _referenceFrame.GetPosition() - _owRigidbody.GetWorldCenterOfMass();
        float distance = toTarget.magnitude;
        Vector3 toHeight = -toTarget.normalized * (_orbitRadius - distance);
        if (toHeight.magnitude < 5f)
        {
            toHeight = Vector3.zero;
        }

        return transform.InverseTransformDirection(toHeight.normalized * _thrusterModel.GetMaxTranslationalThrust());
    }

    public override Vector3 ReadRotationalInput()
    {
        return Vector3.zero;
    }
}
