using UnityEngine;

namespace ShipEnhancements;

public class ShipAutoAlign : AlignWithDirection
{
    private ReferenceFrameTracker _rfTracker;

    public override void Awake()
    {
        //_localAlignmentAxis = new Vector3(0, -1, 0);
        _localAlignmentAxis = new Vector3(0, 0, 1);
        _interpolationMode = InterpolationMode.Exponential;
        _interpolationRate = 0.015f;
        _usePhysicsToRotate = true;

        base.Awake();
        enabled = false;
    }

    public override Vector3 GetAlignmentDirection()
    {
        ReferenceFrame referenceFrame = SELocator.GetReferenceFrame();
        if (referenceFrame == null)
        {
            return _currentDirection;
        }
        return referenceFrame.GetPosition() - _owRigidbody.GetWorldCenterOfMass();
    }

    public override bool CheckAlignmentRequirements()
    {
        return true;
    }

    public override void UpdateRotation(Vector3 currentDirection, Vector3 targetDirection, float slerpRate, bool usePhysics)
    {
        if (usePhysics)
        {
            Vector3 vector = OWPhysics.FromToAngularVelocity(currentDirection, targetDirection);
            //_owRigidbody.SetAngularVelocity(Vector3.zero);
            _owRigidbody.AddAngularVelocityChange(vector * slerpRate);
            return;
        }
        Quaternion quaternion = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(currentDirection, targetDirection), slerpRate);
        _owRigidbody.GetRigidbody().rotation = quaternion * _owRigidbody.GetRotation();
    }
}
